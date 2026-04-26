using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CineCoreBack.Models;
using CineCoreBack.DTOs;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Globalization;

namespace CineCoreBack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlannerController : ControllerBase
{
    private readonly DbConfig _context;

    public PlannerController(DbConfig context)
    {
        _context = context;
    }

    [HttpGet("project/{projectId}/board")]
    public async Task<ActionResult<PlannerBoardDto>> GetPlannerBoard(int projectId)
    {
        var board = new PlannerBoardDto();

        // 1. Отримуємо всі сцени проекту з усіма необхідними зв'язками за один запит
        var allScenes = await _context.Scenes
            .Where(s => s.ProjectId == projectId)
            .Include(s => s.SceneSchedules)
            .Include(s => s.SceneResources)
                .ThenInclude(sr => sr.Resource)
                    .ThenInclude(r => r.Location)
            .Include(s => s.ScriptElements)
                .ThenInclude(se => se.Role)
            .ToListAsync();

        // 2. Формуємо Scene Pool (сцени, які ще не мають розкладу)
        var unscheduledScenes = allScenes.Where(s => !s.SceneSchedules.Any()).ToList();

        foreach (var scene in unscheduledScenes)
        {
            board.ScenePool.Add(MapToPlannerSceneDto(scene, 0));
        }

        // 3. Отримуємо знімальні дні та формуємо їхні колонки
        var shootDays = await _context.ShootDays
            .Where(sd => sd.ProjectId == projectId)
            .Include(sd => sd.SceneSchedules)
            .OrderBy(sd => sd.ShiftStart)
            .ToListAsync();

        foreach (var day in shootDays)
        {
            var schedulesForDay = day.SceneSchedules.OrderBy(ss => ss.SceneOrder).ToList();
            var dayScenesDtos = new List<PlannerSceneDto>();

            TimeSpan scheduledDuration = TimeSpan.Zero;

            foreach (var schedule in schedulesForDay)
            {
                // Знаходимо повну сцену з кешу `allScenes`
                var scene = allScenes.FirstOrDefault(s => s.Id == schedule.SceneId);
                if (scene != null)
                {
                    dayScenesDtos.Add(MapToPlannerSceneDto(scene, schedule.SceneOrder));

                    // Підрахунок реального запланованого часу (Тривалість сцени + Час на підготовку)
                    if (scene.EstimatedDuration.HasValue)
                        scheduledDuration += scene.EstimatedDuration.Value;
                    if (schedule.PrepTimeEstimate.HasValue)
                        scheduledDuration += schedule.PrepTimeEstimate.Value;
                }
            }

            // Динамічний підрахунок місткості (Capacity)
            TimeSpan totalShiftDuration = day.ShiftEnd - day.ShiftStart;

            // Запобігаємо діленню на нуль, якщо зміна вказана некоректно (напр. старт і кінець однакові)
            int capacityPct = totalShiftDuration.TotalMinutes > 0
                ? (int)((scheduledDuration.TotalMinutes / totalShiftDuration.TotalMinutes) * 100)
                : 0;

            string capacityStr = $"{(int)scheduledDuration.TotalHours}h {scheduledDuration.Minutes}m / {(int)totalShiftDuration.TotalHours}h {totalShiftDuration.Minutes}m";

            board.ShootDays.Add(new PlannerShootDayDto
            {
                Id = day.Id,
                // InvariantCulture гарантує англійські назви місяців ("Mar", "Apr") незалежно від локалі сервера
                Date = day.ShiftStart.ToString("MMM dd", CultureInfo.InvariantCulture),
                ShootDateIso = day.ShiftStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ShiftStartTime = day.ShiftStart.ToString("HH:mm", CultureInfo.InvariantCulture),
                ShiftEndTime = day.ShiftEnd.ToString("HH:mm", CultureInfo.InvariantCulture),
                BaseLocationId = day.BaseLocationId,
                Unit = string.IsNullOrEmpty(day.UnitName) ? "MAIN UNIT" : day.UnitName.ToUpper(),
                Status = string.IsNullOrEmpty(day.Status) ? "draft" : day.Status,
                CallTime = day.ShiftStart.ToString("HH:mm", CultureInfo.InvariantCulture),
                Notes = day.GeneralNotes,
                CapacityStr = capacityStr,
                CapacityPct = capacityPct,
                Scenes = dayScenesDtos
            });
        }

        return Ok(board);
    }

