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

    // Коефіцієнт зйомки: реальний час зйомки в середньому в 5-8 разів більший
    // ніж екранний час. Використовуємо 6x як розумний середній показник.
    private const double ShootingRatio = 6.0;

    // Мінімальний час на сцену якщо EstimatedDuration не задано (хвилини екранного часу)
    private const double DefaultScreenMinutes = 2.0;

    public PlannerController(DbConfig context)
    {
        _context = context;
    }

    // --------------------------------------------------------
    // GET BOARD
    // --------------------------------------------------------
    [HttpGet("project/{projectId}/board")]
    public async Task<ActionResult<PlannerBoardDto>> GetPlannerBoard(int projectId)
    {
        var board = new PlannerBoardDto();

        var allScenes = await _context.Scenes
            .Where(s => s.ProjectId == projectId)
            .Include(s => s.SceneSchedules)
            .Include(s => s.SceneResources)
                .ThenInclude(sr => sr.Resource)
                    .ThenInclude(r => r.Location)
            .Include(s => s.ScriptElements)
                .ThenInclude(se => se.Role)
            .ToListAsync();

        // Scene Pool: сцени без розкладу
        var unscheduledScenes = allScenes.Where(s => !s.SceneSchedules.Any()).ToList();
        foreach (var scene in unscheduledScenes)
            board.ScenePool.Add(MapToPlannerSceneDto(scene, 0));

        var shootDays = await _context.ShootDays
            .Where(sd => sd.ProjectId == projectId)
            .Include(sd => sd.SceneSchedules)
            .OrderBy(sd => sd.ShiftStart)
            .ToListAsync();

        foreach (var day in shootDays)
        {
            var schedulesForDay = day.SceneSchedules.OrderBy(ss => ss.SceneOrder).ToList();
            var dayScenesDtos = new List<PlannerSceneDto>();

            // Capacity: рахуємо shoot time (не screen time)
            double scheduledShootMinutes = 0;

            foreach (var schedule in schedulesForDay)
            {
                var scene = allScenes.FirstOrDefault(s => s.Id == schedule.SceneId);
                if (scene != null)
                {
                    dayScenesDtos.Add(MapToPlannerSceneDto(scene, schedule.SceneOrder));
                    scheduledShootMinutes += GetShootMinutes(scene);
                }
            }

            TimeSpan totalShiftDuration = day.ShiftEnd - day.ShiftStart;
            double shiftMinutesTotal = totalShiftDuration.TotalMinutes > 0
                ? totalShiftDuration.TotalMinutes : 600;

            int capacityPct = Math.Min(100,
                (int)(scheduledShootMinutes / shiftMinutesTotal * 100));

            string capacityStr =
                $"{FormatMinutes(scheduledShootMinutes)} / {FormatMinutes(shiftMinutesTotal)}";

            board.ShootDays.Add(new PlannerShootDayDto
            {
                Id = day.Id,
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

    // --------------------------------------------------------
    // CREATE SHOOT DAY
    // --------------------------------------------------------
    [HttpPost("project/{projectId}/shoot-day")]
    public async Task<IActionResult> CreateShootDay(int projectId, [FromBody] CreateShootDayDto dto)
    {
        if (!DateTime.TryParse(dto.ShootDate, out var baseDate))
            return BadRequest("Недійсний формат дати (ShootDate).");

        TimeSpan start = TimeSpan.TryParse(dto.ShiftStart, out var s) ? s : new TimeSpan(9, 0, 0);
        TimeSpan end = TimeSpan.TryParse(dto.ShiftEnd, out var e) ? e : new TimeSpan(19, 0, 0);

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

    // --------------------------------------------------------
    // UPDATE SHOOT DAY — ВИПРАВЛЕНО: зберігає shiftStart/shiftEnd
    // --------------------------------------------------------
    [HttpPut("project/{projectId}/shoot-day/{dayId}")]
    public async Task<IActionResult> UpdateShootDay(int projectId, int dayId,
        [FromBody] UpdateShootDayDto dto)
    {
        var day = await _context.ShootDays
            .FirstOrDefaultAsync(sd => sd.Id == dayId && sd.ProjectId == projectId);

        if (day == null) return NotFound();

        if (!string.IsNullOrEmpty(dto.Unit)) day.UnitName = dto.Unit;
        if (!string.IsNullOrEmpty(dto.Status)) day.Status = dto.Status;
        day.GeneralNotes = dto.GeneralNotes;

        if (dto.BaseLocationId.HasValue)
            day.BaseLocationId = dto.BaseLocationId;

        // Визначаємо базову дату: або передана нова, або залишаємо стару
        DateTime baseDate = day.ShiftStart.Date;
        if (!string.IsNullOrEmpty(dto.ShootDate) &&
            DateTime.TryParse(dto.ShootDate, out var parsedDate))
        {
            baseDate = parsedDate.Date;
        }

        // Визначаємо час початку: або передано нове значення, або залишаємо старе
        TimeSpan newStart = day.ShiftStart.TimeOfDay;
        if (!string.IsNullOrEmpty(dto.ShiftStart) &&
            TimeSpan.TryParse(dto.ShiftStart, out var parsedStart))
        {
            newStart = parsedStart;
        }

        // Визначаємо час кінця: або передано нове значення, або залишаємо старе
        TimeSpan newEnd = day.ShiftEnd.TimeOfDay;
        if (!string.IsNullOrEmpty(dto.ShiftEnd) &&
            TimeSpan.TryParse(dto.ShiftEnd, out var parsedEnd))
        {
            newEnd = parsedEnd;
        }

        // Застосовуємо дату + час, зберігаємо як UTC
        day.ShiftStart = DateTime.SpecifyKind(baseDate.Add(newStart), DateTimeKind.Utc);
        day.ShiftEnd = DateTime.SpecifyKind(baseDate.Add(newEnd), DateTimeKind.Utc);

        await _context.SaveChangesAsync();
        return Ok(day);
    }

    // --------------------------------------------------------
    // MOVE SCENE (Drag & Drop)
    // --------------------------------------------------------
    [HttpPut("project/{projectId}/move-scene")]
    public async Task<IActionResult> ReorderScenes(int projectId, [FromBody] ReorderSceneDto req)
    {
        var existingSchedule = await _context.SceneSchedules
            .FirstOrDefaultAsync(ss => ss.SceneId == req.SceneId);

        if (req.TargetShootDayId == null)
        {
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

        // Оновлюємо текстові поля
        if (!string.IsNullOrEmpty(dto.Unit)) day.UnitName = dto.Unit;
        if (!string.IsNullOrEmpty(dto.Status)) day.Status = dto.Status;
        if (dto.BaseLocationId.HasValue) day.BaseLocationId = dto.BaseLocationId;
        day.GeneralNotes = dto.GeneralNotes;

        // Оновлюємо дати і час
        DateTime newDate = day.ShiftStart.Date;
        if (!string.IsNullOrEmpty(dto.ShootDate) && DateTime.TryParse(dto.ShootDate, out var dateParsed))
        {
            newDate = dateParsed.Date;
        }

        TimeSpan startTime = day.ShiftStart.TimeOfDay;
        if (!string.IsNullOrEmpty(dto.ShiftStart) && TimeSpan.TryParse(dto.ShiftStart, out var startParsed))
        {
            startTime = startParsed;
        }

        TimeSpan endTime = day.ShiftEnd.TimeOfDay;
        if (!string.IsNullOrEmpty(dto.ShiftEnd) && TimeSpan.TryParse(dto.ShiftEnd, out var endParsed))
        {
            endTime = endParsed;
        }

        // Зберігаємо в UTC
        day.ShiftStart = DateTime.SpecifyKind(newDate.Add(startTime), DateTimeKind.Utc);
        day.ShiftEnd = DateTime.SpecifyKind(newDate.Add(endTime), DateTimeKind.Utc);

        await _context.SaveChangesAsync();
        return Ok(day);
    }

    // --------------------------------------------------------
    // ВИДАЛЕННЯ ДНЯ
    // --------------------------------------------------------
    [HttpDelete("project/{projectId}/shoot-day/{dayId}")]
    public async Task<IActionResult> DeleteShootDay(int projectId, int dayId)
    {
        var day = await _context.ShootDays
            .Include(sd => sd.SceneSchedules)
            .FirstOrDefaultAsync(sd => sd.Id == dayId && sd.ProjectId == projectId);

        if (day == null) return NotFound();

        _context.SceneSchedules.RemoveRange(day.SceneSchedules);
        _context.ShootDays.Remove(day);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // --------------------------------------------------------
    // AUTO-SCHEDULE — ПОВНІСТЮ ПЕРЕПИСАНИЙ АЛГОРИТМ
    // --------------------------------------------------------
    [HttpPost("project/{projectId}/auto-schedule")]
    public async Task<ActionResult<AutoScheduleResultDto>> AutoSchedule(
        int projectId, [FromBody] AutoScheduleRequestDto req)
    {
        var result = new AutoScheduleResultDto();

        // 1. Незаплановані сцени з усіма зв'язками
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

        // 2. Попереджаємо про сцени без локації (але не блокуємо розклад)
        var scenesWithoutLocation = unscheduledScenes
            .Where(s => GetPrimaryLocationId(s) == null)
            .ToList();

        // 3. Формуємо список цільових днів
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

            // ВИПРАВЛЕННЯ: у fill-режимі теж помічаємо дні як "generated"
            // щоб фронтенд показував кнопки Accept/Reject
            foreach (var day in targetDays)
            {
                if (day.Status == "draft")
                    day.Status = "generated";
            }
            await _context.SaveChangesAsync();
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

            // Shoot time = screen time * ShootingRatio
            double totalShootMinutes = unscheduledScenes
                .Sum(s => GetShootMinutes(s) + req.BufferMinutes);

            // Рахуємо точно потрібну кількість днів (без зайвого +1)
            int daysNeeded = Math.Max(1,
                (int)Math.Ceiling(totalShootMinutes / Math.Min(effectiveShiftMinutes, req.MaxShiftMinutes)));

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

            await _context.SaveChangesAsync();
        }

        // 4. Сортування сцен: спочатку groupBy, потім сцени без локації — в кінець
        IEnumerable<Scene> sortedScenes;

        if (req.GroupBy == "location")
        {
            // Сцени з локацією — групуємо по локації (найбільші групи першими)
            // Сцени без локації — в кінець
            var withLocation = unscheduledScenes
                .Where(s => GetPrimaryLocationId(s) != null)
                .GroupBy(s => GetPrimaryLocationId(s))
                .OrderByDescending(g => g.Count())
                .SelectMany(g => g);

            var withoutLocation = unscheduledScenes
                .Where(s => GetPrimaryLocationId(s) == null)
                .OrderBy(s => s.SequenceNum);

            sortedScenes = withLocation.Concat(withoutLocation);
        }
        else
        {
            // sequence order — сцени без локації теж у своєму порядку
            sortedScenes = unscheduledScenes.OrderBy(s => s.SequenceNum);
        }

        // 5. Алгоритм розподілу — враховує shoot time
        double maxShiftMinutes = req.MaxShiftMinutes > 0 ? req.MaxShiftMinutes : 600;

        // Словник: dayId -> вже використані хвилини (shoot time)
        var usedMinutesPerDay = new Dictionary<int, double>();
        // Словник: dayId -> наступний SceneOrder
        var nextOrderPerDay = new Dictionary<int, int>();

        foreach (var day in targetDays)
        {
            double alreadyUsed = 0;
            if (req.Mode == "fill" && day.SceneSchedules?.Any() == true)
            {
                alreadyUsed = day.SceneSchedules
                    .Where(ss => ss.Scene != null)
                    .Sum(ss => GetShootMinutes(ss.Scene));
            }
            usedMinutesPerDay[day.Id] = alreadyUsed;
            nextOrderPerDay[day.Id] = day.SceneSchedules?.Count ?? 0;
        }

        var newSchedules = new List<SceneSchedule>();
        int scheduledCount = 0;
        var unscheduledWarnings = new List<string>(); // сцени без локації

        foreach (var scene in sortedScenes)
        {
            double shootMins = GetShootMinutes(scene);
            bool hasLocation = GetPrimaryLocationId(scene) != null;

            // Якщо сцена без локації — додаємо попередження
            if (!hasLocation)
            {
                unscheduledWarnings.Add(
                    $"SC-{scene.SequenceNum:D3}: no location assigned (scheduled anyway)");
            }

            // Шукаємо перший день де є місце (враховуємо shoot time + buffer)
            ShootDay? foundDay = null;

            foreach (var day in targetDays)
            {
                double dayCapacity = Math.Min(
                    (day.ShiftEnd - day.ShiftStart).TotalMinutes,
                    maxShiftMinutes);

                double used = usedMinutesPerDay[day.Id];

                // Перший елемент дня — завжди приймаємо (навіть якщо сцена більша за зміну)
                bool dayIsEmpty = used == 0 && nextOrderPerDay[day.Id] == 0;
                bool fitsInDay = (used + shootMins + req.BufferMinutes) <= dayCapacity;

                if (fitsInDay || dayIsEmpty)
                {
                    foundDay = day;
                    break;
                }
            }

            if (foundDay == null) break; // більше немає місця в жодному дні

            newSchedules.Add(new SceneSchedule
            {
                SceneId = scene.Id,
                ShootDayId = foundDay.Id,
                SceneOrder = nextOrderPerDay[foundDay.Id]++
            });

            usedMinutesPerDay[foundDay.Id] += shootMins + req.BufferMinutes;
            scheduledCount++;
        }

        // 6. Зберігаємо
        _context.SceneSchedules.AddRange(newSchedules);
        await _context.SaveChangesAsync();

        // 7. Видаляємо порожні згенеровані дні (які залишилися без сцен)
        var usedDayIds = newSchedules.Select(ss => ss.ShootDayId).Distinct().ToHashSet();
        var emptyGeneratedDays = targetDays
            .Where(d => !usedDayIds.Contains(d.Id) && d.Status == "generated")
            .ToList();

        if (emptyGeneratedDays.Any())
        {
            _context.ShootDays.RemoveRange(emptyGeneratedDays);
            await _context.SaveChangesAsync();
        }

        // 8. Формуємо відповідь
        result.TotalScenesScheduled = scheduledCount;
        result.UnscheduledCount = unscheduledScenes.Count - scheduledCount;

        string warningNote = unscheduledWarnings.Any()
            ? $" ⚠ {unscheduledWarnings.Count} scene(s) have no location and were placed by sequence."
            : "";

        result.Message = $"Scheduled {scheduledCount} scenes across {usedDayIds.Count} day(s).{warningNote}";

        foreach (var day in targetDays.Where(d => usedDayIds.Contains(d.Id)))
        {
            double usedMins = usedMinutesPerDay[day.Id];
            double dayCapacity = (day.ShiftEnd - day.ShiftStart).TotalMinutes;

            result.GeneratedDays.Add(new AutoScheduleDayPreviewDto
            {
                ShootDayId = day.Id,
                Date = day.ShiftStart.ToString("MMM dd", CultureInfo.InvariantCulture),
                ShootDateIso = day.ShiftStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                IsNewlyCreated = day.GeneralNotes?.Contains("Auto-generated") == true,
                AssignedSceneIds = newSchedules
                    .Where(ss => ss.ShootDayId == day.Id)
                    .Select(ss => ss.SceneId)
                    .ToList(),
                CapacityStr = $"{FormatMinutes(usedMins)} / {FormatMinutes(dayCapacity)}",
                CapacityPct = dayCapacity > 0
                    ? Math.Min(100, (int)(usedMins / dayCapacity * 100)) : 0
            });
        }

        return Ok(result);
    }

    // --------------------------------------------------------
    // CONFIRM / REJECT GENERATED DAY
    // --------------------------------------------------------
    [HttpPost("project/{projectId}/confirm-day")]
    public async Task<IActionResult> ConfirmDay(int projectId, [FromBody] ConfirmDayDto dto)
    {
        var day = await _context.ShootDays
            .Include(sd => sd.SceneSchedules)
            .FirstOrDefaultAsync(sd => sd.Id == dto.ShootDayId && sd.ProjectId == projectId);

        if (day == null) return NotFound();

        if (dto.Confirm)
        {
            day.Status = "draft";
            day.GeneralNotes = day.GeneralNotes?
                .Replace("Auto-generated. Review and confirm.", "")
                .Trim();
        }
        else
        {
            _context.SceneSchedules.RemoveRange(day.SceneSchedules);
            _context.ShootDays.Remove(day);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // --------------------------------------------------------
    // CONFIRM ALL GENERATED
    // --------------------------------------------------------
    [HttpPost("project/{projectId}/confirm-all-generated")]
    public async Task<IActionResult> ConfirmAllGenerated(int projectId)
    {
        var generatedDays = await _context.ShootDays
            .Where(sd => sd.ProjectId == projectId && sd.Status == "generated")
            .ToListAsync();

        foreach (var day in generatedDays)
        {
            day.Status = "draft";
            day.GeneralNotes = day.GeneralNotes?
                .Replace("Auto-generated. Review and confirm.", "")
                .Trim();
        }

        await _context.SaveChangesAsync();
        return Ok(new { confirmed = generatedDays.Count });
    }

    // --------------------------------------------------------
    // ДОПОМІЖНІ МЕТОДИ
    // --------------------------------------------------------

    /// <summary>
    /// Повертає час зйомки сцени в хвилинах.
    /// Shoot time = screen time × ShootingRatio (6x).
    /// Мінімум DefaultScreenMinutes якщо duration не задано.
    /// </summary>
    private static double GetShootMinutes(Scene scene)
    {
        double screenMinutes = scene.EstimatedDuration.HasValue
            ? scene.EstimatedDuration.Value.TotalMinutes
            : DefaultScreenMinutes;

        return screenMinutes * ShootingRatio;
    }

    private static int? GetPrimaryLocationId(Scene scene)
    {
        return scene.SceneResources
            .Where(sr => sr.Resource?.Location != null)
            .Select(sr => (int?)sr.Resource.Location.Id)
            .FirstOrDefault();
    }

    private static string FormatMinutes(double totalMinutes)
    {
        int h = (int)(totalMinutes / 60);
        int m = (int)(totalMinutes % 60);
        return h > 0 ? $"{h}h {m}m" : $"{m}m";
    }

    private PlannerSceneDto MapToPlannerSceneDto(Scene scene, int order)
    {
        // TimeOfDay: парсимо зі Slugline "INT. LOCATION - DAY/NIGHT/DUSK/DAWN"
        string timeOfDay = ParseTimeOfDay(scene.SluglineText);

        // Duration: відображаємо screen time (для довідки на картці)
        string durationStr = "–";
        if (scene.EstimatedDuration.HasValue)
        {
            var dur = scene.EstimatedDuration.Value;
            if (dur.TotalSeconds < 1)
                durationStr = "< 1s";
            else if (dur.Hours > 0)
                durationStr = $"{dur.Hours}h {dur.Minutes}m";
            else if (dur.Minutes > 0)
                durationStr = $"{dur.Minutes}m {dur.Seconds}s".TrimEnd('s').TrimEnd(' ').TrimEnd('0').TrimEnd('m').TrimEnd(' ')
                    + (dur.Seconds > 0 ? $"{dur.Minutes}m {dur.Seconds}s" : $"{dur.Minutes}m");
            else
                durationStr = $"{dur.Seconds}s";

            // Спрощений варіант
            durationStr = FormatDuration(dur);
        }

        // Location: спочатку з resources, fallback — зі Slugline
        string locationName = scene.SceneResources
            .Where(sr => sr.Resource?.Location != null)
            .Select(sr => sr.Resource.Location.LocationName)
            .FirstOrDefault() ?? "";

        if (string.IsNullOrEmpty(locationName))
        {
            locationName = ParseLocationFromSlugline(scene.SluglineText);
        }

        // Cast
        var uniqueRoles = scene.ScriptElements
            .Where(se => se.Role != null && !string.IsNullOrEmpty(se.Role.RoleName))
            .Select(se => se.Role)
            .DistinctBy(r => r!.Id)
            .ToList();

        var castList = uniqueRoles.Select(r => GetInitials(r!.RoleName)).ToList();
        var castColors = uniqueRoles
            .Select(r => string.IsNullOrEmpty(r!.ColorHex) ? "#3AB9A0" : r.ColorHex)
            .ToList();
        var roleIds = uniqueRoles.Select(r => r!.Id).ToList();

        // Shoot time для підказки
        double shootMins = scene.EstimatedDuration.HasValue
            ? scene.EstimatedDuration.Value.TotalMinutes * ShootingRatio
            : DefaultScreenMinutes * ShootingRatio;

        bool hasLocation = scene.SceneResources.Any(sr => sr.Resource?.Location != null);

        return new PlannerSceneDto
        {
            Id = scene.Id,
            DisplayId = $"SC-{scene.SequenceNum:D3}",
            Title = string.IsNullOrEmpty(scene.SluglineText) ? "Untitled Scene" : scene.SluglineText,
            Duration = durationStr,
            // Shoot time для відображення на картці (замість screen time)
            ShootDuration = $"~{FormatMinutes(shootMins)} shoot",
            TimeOfDay = timeOfDay,
            Location = hasLocation ? locationName : (string.IsNullOrEmpty(locationName) ? "⚠ No location" : $"⚠ {locationName}"),
            HasLocationResource = hasLocation,
            Cast = castList,
            CastColors = castColors,
            RoleIds = roleIds,
            Order = order
        };
    }

    private static string ParseTimeOfDay(string? slugline)
    {
        if (string.IsNullOrEmpty(slugline)) return "N/A";

        var upper = slugline.ToUpperInvariant();

        // Шукаємо стандартні позначення після останнього дефісу
        var lastDash = upper.LastIndexOf('-');
        if (lastDash >= 0)
        {
            var afterDash = upper.Substring(lastDash + 1).Trim();
            // Може містити додатковий текст, беремо перше слово
            var firstWord = afterDash.Split(' ')[0].Trim();

            return firstWord switch
            {
                "DAY" => "DAY",
                "NIGHT" => "NIGHT",
                "DUSK" => "DUSK",
                "DAWN" => "DAWN",
                "MORNING" => "MORN",
                "EVENING" => "EVE",
                "CONTINUOUS" => "CONT",
                "LATER" => "LATER",
                _ => firstWord.Length > 6 ? firstWord[..6] : firstWord
            };
        }

        // Fallback
        if (upper.Contains("NIGHT")) return "NIGHT";
        if (upper.Contains("DAY")) return "DAY";
        if (upper.Contains("DUSK")) return "DUSK";
        if (upper.Contains("DAWN")) return "DAWN";

        return "N/A";
    }

    private static string ParseLocationFromSlugline(string? slugline)
    {
        if (string.IsNullOrEmpty(slugline)) return "Unspecified";

        // Формат: "INT./EXT. LOCATION NAME - TIME"
        var dashIdx = slugline.IndexOf('-');
        var locPart = dashIdx > 0 ? slugline[..dashIdx].Trim() : slugline.Trim();

        // Прибираємо INT./EXT. префікс
        foreach (var prefix in new[] { "INT.", "EXT.", "INT/EXT.", "EXT/INT." })
        {
            if (locPart.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                locPart = locPart[prefix.Length..].Trim();
                break;
            }
        }

        return string.IsNullOrEmpty(locPart) ? "Unspecified" : locPart;
    }

    private static string FormatDuration(TimeSpan dur)
    {
        if (dur.TotalSeconds < 1) return "< 1s";
        if (dur.Hours > 0) return $"{dur.Hours}h {dur.Minutes}m";
        if (dur.Minutes > 0 && dur.Seconds > 0) return $"{dur.Minutes}m {dur.Seconds}s";
        if (dur.Minutes > 0) return $"{dur.Minutes}m";
        return $"{dur.Seconds}s";
    }

    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1)
            return words[0].Length >= 2 ? words[0][..2].ToUpper() : words[0].ToUpper();
        return (words[0][..1] + words[1][..1]).ToUpper();
    }
}