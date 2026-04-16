using System;
using System.Collections.Generic;

namespace CineCoreBack.Models;

public partial class ShootDay
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string UnitName { get; set; } = null!;

    public DateTime ShiftStart { get; set; }

    public DateTime ShiftEnd { get; set; }

    public int? BaseLocationId { get; set; }

    public string? GeneralNotes { get; set; }

    public virtual Location? BaseLocation { get; set; }

    public virtual ICollection<CallSheet> CallSheets { get; set; } = new List<CallSheet>();

    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<SceneSchedule> SceneSchedules { get; set; } = new List<SceneSchedule>();
}
