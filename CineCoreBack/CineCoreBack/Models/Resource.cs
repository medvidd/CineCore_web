using System;
using System.Collections.Generic;

namespace CineCoreBack.Models;

public partial class Resource
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public virtual Project Project { get; set; } = null!;

    public virtual Location? Location { get; set; }

    public virtual Prop? Prop { get; set; }

    public virtual Role? Role { get; set; }

    public virtual ICollection<SceneResource> SceneResources { get; set; } = new List<SceneResource>();
}
