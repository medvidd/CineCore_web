using System;
using System.Collections.Generic;

namespace CineCoreBack.Models;

public partial class Actor
{
    public int Id { get; set; }

    public string? Characteristics { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual ICollection<Casting> Castings { get; set; } = new List<Casting>();
}
