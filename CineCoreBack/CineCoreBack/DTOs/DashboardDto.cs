using System;

namespace CineCoreBack.DTOs
{
    public class DashboardDto
    {
        public ProjectSummaryDto ProjectSummary { get; set; } = null!;
        public ScriptProgressDto ScriptProgress { get; set; } = null!;
        public CastingProgressDto CastingProgress { get; set; } = null!;
        public UpcomingShootDto UpcomingShoot { get; set; } = null!;
        public QuickStatsDto QuickStats { get; set; } = null!;
    }

    public class ProjectSummaryDto
    {
        public string Title { get; set; } = string.Empty;
        public string Synopsis { get; set; } = string.Empty;
        public int TeamMembersCount { get; set; }
    }

    public class ScriptProgressDto
    {
        public int CompletedScenes { get; set; }
        public int DraftScenes { get; set; }
        public int TotalScenes { get; set; }
        public int ProgressPercentage { get; set; }
    }

    public class CastingProgressDto
    {
        public int CastRoles { get; set; }
        public int PendingRoles { get; set; }
        public int TotalRoles { get; set; }
        public int ProgressPercentage { get; set; }
    }

    public class UpcomingShootDto
    {
        public bool HasUpcoming { get; set; }
        public DateTime? Date { get; set; }
        public string? CallTime { get; set; }
        public string? LocationName { get; set; }
        public int ScenesCount { get; set; }
    }

    public class QuickStatsDto
    {
        public int TotalScenes { get; set; }
        public int TotalRoles { get; set; }
        public int TotalLocations { get; set; }
        public int TotalProps { get; set; }
        public int UnscheduledScenes { get; set; }
        public int PendingInvites { get; set; }
    }
}