using System;
using System.Collections.Generic;

namespace CineCoreBack.Models;

public partial class Casting
{
    public int RoleId { get; set; }

    public int ActorId { get; set; }

    public DateOnly CastDate { get; set; }

    public string? Notes { get; set; }

    public string CastStatus { get; set; } = "pending";

    public virtual Actor Actor { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
