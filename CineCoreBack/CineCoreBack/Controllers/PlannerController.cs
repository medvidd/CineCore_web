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
    // UPDATE SHOOT DAY
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
    // AUTO-SCHEDULE (Оригінальний каркас + Актори)
    // --------------------------------------------------------
    [HttpPost("project/{projectId}/auto-schedule")]
    public async Task<ActionResult<AutoScheduleResultDto>> AutoSchedule(
        int projectId, [FromBody] AutoScheduleRequestDto req)
    {
        var result = new AutoScheduleResultDto();

        // 1. Завантажуємо всі незаплановані сцени з усіма зв'язками
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

        // 2. Розділяємо сцени: з локацією та без неї
        var scenesWithLocation = unscheduledScenes
            .Where(s => GetPrimaryLocationId(s) != null)
            .ToList();

        var scenesWithoutLocation = unscheduledScenes
            .Where(s => GetPrimaryLocationId(s) == null)
            .ToList();

        if (!scenesWithLocation.Any())
        {
            result.Message = "No scenes with assigned location resources found. " +
                             "Assign locations to scenes before auto-scheduling.";
            result.UnscheduledCount = unscheduledScenes.Count;
            return Ok(result);
        }

        // 3. Формуємо список цільових знімальних днів
        List<ShootDay> targetDays;

        if (req.Mode == "fill")
        {
            targetDays = await _context.ShootDays
                .Where(sd => sd.ProjectId == projectId &&
                             (sd.Status == "draft" || sd.Status == "generated"))
                .Include(sd => sd.SceneSchedules)
                    .ThenInclude(ss => ss.Scene)
                        .ThenInclude(s => s.SceneResources)
                            .ThenInclude(sr => sr.Resource)
                                .ThenInclude(r => r.Location)
                // ДОДАНО: Завантажуємо ролі для вже існуючих сцен, щоб враховувати їх при перетині акторів
                .Include(sd => sd.SceneSchedules)
                    .ThenInclude(ss => ss.Scene)
                        .ThenInclude(s => s.ScriptElements)
                            .ThenInclude(se => se.Role)
                .OrderBy(sd => sd.ShiftStart)
                .ToListAsync();

            if (!targetDays.Any())
            {
                result.Message = "No available shoot days found. Use 'Generate' mode to create new days.";
                return Ok(result);
            }

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

            double totalShootMinutes = scenesWithLocation
                .Sum(s => GetShootMinutes(s) + req.BufferMinutes);

            double usableMinutesPerDay = Math.Min(effectiveShiftMinutes, req.MaxShiftMinutes) - req.SetupMinutes;
            if (usableMinutesPerDay <= 0) usableMinutesPerDay = effectiveShiftMinutes * 0.8;

            int daysNeeded = Math.Max(1, (int)Math.Ceiling(totalShootMinutes / usableMinutesPerDay));

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

        // 4. ОРИГІНАЛЬНЕ СОРТУВАННЯ: Зберігаємо сценарний порядок (Continuity)
        IEnumerable<Scene> sortedScenes;

        if (req.GroupBy == "location")
        {
            sortedScenes = scenesWithLocation
                .GroupBy(s => GetPrimaryLocationId(s))
                .OrderByDescending(g => g.Sum(s => GetShootMinutes(s)))
                .SelectMany(g => g.OrderBy(s => s.SequenceNum)); // Жодного сортування за розміром! Тільки порядок.
        }
        else
        {
            sortedScenes = scenesWithLocation.OrderBy(s => s.SequenceNum);
        }

        // 5. Ініціалізація стану кожного дня перед розподілом
        double maxShiftMinutes = req.MaxShiftMinutes > 0 ? req.MaxShiftMinutes : 600;

        var usedMinutesPerDay = new Dictionary<int, double>();
        var nextOrderPerDay = new Dictionary<int, int>();
        var lastLocationPerDay = new Dictionary<int, int?>();

        foreach (var day in targetDays)
        {
            double alreadyUsed = req.SetupMinutes;
            int? lastLocId = null;
            int existingCount = 0;

            if (req.Mode == "fill" && day.SceneSchedules?.Any() == true)
            {
                foreach (var ss in day.SceneSchedules.OrderBy(x => x.SceneOrder))
                {
                    if (ss.Scene == null) continue;
                    alreadyUsed += GetShootMinutes(ss.Scene) + req.BufferMinutes;

                    int? sceneLoc = GetPrimaryLocationId(ss.Scene);
                    if (lastLocId.HasValue && sceneLoc.HasValue && lastLocId.Value != sceneLoc.Value)
                        alreadyUsed += req.LocationSwitchMinutes;

                    lastLocId = sceneLoc;
                }
                existingCount = day.SceneSchedules.Count;
            }

            usedMinutesPerDay[day.Id] = alreadyUsed;
            nextOrderPerDay[day.Id] = existingCount;
            lastLocationPerDay[day.Id] = lastLocId;
        }

        // 6. ОСНОВНИЙ АЛГОРИТМ РОЗПОДІЛУ З УРАХУВАННЯМ АКТОРІВ
        var newSchedules = new List<SceneSchedule>();
        int scheduledCount = 0;

        foreach (var scene in sortedScenes)
        {
            double shootMins = GetShootMinutes(scene);
            int? sceneLocId = GetPrimaryLocationId(scene);

            // Отримуємо список ID ролей (акторів) для поточної сцени
            var sceneRoleIds = scene.ScriptElements
                .Where(se => se.Role != null)
                .Select(se => se.Role!.Id)
                .ToList();

            IEnumerable<ShootDay> daySearchOrder;

            if (req.GroupBy == "location" && sceneLocId.HasValue)
            {
                var preferredDays = targetDays
                    .Where(d => lastLocationPerDay[d.Id] == sceneLocId.Value
                                || nextOrderPerDay[d.Id] == 0);
                var otherDays = targetDays
                    .Where(d => lastLocationPerDay[d.Id] != sceneLocId.Value
                                && nextOrderPerDay[d.Id] > 0);

                daySearchOrder = preferredDays
                    .OrderBy(d => nextOrderPerDay[d.Id] == 0 ? 1 : 0)
                    .ThenBy(d => d.ShiftStart)
                    .Concat(otherDays.OrderBy(d => d.ShiftStart));
            }
            else
            {
                daySearchOrder = targetDays.OrderBy(d => d.ShiftStart);
            }

            // ЗМІНА: Замість foundDay шукаємо bestDay за кількістю спільних акторів
            ShootDay? bestDay = null;
            int maxActorOverlap = -1;

            foreach (var day in daySearchOrder)
            {
                double dayCapacity = Math.Min(
                    (day.ShiftEnd - day.ShiftStart).TotalMinutes,
                    maxShiftMinutes);

                double used = usedMinutesPerDay[day.Id];
                int? dayLastLocId = lastLocationPerDay[day.Id];

                double locationSwitchCost = 0;
                bool isFirstSceneInDay = nextOrderPerDay[day.Id] == 0;

                if (!isFirstSceneInDay
                    && dayLastLocId.HasValue
                    && sceneLocId.HasValue
                    && dayLastLocId.Value != sceneLocId.Value)
                {
                    locationSwitchCost = req.LocationSwitchMinutes;
                }

                double totalSceneCost = shootMins + req.BufferMinutes + locationSwitchCost;
                bool fitsInDay = (used + totalSceneCost) <= dayCapacity;

                if (fitsInDay || isFirstSceneInDay)
                {
                    // РАХУЄМО ПЕРЕТИН АКТОРІВ
                    var dayActors = new HashSet<int>();

                    // 1. Актори з уже існуючих у дні сцен (для fill-режиму)
                    if (day.SceneSchedules != null)
                    {
                        foreach (var ss in day.SceneSchedules.Where(x => x.Scene != null && x.Scene.ScriptElements != null))
                        {
                            foreach (var se in ss.Scene.ScriptElements.Where(x => x.Role != null))
                            {
                                dayActors.Add(se.Role!.Id);
                            }
                        }
                    }

                    // 2. Актори зі сцен, які ми щойно додали в цей день під час поточної генерації
                    var newlyAddedScenesForDay = newSchedules
                        .Where(ss => ss.ShootDayId == day.Id)
                        .Select(ss => ss.SceneId)
                        .ToList();

                    var newActors = scenesWithLocation
                        .Where(s => newlyAddedScenesForDay.Contains(s.Id))
                        .SelectMany(s => s.ScriptElements)
                        .Where(se => se.Role != null)
                        .Select(se => se.Role!.Id);

                    foreach (var a in newActors) dayActors.Add(a);

                    // Кількість акторів поточної сцени, які ВЖЕ працюють у цей день
                    int actorOverlap = sceneRoleIds.Intersect(dayActors).Count();

                    // Якщо це найкращий збіг по акторах — запам'ятовуємо цей день
                    if (bestDay == null || actorOverlap > maxActorOverlap)
                    {
                        bestDay = day;
                        maxActorOverlap = actorOverlap;
                    }
                }
            }

            if (bestDay == null)
            {
                // Повертаємо оригінальний break. 
                // Якщо йдемо по порядку і сцена не влізла — зупиняємось, щоб не ламати послідовність.
                break;
            }

            // Додаємо сцену в НАЙКРАЩИЙ знайдений день
            newSchedules.Add(new SceneSchedule
            {
                SceneId = scene.Id,
                ShootDayId = bestDay.Id,
                SceneOrder = nextOrderPerDay[bestDay.Id]++
            });

            // Оновлюємо стан дня
            int? prevLocId = lastLocationPerDay[bestDay.Id];
            bool locSwitch = prevLocId.HasValue && sceneLocId.HasValue && prevLocId.Value != sceneLocId.Value
                             && nextOrderPerDay[bestDay.Id] > 1;

            usedMinutesPerDay[bestDay.Id] += shootMins + req.BufferMinutes
                + (locSwitch ? req.LocationSwitchMinutes : 0);

            lastLocationPerDay[bestDay.Id] = sceneLocId;
            scheduledCount++;
        }

        // 7. Зберігаємо нові записи розкладу
        _context.SceneSchedules.AddRange(newSchedules);
        await _context.SaveChangesAsync();

        // 8. GENERATE-режим: видаляємо порожні дні
        var usedDayIds = newSchedules.Select(ss => ss.ShootDayId).Distinct().ToHashSet();

        if (req.Mode == "generate")
        {
            var emptyGeneratedDays = targetDays
                .Where(d => !usedDayIds.Contains(d.Id) && d.Status == "generated")
                .ToList();

            if (emptyGeneratedDays.Any())
            {
                _context.ShootDays.RemoveRange(emptyGeneratedDays);
                await _context.SaveChangesAsync();
            }
        }

        // 9. Формуємо відповідь (як в оригіналі)
        result.TotalScenesScheduled = scheduledCount;

        int failedToSchedule = scenesWithLocation.Count - scheduledCount;
        result.UnscheduledCount = failedToSchedule + scenesWithoutLocation.Count;

        var messageParts = new List<string>();
        messageParts.Add($"Scheduled {scheduledCount} scene(s) across {usedDayIds.Count} day(s).");

        if (scenesWithoutLocation.Any())
        {
            messageParts.Add(
                $"⚠ {scenesWithoutLocation.Count} scene(s) skipped — no location resource assigned. " +
                "Assign a location in the Script module and run again.");
        }

        if (failedToSchedule > 0)
        {
            messageParts.Add(
                $"⚠ {failedToSchedule} scene(s) could not fit into available days. " +
                "Consider adding more shoot days or increasing shift duration.");
        }

        result.Message = string.Join(" ", messageParts);

        var daysForPreview = req.Mode == "fill"
            ? targetDays
            : targetDays.Where(d => usedDayIds.Contains(d.Id));

        foreach (var day in daysForPreview.OrderBy(d => d.ShiftStart))
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
    /// 
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
        string timeOfDay = ParseTimeOfDay(scene.SluglineText);

        string durationStr = "–";
        if (scene.EstimatedDuration.HasValue)
            durationStr = FormatDuration(scene.EstimatedDuration.Value);

        // Location: спочатку з resources, fallback — зі Slugline
        string locationName = scene.SceneResources
            .Where(sr => sr.Resource?.Location != null)
            .Select(sr => sr.Resource.Location.LocationName)
            .FirstOrDefault() ?? "";

        if (string.IsNullOrEmpty(locationName))
            locationName = ParseLocationFromSlugline(scene.SluglineText);

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
            ShootDuration = $"~{FormatMinutes(shootMins)} shoot",
            TimeOfDay = timeOfDay,
            Location = hasLocation
                ? locationName
                : (string.IsNullOrEmpty(locationName) ? "⚠ No location" : $"⚠ {locationName}"),
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
        var lastDash = upper.LastIndexOf('-');

        if (lastDash >= 0)
        {
            var afterDash = upper.Substring(lastDash + 1).Trim();
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

        if (upper.Contains("NIGHT")) return "NIGHT";
        if (upper.Contains("DAY")) return "DAY";
        if (upper.Contains("DUSK")) return "DUSK";
        if (upper.Contains("DAWN")) return "DAWN";

        return "N/A";
    }

    private static string ParseLocationFromSlugline(string? slugline)
    {
        if (string.IsNullOrEmpty(slugline)) return "Unspecified";

        var dashIdx = slugline.IndexOf('-');
        var locPart = dashIdx > 0 ? slugline[..dashIdx].Trim() : slugline.Trim();

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

    private static int? GetDominantLocationId(
    List<SceneSchedule> schedules,
    List<Scene> allScenes)
    {
        return schedules
            .Select(ss => allScenes.FirstOrDefault(s => s.Id == ss.SceneId))
            .Where(s => s != null)
            .GroupBy(s => GetPrimaryLocationId(s!))
            .Where(g => g.Key.HasValue)
            .OrderByDescending(g => g.Sum(s => GetShootMinutes(s!)))
            .Select(g => g.Key)
            .FirstOrDefault();
    }
}