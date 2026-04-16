using System;
using System.Collections.Generic;

namespace CineCoreBack.Models;

public partial class SceneResource
{
    public int SceneId { get; set; }

    public int ResourceId { get; set; }

    public string? Notes { get; set; }

    public virtual Resource Resource { get; set; } = null!;

    public virtual Scene Scene { get; set; } = null!;
}
