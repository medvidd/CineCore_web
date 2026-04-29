namespace CineCoreBack.Models;

public class ProjectDashboardStat
{
    public int ProjectId { get; set; }
    public long TotalScenes { get; set; }
    public long CompletedScenes { get; set; }
    public long TotalRoles { get; set; }
    public long CastRoles { get; set; }
    public long TotalLocations { get; set; }
    public long TotalProps { get; set; }
    public long PendingInvites { get; set; }
}