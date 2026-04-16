using System;
using System.Collections.Generic;

namespace CineCoreBack.Models;

public partial class SceneSchedule
{
    public int Id { get; set; }

    public int ShootDayId { get; set; }

    public int SceneId { get; set; }

    public int SceneOrder { get; set; }

    public TimeOnly? ScheduledTime { get; set; }

    public TimeSpan? PrepTimeEstimate { get; set; }

    public TimeSpan? ShootTimeEstimate { get; set; }

    public virtual Scene Scene { get; set; } = null!;

    public virtual ShootDay ShootDay { get; set; } = null!;
}
