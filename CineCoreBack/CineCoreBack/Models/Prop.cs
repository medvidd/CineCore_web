using System;
using System.Collections.Generic;

namespace CineCoreBack.Models;

public partial class Prop
{
    public int Id { get; set; }

    public string PropName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual Resource IdNavigation { get; set; } = null!;
}
