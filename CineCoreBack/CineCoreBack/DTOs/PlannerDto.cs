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
    public string Date { get; set; } = null!;         // "Mar 24" — для відображення на картці (InvariantCulture)
    public string ShootDateIso { get; set; } = null!;  // "YYYY-MM-DD" — для pre-fill форми редагування
    public string ShiftStartTime { get; set; } = null!; // "HH:mm" — для поля Shift Start у формі
    public string ShiftEndTime { get; set; } = null!;   // "HH:mm" — для поля Shift End у формі
    public int? BaseLocationId { get; set; }            // для pre-select локації у формі
    public string Unit { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string CallTime { get; set; } = null!;
    public string? Notes { get; set; }

    // Для прогрес-бару на фронті
    public string CapacityStr { get; set; } = null!;
    public int CapacityPct { get; set; }

    public List<PlannerSceneDto> Scenes { get; set; } = new();
}

// DTO для Сцени (Картка, яку перетягують)
public class PlannerSceneDto
{
    public int Id { get; set; } // Внутрішній ID БД
    public string DisplayId { get; set; } = null!; // Напр. "SC-005" або номер сцени
    public string Title { get; set; } = null!;
    public string Duration { get; set; } = null!;
    public string Location { get; set; } = "TBD";
    public string TimeOfDay { get; set; } = "INT/DAY"; // Зазвичай береться з Slugline
    public List<string> Cast { get; set; } = new(); // Ініціали ролей
    public List<string> CastColors { get; set; } = new(); // HEX-кольори ролей (паралельний список)
    public List<int> RoleIds { get; set; } = new();
    public int Order { get; set; } // Порядок у дні
}

// DTO для створення нового дня з модалки
public class CreateShootDayDto
{
    public string Unit { get; set; } = null!;
    public string ShootDate { get; set; } = null!; // YYYY-MM-DD
    public string CallTime { get; set; } = null!; // HH:MM
    public string ShiftStart { get; set; } = null!; // HH:MM
    public string ShiftEnd { get; set; } = null!; // HH:MM
    public int? BaseLocationId { get; set; }
    public string? Notes { get; set; }
}

// DTO для оновлення розкладу після Drag & Drop
public class ReorderSceneDto
{
    public int SceneId { get; set; }
    public int? TargetShootDayId { get; set; } // null, якщо сцену повернули в Pool
    public int NewIndex { get; set; }
}

public class UpdateShootDayDto
{
    public string? Unit { get; set; }
    public string? ShootDate { get; set; } // YYYY-MM-DD
    public string? CallTime { get; set; }
    public string? ShiftStart { get; set; }
    public string? ShiftEnd { get; set; }
    public int? BaseLocationId { get; set; }
    public string? GeneralNotes { get; set; }
    public string? Status { get; set; } // Для переведення з draft у published
}

// DTO для запиту Auto-Schedule
public class AutoScheduleRequestDto
{
    // Режим: "fill" — заповнити наявні дні, "generate" — створити нові
    public string Mode { get; set; } = "fill";

    // Для режиму "generate" — з якої дати починати
    public string? StartDate { get; set; }

    // Макс. тривалість зміни в хвилинах (напр. 600 = 10 годин)
    public int MaxShiftMinutes { get; set; } = 600;

    // Час початку зміни для нових днів (напр. "09:00")
    public string DefaultShiftStart { get; set; } = "09:00";

    // Час кінця зміни для нових днів
    public string DefaultShiftEnd { get; set; } = "19:00";

    // Чи пропускати вихідні при генерації нових днів
    public bool SkipWeekends { get; set; } = true;

    // Залишати буфер між сценами (в хвилинах)
    public int BufferMinutes { get; set; } = 15;

    // Пріоритет групування: "location" або "sequence"
    public string GroupBy { get; set; } = "location";
}

// Результат Auto-Schedule — повертається одразу на фронтенд для preview
public class AutoScheduleResultDto
{
    public List<AutoScheduleDayPreviewDto> GeneratedDays { get; set; } = new();
    public int TotalScenesScheduled { get; set; }
    public int UnscheduledCount { get; set; }
    public string Message { get; set; } = "";
}

public class AutoScheduleDayPreviewDto
{
    public int ShootDayId { get; set; }       // ID щойно створеного/існуючого дня
    public string Date { get; set; } = "";
    public string ShootDateIso { get; set; } = "";
    public bool IsNewlyCreated { get; set; }  // true = щойно згенеровано, false = вже існував
    public List<int> AssignedSceneIds { get; set; } = new();
    public string CapacityStr { get; set; } = "";
    public int CapacityPct { get; set; }
}

// DTO для підтвердження/відхилення конкретного дня
public class ConfirmDayDto
{
    public int ShootDayId { get; set; }
    public bool Confirm { get; set; } // true = підтвердити (draft), false = відхилити (видалити)
}