    [HttpPost("project/{projectId}/shoot-day")]
    public async Task<IActionResult> CreateShootDay(int projectId, [FromBody] CreateShootDayDto dto)
    {
        // 1. Парсимо базову дату (напр. "2024-03-25")
        if (!DateTime.TryParse(dto.ShootDate, out var baseDate))
        {
            return BadRequest("Недійсний формат дати (ShootDate).");
        }

        // 2. Парсимо час (напр. "09:00" та "19:00"). Якщо формат невірний, ставимо дефолтні значення
        TimeSpan start = TimeSpan.TryParse(dto.ShiftStart, out var s) ? s : new TimeSpan(9, 0, 0);
        TimeSpan end = TimeSpan.TryParse(dto.ShiftEnd, out var e) ? e : new TimeSpan(19, 0, 0);

        // 3. З'єднуємо дату і час та ОБОВ'ЯЗКОВО вказуємо DateTimeKind.Utc для PostgreSQL
        DateTime shiftStartUtc = DateTime.SpecifyKind(baseDate.Date.Add(start), DateTimeKind.Utc);
        DateTime shiftEndUtc = DateTime.SpecifyKind(baseDate.Date.Add(end), DateTimeKind.Utc);

        var shootDay = new ShootDay
        {
            ProjectId = projectId,
            UnitName = dto.Unit,
            ShiftStart = shiftStartUtc,
            ShiftEnd = shiftEndUtc,
            BaseLocationId = dto.BaseLocationId,
            GeneralNotes = dto.Notes,
            Status = "draft"
        };

        _context.ShootDays.Add(shootDay);
        await _context.SaveChangesAsync();

        return Ok(shootDay);
    }

