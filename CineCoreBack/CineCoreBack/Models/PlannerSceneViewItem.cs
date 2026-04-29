using System;

namespace CineCoreBack.Models;

public class PlannerSceneViewItem
{
    public int SceneId { get; set; }
    public int ProjectId { get; set; }
    public int SequenceNum { get; set; }
    public string SluglineText { get; set; } = null!;
    public TimeSpan? EstimatedDuration { get; set; }
    public string? PrimaryLocationName { get; set; }
    public string? CastNames { get; set; }
}