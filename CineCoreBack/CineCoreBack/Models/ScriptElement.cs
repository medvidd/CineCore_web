using System;
using System.Collections.Generic;

namespace CineCoreBack.Models;

public partial class ScriptElement
{
    public int Id { get; set; }

    public int SceneId { get; set; }

    public int OrderIndex { get; set; }

    public string Content { get; set; } = null!;

    public int? RoleId { get; set; }

    public virtual Role? Role { get; set; }

    public virtual Scene Scene { get; set; } = null!;
}