    [HttpPut("project/{projectId}/move-scene")]
    public async Task<IActionResult> ReorderScenes(int projectId, [FromBody] ReorderSceneDto req)
    {
        var existingSchedule = await _context.SceneSchedules
            .FirstOrDefaultAsync(ss => ss.SceneId == req.SceneId);

        if (req.TargetShootDayId == null)
        {
            // Сцену повернули в Pool — видаляємо SceneSchedule
            if (existingSchedule != null)
                _context.SceneSchedules.Remove(existingSchedule);
        }
        else
        {
            if (existingSchedule != null)
            {
                existingSchedule.ShootDayId = req.TargetShootDayId.Value;
                existingSchedule.SceneOrder = req.NewIndex;
            }
            else
            {
                _context.SceneSchedules.Add(new SceneSchedule
                {
                    SceneId = req.SceneId,
                    ShootDayId = req.TargetShootDayId.Value,
                    SceneOrder = req.NewIndex
                });
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("project/{projectId}/shoot-day/{dayId}")]
    public async Task<IActionResult> UpdateShootDay(int projectId, int dayId, [FromBody] UpdateShootDayDto dto)
    {
        var day = await _context.ShootDays
            .FirstOrDefaultAsync(sd => sd.Id == dayId && sd.ProjectId == projectId);

        if (day == null) return NotFound();

        // Оновлюємо лише ті поля, які передані
        if (!string.IsNullOrEmpty(dto.Unit)) day.UnitName = dto.Unit;
        if (!string.IsNullOrEmpty(dto.Status)) day.Status = dto.Status;
        if (dto.BaseLocationId.HasValue) day.BaseLocationId = dto.BaseLocationId;
        day.GeneralNotes = dto.GeneralNotes;

        // Обробка дат (враховуючи ваш формат ShiftStart/End у ShootDay.cs)
        // Приклад з UpdateShootDay:
        if (!string.IsNullOrEmpty(dto.ShootDate))
        {
            if (DateTime.TryParse(dto.ShootDate, out var date))
            {
                // Зберігаємо старий час, але оновлюємо дату, ставлячи UTC
                day.ShiftStart = DateTime.SpecifyKind(date.Date.Add(day.ShiftStart.TimeOfDay), DateTimeKind.Utc);
                day.ShiftEnd = DateTime.SpecifyKind(date.Date.Add(day.ShiftEnd.TimeOfDay), DateTimeKind.Utc);
            }
        }

        await _context.SaveChangesAsync();
        return Ok(day);
    }

    // ВИДАЛЕННЯ ДНЯ
    [HttpDelete("project/{projectId}/shoot-day/{dayId}")]
    public async Task<IActionResult> DeleteShootDay(int projectId, int dayId)
    {
        var day = await _context.ShootDays
            .Include(sd => sd.SceneSchedules)
            .FirstOrDefaultAsync(sd => sd.Id == dayId && sd.ProjectId == projectId);

        if (day == null) return NotFound();

        // При видаленні дня SceneSchedules видаляться каскадно (якщо налаштовано в БД)
        // або ми можемо видалити їх вручну тут
        _context.SceneSchedules.RemoveRange(day.SceneSchedules);
        _context.ShootDays.Remove(day);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // --------------------------------------------------------
    // ДОПОМІЖНІ МЕТОДИ
    // --------------------------------------------------------

    private PlannerSceneDto MapToPlannerSceneDto(Scene scene, int order)
    {
        // 1. TimeOfDay: Динамічний парсинг з SluglineText (напр. "EXT. HOUSE - NIGHT")
        string timeOfDay = "N/A";
        if (!string.IsNullOrEmpty(scene.SluglineText))
        {
            var parts = scene.SluglineText.Split('-');
            if (parts.Length > 1)
            {
                timeOfDay = parts.Last().Trim().ToUpper();
            }
            else if (scene.SluglineText.Contains("DAY", StringComparison.OrdinalIgnoreCase)) timeOfDay = "DAY";
            else if (scene.SluglineText.Contains("NIGHT", StringComparison.OrdinalIgnoreCase)) timeOfDay = "NIGHT";
        }

        // 2. Duration: Реальний час зі сцени
        string durationStr = "0m";
        if (scene.EstimatedDuration.HasValue)
        {
            var dur = scene.EstimatedDuration.Value;
            if (dur.Hours > 0 && dur.Seconds > 0)
                durationStr = $"{dur.Hours}h {dur.Minutes}m {dur.Seconds}s";
            else if (dur.Hours > 0)
                durationStr = $"{dur.Hours}h {dur.Minutes}m";
            else if (dur.Seconds > 0)
                durationStr = $"{dur.Minutes}m {dur.Seconds}s";
            else
                durationStr = $"{dur.Minutes}m";
        }

        // 3. Location: Пошук першого ресурсу типу Location
        string locationName = scene.SceneResources
            .Where(sr => sr.Resource != null && sr.Resource.Location != null)
            .Select(sr => sr.Resource.Location.LocationName)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(locationName))
        {
            // Fallback: якщо локацію ще не створено як ресурс, пробуємо дістати її зі Slugline
            if (!string.IsNullOrEmpty(scene.SluglineText) && scene.SluglineText.Contains("-"))
            {
                var parts = scene.SluglineText.Split('-');
                locationName = parts[0].Trim(); // Беремо все, що до дефісу

                if (locationName.StartsWith("EXT.") || locationName.StartsWith("INT."))
                {
                    locationName = locationName.Substring(4).Trim();
                }
            }
            else
            {
                locationName = "Unspecified Location";
            }
        }

        // 4. Cast: Унікальні ролі з елементів сценарію (разом із кольорами)
        var uniqueRoles = scene.ScriptElements
            .Where(se => se.Role != null && !string.IsNullOrEmpty(se.Role.RoleName))
            .Select(se => se.Role)
            .DistinctBy(r => r.Id)
            .ToList();

        var castList = uniqueRoles.Select(r => GetInitials(r.RoleName)).ToList();
        var castColors = uniqueRoles
            .Select(r => string.IsNullOrEmpty(r.ColorHex) ? "#3AB9A0" : r.ColorHex)
            .ToList();

        var roleIds = uniqueRoles.Select(r => r.Id).ToList();

        return new PlannerSceneDto
        {
            Id = scene.Id,
            DisplayId = $"SC-{(scene.SequenceNum):D3}",
            Title = string.IsNullOrEmpty(scene.SluglineText) ? "Untitled Scene" : scene.SluglineText,
            Duration = durationStr,
            TimeOfDay = timeOfDay,
            Location = locationName,
            Cast = castList,
            CastColors = castColors,
            RoleIds = roleIds,
            Order = order
        };
    }

    private string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        var words = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1)
        {
            return words[0].Length >= 2 ? words[0].Substring(0, 2).ToUpper() : words[0].ToUpper();
        }
        return (words[0].Substring(0, 1) + words[1].Substring(0, 1)).ToUpper();
    }

