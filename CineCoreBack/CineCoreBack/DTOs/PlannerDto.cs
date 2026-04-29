using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CineCoreBack.DTOs;

public class PlannerBoardDto
{
    public List<PlannerSceneDto> ScenePool { get; set; } = new();
    public List<PlannerShootDayDto> ShootDays { get; set; } = new();
}

public class PlannerShootDayDto
{
    public int Id { get; set; }
    public string Date { get; set; } = null!;
    public string ShootDateIso { get; set; } = null!;
    public string ShiftStartTime { get; set; } = null!;
    public string ShiftEndTime { get; set; } = null!;
    public int? BaseLocationId { get; set; }
    public string Unit { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string CallTime { get; set; } = null!;
    public string? Notes { get; set; }

    public string CapacityStr { get; set; } = null!;
    public int CapacityPct { get; set; }

    public List<PlannerSceneDto> Scenes { get; set; } = new();
}

public class PlannerSceneDto
{
    public int Id { get; set; }
    public string DisplayId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Duration { get; set; } = null!;
    public string ShootDuration { get; set; } = null!;
    public string Location { get; set; } = "TBD";
    public bool HasLocationResource { get; set; }
    public string TimeOfDay { get; set; } = "N/A";
    public List<string> Cast { get; set; } = new();
    public List<string> CastColors { get; set; } = new();
    public List<int> RoleIds { get; set; } = new();
    public int Order { get; set; }
}

public class CreateShootDayDto
{
    [Required(ErrorMessage = "Unit name is required")]
    public string Unit { get; set; } = null!;

    [Required(ErrorMessage = "Shoot date is required")]
    public string ShootDate { get; set; } = null!;

    [Required]
    [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Invalid Shift Start time format (HH:mm)")]
    public string ShiftStart { get; set; } = null!;

    [Required]
    [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Invalid Shift End time format (HH:mm)")]
    public string ShiftEnd { get; set; } = null!;

    public int? BaseLocationId { get; set; }
    public string? Notes { get; set; }
}

public class ReorderSceneDto
{
    public int SceneId { get; set; }
    public int? TargetShootDayId { get; set; }
    public int NewIndex { get; set; }
}

public class UpdateShootDayDto
{
    public string? Unit { get; set; }
    public string? ShootDate { get; set; }

    [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Invalid time format")]
    public string? ShiftStart { get; set; }

    [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Invalid time format")]
    public string? ShiftEnd { get; set; }

    public int? BaseLocationId { get; set; }
    public string? GeneralNotes { get; set; }
    public string? Status { get; set; }
}

public class AutoScheduleRequestDto
{
    [Required]
    public string Mode { get; set; } = "fill";

    public string? StartDate { get; set; }

    [Range(1, 1440, ErrorMessage = "Max shift minutes must be between 1 and 1440")]
    public int MaxShiftMinutes { get; set; } = 600;

    [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$")]
    public string DefaultShiftStart { get; set; } = "09:00";

    [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$")]
    public string DefaultShiftEnd { get; set; } = "19:00";

    public bool SkipWeekends { get; set; } = true;

    [Range(0, 300)]
    public int BufferMinutes { get; set; } = 15;

    public string GroupBy { get; set; } = "location";

    [Range(0, 300)]
    public int SetupMinutes { get; set; } = 30;

    [Range(0, 300)]
    public int LocationSwitchMinutes { get; set; } = 20;
}

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

public class ConfirmDayDto
{
    public int ShootDayId { get; set; }
    public bool Confirm { get; set; }
}