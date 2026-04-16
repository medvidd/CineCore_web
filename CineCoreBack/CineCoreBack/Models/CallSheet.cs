using System;
using System.Collections.Generic;

namespace CineCoreBack.Models;

public partial class CallSheet
{
    public int Id { get; set; }

    public int ShootDayId { get; set; }

    public int VersionNum { get; set; }

    public DateTime? PublishedAt { get; set; }

    public int? PublishedByUserId { get; set; }

    public string SnapshotData { get; set; } = null!;

    public virtual User? PublishedByUser { get; set; }

    public virtual ShootDay ShootDay { get; set; } = null!;
}