    // POST api/planner/project/{projectId}/auto-schedule
    [HttpPost("project/{projectId}/auto-schedule")]
    public async Task<ActionResult<AutoScheduleResultDto>> AutoSchedule(
    int projectId, [FromBody] AutoScheduleRequestDto req)
    {
        var result = new AutoScheduleResultDto();

        // 1. Незаплановані сцени
        var unscheduledScenes = await _context.Scenes
            .Where(s => s.ProjectId == projectId && !s.SceneSchedules.Any())
            .Include(s => s.SceneResources)
                .ThenInclude(sr => sr.Resource)
                    .ThenInclude(r => r.Location)
            .Include(s => s.ScriptElements)
                .ThenInclude(se => se.Role)
            .OrderBy(s => s.SequenceNum)
            .ToListAsync();

        if (!unscheduledScenes.Any())
        {
            result.Message = "All scenes are already scheduled.";
            return Ok(result);
        }

        // 2. Формуємо список цільових днів
        List<ShootDay> targetDays;

        if (req.Mode == "fill")
        {
            targetDays = await _context.ShootDays
                .Where(sd => sd.ProjectId == projectId &&
                             (sd.Status == "draft" || sd.Status == "generated"))
                .Include(sd => sd.SceneSchedules)
                    .ThenInclude(ss => ss.Scene)
                .OrderBy(sd => sd.ShiftStart)
                .ToListAsync();

            if (!targetDays.Any())
            {
                result.Message = "No available shoot days found. Use 'Generate' mode to create new days.";
                return Ok(result);
            }
        }
        else // generate
        {
            if (!DateTime.TryParse(req.StartDate, out var startDate))
                startDate = DateTime.UtcNow.Date;

            TimeSpan shiftStart = TimeSpan.TryParse(req.DefaultShiftStart, out var ss)
                ? ss : new TimeSpan(9, 0, 0);
            TimeSpan shiftEnd = TimeSpan.TryParse(req.DefaultShiftEnd, out var se)
                ? se : new TimeSpan(19, 0, 0);

            double effectiveShiftMinutes = (shiftEnd - shiftStart).TotalMinutes;
            if (effectiveShiftMinutes <= 0) effectiveShiftMinutes = 600;

            // Рахуємо загальну тривалість — сцени без duration отримують дефолт 20 хв
            double totalMinutes = unscheduledScenes
                .Sum(s => s.EstimatedDuration.HasValue
                    ? s.EstimatedDuration.Value.TotalMinutes
                    : 20.0);

            // +1 день про запас, мінімум 1
            int daysNeeded = Math.Max(1,
                (int)Math.Ceiling(totalMinutes / effectiveShiftMinutes) + 1);

            targetDays = new List<ShootDay>();
            var currentDate = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);

            for (int i = 0; i < daysNeeded; i++)
            {
                if (req.SkipWeekends)
                {
                    while (currentDate.DayOfWeek == DayOfWeek.Saturday ||
                           currentDate.DayOfWeek == DayOfWeek.Sunday)
                        currentDate = currentDate.AddDays(1);
                }

                var newDay = new ShootDay
                {
                    ProjectId = projectId,
                    UnitName = "MAIN UNIT",
                    ShiftStart = DateTime.SpecifyKind(currentDate.Add(shiftStart), DateTimeKind.Utc),
                    ShiftEnd = DateTime.SpecifyKind(currentDate.Add(shiftEnd), DateTimeKind.Utc),
                    Status = "generated",
                    GeneralNotes = "Auto-generated. Review and confirm."
                };

                _context.ShootDays.Add(newDay);
                targetDays.Add(newDay);
                currentDate = currentDate.AddDays(1);
            }

            // *** КРИТИЧНО: зберігаємо ДО розподілу щоб отримати реальні Id ***
            await _context.SaveChangesAsync();
        }

