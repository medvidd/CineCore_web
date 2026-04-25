using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CineCoreBack.Models;
using CineCoreBack.DTOs;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

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
                Date = day.ShiftStart.ToString("MMM dd"),
                Unit = string.IsNullOrEmpty(day.UnitName) ? "MAIN UNIT" : day.UnitName.ToUpper(),
                Status = string.IsNullOrEmpty(day.Status) ? "draft" : day.Status,
                CallTime = day.ShiftStart.ToString("HH:mm"),
                CapacityStr = capacityStr,
                CapacityPct = capacityPct,
                Scenes = dayScenesDtos
            });
        }

        return Ok(board);
    }

    [HttpPost("project/{projectId}/shoot-day")] 
    public async Task<ActionResult<PlannerShootDayDto>> CreateShootDay(int projectId, [FromBody] CreateShootDayDto dto)
    {
        // Надійна валідація та парсинг часу з фронтенду
        if (!DateTime.TryParse(dto.ShootDate, out DateTime shootDate))
            return BadRequest("Invalid ShootDate format.");
        if (!TimeSpan.TryParse(dto.ShiftStart, out TimeSpan shiftStart))
            return BadRequest("Invalid ShiftStart format.");
        if (!TimeSpan.TryParse(dto.ShiftEnd, out TimeSpan shiftEnd))
            return BadRequest("Invalid ShiftEnd format.");

        var startDateTime = shootDate.Date.Add(shiftStart);
        var endDateTime = shootDate.Date.Add(shiftEnd);

        // Якщо кінець зміни менший за початок (нічна зміна), переносимо кінець на наступний день
        if (endDateTime <= startDateTime)
        {
            endDateTime = endDateTime.AddDays(1);
        }

        var newDay = new ShootDay
        {
            ProjectId = projectId,
            UnitName = dto.Unit,
            ShiftStart = startDateTime,
            ShiftEnd = endDateTime,
            GeneralNotes = dto.Notes,
            Status = "draft"
        };

        _context.ShootDays.Add(newDay);
        await _context.SaveChangesAsync();

        TimeSpan totalShiftDuration = newDay.ShiftEnd - newDay.ShiftStart;

        return Ok(new PlannerShootDayDto
        {
            Id = newDay.Id,
            Date = newDay.ShiftStart.ToString("MMM dd"),
            Unit = newDay.UnitName.ToUpper(),
            Status = newDay.Status,
            CallTime = newDay.ShiftStart.ToString("HH:mm"),
            CapacityStr = $"0h 0m / {(int)totalShiftDuration.TotalHours}h {totalShiftDuration.Minutes}m",
            CapacityPct = 0,
            Scenes = new List<PlannerSceneDto>()
        });
    }

    [HttpPut("project/{projectId}/move-scene")] 
    public async Task<IActionResult> ReorderScenes(int projectId, [FromBody] List<ReorderSceneDto> reorderRequests)
    {
        var sceneIds = reorderRequests.Select(r => r.SceneId).ToList();
        var existingSchedules = await _context.SceneSchedules
            .Where(ss => sceneIds.Contains(ss.SceneId))
            .ToListAsync();

        foreach (var req in reorderRequests)
        {
            var schedule = existingSchedules.FirstOrDefault(ss => ss.SceneId == req.SceneId);

            if (req.ShootDayId == null)
            {
                // Сцену викинули назад у Pool
                if (schedule != null)
                {
                    _context.SceneSchedules.Remove(schedule);
                }
            }
            else
            {
                if (schedule != null)
                {
                    // Оновлення існуючого запису
                    schedule.ShootDayId = req.ShootDayId.Value;
                    schedule.SceneOrder = req.NewOrder;
                    _context.Entry(schedule).State = EntityState.Modified;
                }
                else
                {
                    // Нове перетягування з Pool у день
                    _context.SceneSchedules.Add(new SceneSchedule
                    {
                        SceneId = req.SceneId,
                        ShootDayId = req.ShootDayId.Value,
                        SceneOrder = req.NewOrder
                    });
                }
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
        if (!string.IsNullOrEmpty(dto.ShootDate))
        {
            if (DateTime.TryParse(dto.ShootDate, out var date))
            {
                // Тут логіка оновлення дати в ShiftStart/End, зберігаючи час
                day.ShiftStart = date.Date.Add(day.ShiftStart.TimeOfDay);
                day.ShiftEnd = date.Date.Add(day.ShiftEnd.TimeOfDay);
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
            durationStr = dur.Hours > 0 ? $"{dur.Hours}h {dur.Minutes}m" : $"{dur.Minutes}m";
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

        // 4. Cast: Унікальні ролі з елементів сценарію
        var castList = scene.ScriptElements
            .Where(se => se.Role != null && !string.IsNullOrEmpty(se.Role.RoleName))
            .Select(se => se.Role.RoleName)
            .Distinct()
            .Select(GetInitials)
            .ToList();

        return new PlannerSceneDto
        {
            Id = scene.Id,
            DisplayId = $"SC-{(scene.SequenceNum):D3}",
            Title = string.IsNullOrEmpty(scene.SluglineText) ? "Untitled Scene" : scene.SluglineText,
            Duration = durationStr,
            TimeOfDay = timeOfDay,
            Location = locationName,
            Cast = castList,
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
}