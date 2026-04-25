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
    public string Date { get; set; } = null!; // Формат "MMM dd" (напр. "Mar 24")
    public string Unit { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string CallTime { get; set; } = null!;

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
    public List<string> Cast { get; set; } = new(); // Ініціали акторів або ролей
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
    public string? BaseLocation { get; set; }
    public string? Notes { get; set; }
}

// DTO для оновлення розкладу після Drag & Drop
public class ReorderSceneDto
{
    public int SceneId { get; set; }
    public int? ShootDayId { get; set; } // null, якщо сцену повернули в Pool
    public int NewOrder { get; set; }
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