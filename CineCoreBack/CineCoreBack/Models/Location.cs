using System;
using System.Collections.Generic;

namespace CineCoreBack.Models;

public partial class Location
{
    public int Id { get; set; }

    public string LocationName { get; set; } = null!;

    public string? City { get; set; }

    public string? Street { get; set; }

    public string? ContactName { get; set; }

    public string? ContactPhone { get; set; }

    public virtual Resource IdNavigation { get; set; } = null!;

    public virtual ICollection<ShootDay> ShootDays { get; set; } = new List<ShootDay>();
}
