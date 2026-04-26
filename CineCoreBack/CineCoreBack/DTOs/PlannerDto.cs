using System;
using System.Collections.Generic;

namespace CineCoreBack.DTOs;

// Головний об'єкт дошки планувальника
public class PlannerBoardDto
{
    public List<PlannerSceneDto> ScenePool { get; set; } = new();
    public List<PlannerShootDayDto> ShootDays { get; set; } = new();
}

// DTO для Знімального Дня (Колонка на дошці)
public class PlannerShootDayDto
{
    public int Id { get; set; }
    public string Date { get; set; } = null!;           // "Mar 24" — для відображення
    public string ShootDateIso { get; set; } = null!;   // "YYYY-MM-DD" — для форми редагування
    public string ShiftStartTime { get; set; } = null!; // "HH:mm"
    public string ShiftEndTime { get; set; } = null!;   // "HH:mm"
    public int? BaseLocationId { get; set; }
    public string Unit { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string CallTime { get; set; } = null!;
    public string? Notes { get; set; }

    // Capacity — розраховується на основі shoot time
    public string CapacityStr { get; set; } = null!;
    public int CapacityPct { get; set; }

    public List<PlannerSceneDto> Scenes { get; set; } = new();
}

// DTO для Сцени (Картка)
public class PlannerSceneDto
{
    public int Id { get; set; }
    public string DisplayId { get; set; } = null!;   // "SC-005"
    public string Title { get; set; } = null!;

    // Екранна тривалість (для довідки): "2m 30s"
    public string Duration { get; set; } = null!;

    // Приблизний час зйомки з урахуванням shooting ratio (~6x): "~15m shoot"
    public string ShootDuration { get; set; } = null!;

    // Локація з текстовим попередженням якщо не прив'язана до ресурсу
    public string Location { get; set; } = "TBD";

    // true — локація є в Resources; false — лише з Slugline або взагалі відсутня
    public bool HasLocationResource { get; set; }

    // DAY / NIGHT / DUSK / DAWN / N/A — з Slugline
    public string TimeOfDay { get; set; } = "N/A";

    public List<string> Cast { get; set; } = new();
    public List<string> CastColors { get; set; } = new();
    public List<int> RoleIds { get; set; } = new();
    public int Order { get; set; }
}

// DTO для створення нового дня
public class CreateShootDayDto
{
    public string Unit { get; set; } = null!;
    public string ShootDate { get; set; } = null!;  // YYYY-MM-DD
    public string CallTime { get; set; } = null!;   // HH:MM
    public string ShiftStart { get; set; } = null!; // HH:MM
    public string ShiftEnd { get; set; } = null!;   // HH:MM
    public int? BaseLocationId { get; set; }
    public string? Notes { get; set; }
}

// DTO для оновлення розкладу після Drag & Drop
public class ReorderSceneDto
{
    public int SceneId { get; set; }
    public int? TargetShootDayId { get; set; } // null = повернули в Pool
    public int NewIndex { get; set; }
}

// DTO для редагування дня
public class UpdateShootDayDto
{
    public string? Unit { get; set; }
    public string? ShootDate { get; set; }   // YYYY-MM-DD
    public string? CallTime { get; set; }
    public string? ShiftStart { get; set; }  // HH:MM
    public string? ShiftEnd { get; set; }    // HH:MM
    public int? BaseLocationId { get; set; }
    public string? GeneralNotes { get; set; }
    public string? Status { get; set; }
}

// DTO для запиту Auto-Schedule
public class AutoScheduleRequestDto
{
    // "fill" — розподілити по існуючих днях | "generate" — створити нові
    public string Mode { get; set; } = "fill";

    // Для режиму "generate" — дата початку
    public string? StartDate { get; set; }

    // Максимальна тривалість зміни в хвилинах
    public int MaxShiftMinutes { get; set; } = 600;

    // Час початку зміни для нових днів
    public string DefaultShiftStart { get; set; } = "09:00";

    // Час кінця зміни для нових днів
    public string DefaultShiftEnd { get; set; } = "19:00";

    // Пропускати вихідні
    public bool SkipWeekends { get; set; } = true;

    // Буфер між сценами (хвилини)
    public int BufferMinutes { get; set; } = 15;

    // Пріоритет: "location" або "sequence"
    public string GroupBy { get; set; } = "location";
}

// Результат Auto-Schedule
public class AutoScheduleResultDto
{
    public List<AutoScheduleDayPreviewDto> GeneratedDays { get; set; } = new();
    public int TotalScenesScheduled { get; set; }
    public int UnscheduledCount { get; set; }
    public string Message { get; set; } = "";
}

public class AutoScheduleDayPreviewDto
{
    public int ShootDayId { get; set; }
    public string Date { get; set; } = "";
    public string ShootDateIso { get; set; } = "";
    public bool IsNewlyCreated { get; set; }
    public List<int> AssignedSceneIds { get; set; } = new();
    public string CapacityStr { get; set; } = "";
    public int CapacityPct { get; set; }
}

// DTO для підтвердження/відхилення дня
public class ConfirmDayDto
{
    public int ShootDayId { get; set; }
    public bool Confirm { get; set; }
}