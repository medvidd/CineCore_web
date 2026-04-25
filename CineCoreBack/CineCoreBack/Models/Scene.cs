using System;
using System.Collections.Generic;

namespace CineCoreBack.Models;

public partial class Scene
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public int SequenceNum { get; set; }

    public string SluglineText { get; set; } = null!;

    public TimeSpan? EstimatedDuration { get; set; }

    public virtual Project Project { get; set; } = null!;

    public string? Notes { get; set; }

    public virtual ICollection<SceneResource> SceneResources { get; set; } = new List<SceneResource>();

    public virtual ICollection<SceneSchedule> SceneSchedules { get; set; } = new List<SceneSchedule>();

    public virtual ICollection<ScriptElement> ScriptElements { get; set; } = new List<ScriptElement>();
}