        // 3. Сортування сцен
        IEnumerable<Scene> sortedScenes = req.GroupBy == "location"
            ? unscheduledScenes
                .GroupBy(s => GetPrimaryLocationId(s))
                .OrderByDescending(g => g.Key.HasValue ? g.Count() : 0)
                .SelectMany(g => g)
            : unscheduledScenes.OrderBy(s => s.SequenceNum);

        // 4. Алгоритм розподілу — повністю in-memory, без async запитів у циклі
        // Будуємо словник: dayId -> вже заплановані хвилини
        var scheduledMinutesPerDay = new Dictionary<int, double>();

        foreach (var day in targetDays)
        {
            // Для fill-режиму: враховуємо вже прикріплені сцени
            double alreadyScheduled = 0;
            if (req.Mode == "fill" && day.SceneSchedules?.Any() == true)
            {
                alreadyScheduled = day.SceneSchedules
                    .Where(ss => ss.Scene != null)
                    .Sum(ss => ss.Scene.EstimatedDuration.HasValue
                        ? ss.Scene.EstimatedDuration.Value.TotalMinutes
                        : 20.0);
            }
            scheduledMinutesPerDay[day.Id] = alreadyScheduled;
        }

        // Словник: dayId -> наступний порядковий номер сцени
        var nextOrderPerDay = new Dictionary<int, int>();
        foreach (var day in targetDays)
        {
            int existingCount = day.SceneSchedules?.Count ?? 0;
            nextOrderPerDay[day.Id] = existingCount;
        }

        double maxShiftMinutes = req.MaxShiftMinutes > 0 ? req.MaxShiftMinutes : 600;
        var newSchedules = new List<SceneSchedule>();
        int scheduledCount = 0;

        foreach (var scene in sortedScenes)
        {
            double sceneDuration = scene.EstimatedDuration.HasValue
                ? scene.EstimatedDuration.Value.TotalMinutes
                : 20.0; // дефолт для сцен без тривалості

            // Шукаємо перший день де є місце
            ShootDay? foundDay = null;

            foreach (var day in targetDays)
            {
                double shiftCapacity = Math.Min(
                    (day.ShiftEnd - day.ShiftStart).TotalMinutes,
                    maxShiftMinutes);

                double used = scheduledMinutesPerDay[day.Id];
                double wouldUse = used + sceneDuration + req.BufferMinutes;

                if (wouldUse <= shiftCapacity || used == 0) // якщо день порожній — кладемо завжди
                {
                    foundDay = day;
                    break;
                }
            }

            if (foundDay == null) break; // більше немає місця

            newSchedules.Add(new SceneSchedule
            {
                SceneId = scene.Id,
                ShootDayId = foundDay.Id,
                SceneOrder = nextOrderPerDay[foundDay.Id]++
            });

            scheduledMinutesPerDay[foundDay.Id] += sceneDuration + req.BufferMinutes;
            scheduledCount++;
        }

        // 5. Зберігаємо всі SceneSchedule одним батчем
        _context.SceneSchedules.AddRange(newSchedules);
        await _context.SaveChangesAsync();

        // 6. Формуємо відповідь
        // Повертаємо тільки дні де реально є хоч одна сцена
        var usedDayIds = newSchedules.Select(ss => ss.ShootDayId).Distinct().ToHashSet();

        result.TotalScenesScheduled = scheduledCount;
        result.UnscheduledCount = unscheduledScenes.Count - scheduledCount;
        result.Message = $"Scheduled {scheduledCount} scenes across {usedDayIds.Count} days.";

        foreach (var day in targetDays.Where(d => usedDayIds.Contains(d.Id)))
        {
            var assignedScenes = newSchedules
                .Where(ss => ss.ShootDayId == day.Id)
                .ToList();

            double shiftCapacity = (day.ShiftEnd - day.ShiftStart).TotalMinutes;
            double usedMinutes = scheduledMinutesPerDay[day.Id];

            result.GeneratedDays.Add(new AutoScheduleDayPreviewDto
            {
                ShootDayId = day.Id,
                Date = day.ShiftStart.ToString("MMM dd", CultureInfo.InvariantCulture),
                ShootDateIso = day.ShiftStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                IsNewlyCreated = req.Mode == "generate",
                AssignedSceneIds = assignedScenes.Select(ss => ss.SceneId).ToList(),
                CapacityStr = $"{(int)(usedMinutes / 60)}h {(int)(usedMinutes % 60)}m / {(int)(shiftCapacity / 60)}h {(int)(shiftCapacity % 60)}m",
                CapacityPct = shiftCapacity > 0 ? (int)(usedMinutes / shiftCapacity * 100) : 0
            });
        }

        return Ok(result);
    }

    // POST api/planner/project/{projectId}/confirm-day
    // Підтверджує або відхиляє конкретний generated день
    [HttpPost("project/{projectId}/confirm-day")]
    public async Task<IActionResult> ConfirmDay(
        int projectId, [FromBody] ConfirmDayDto dto)
    {
        var day = await _context.ShootDays
            .Include(sd => sd.SceneSchedules)
            .FirstOrDefaultAsync(sd => sd.Id == dto.ShootDayId
                                    && sd.ProjectId == projectId);

        if (day == null) return NotFound();

        if (dto.Confirm)
        {
            // Підтверджуємо → змінюємо статус з generated на draft
            day.Status = "draft";
            day.GeneralNotes = day.GeneralNotes?.Replace(
                "Auto-generated. Review and confirm.", "").Trim();
        }
        else
        {
            // Відхиляємо → видаляємо SceneSchedules (сцени повертаються в pool)
            _context.SceneSchedules.RemoveRange(day.SceneSchedules);
            _context.ShootDays.Remove(day);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // POST api/planner/project/{projectId}/confirm-all-generated
    // Масове підтвердження всіх generated днів
    [HttpPost("project/{projectId}/confirm-all-generated")]
    public async Task<IActionResult> ConfirmAllGenerated(int projectId)
    {
        var generatedDays = await _context.ShootDays
            .Where(sd => sd.ProjectId == projectId && sd.Status == "generated")
            .ToListAsync();

        foreach (var day in generatedDays)
        {
            day.Status = "draft";
        }

        await _context.SaveChangesAsync();
        return Ok(new { confirmed = generatedDays.Count });
    }

    // Допоміжні методи
    private static int? GetPrimaryLocationId(Scene scene)
    {
        return scene.SceneResources
            .Where(sr => sr.Resource?.Location != null)
            .Select(sr => (int?)sr.Resource.Location.Id)
            .FirstOrDefault();
    }

    private async Task<double> GetScheduledMinutesForDay(int dayId)
    {
        var sceneIds = await _context.SceneSchedules
            .Where(ss => ss.ShootDayId == dayId)
            .Select(ss => ss.SceneId)
            .ToListAsync();

        return await _context.Scenes
            .Where(s => sceneIds.Contains(s.Id))
            .SumAsync(s => (double?)s.EstimatedDuration.Value.TotalMinutes ?? 30);
    }
}