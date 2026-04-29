using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CineCoreBack.Models;
using CineCoreBack.DTOs;

namespace CineCoreBack.Controllers
{
    [Route("api/projects/{projectId}/dashboard")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly DbConfig _context;

        public DashboardController(DbConfig context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<DashboardDto>> GetDashboardStats(int projectId)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return NotFound(new { message = "Project not found" });

            var teamMembersCount = await _context.ProjectMembers.CountAsync(pm => pm.ProjectId == projectId) + 1;

            // ВИКОРИСТОВУЄМО НАШЕ VIEW!
            var stats = await _context.ProjectDashboardStats.FirstOrDefaultAsync(s => s.ProjectId == projectId)
                        ?? new ProjectDashboardStat();

            var draftScenes = (int)(stats.TotalScenes - stats.CompletedScenes);
            var scriptProgressPct = stats.TotalScenes > 0
                ? (int)Math.Round((double)stats.CompletedScenes / stats.TotalScenes * 100) : 0;

            var pendingRoles = (int)(stats.TotalRoles - stats.CastRoles);
            var castingProgressPct = stats.TotalRoles > 0
                ? (int)Math.Round((double)stats.CastRoles / stats.TotalRoles * 100) : 0;

            // Upcoming Shoot залишаємо, він витягує зв'язки для конкретного дня
            var upcomingShoot = await _context.ShootDays
                .Include(sd => sd.BaseLocation)
                .Include(sd => sd.SceneSchedules)
                .Where(sd => sd.ProjectId == projectId && sd.ShiftStart > DateTime.UtcNow && sd.Status == "published")
                .OrderBy(sd => sd.ShiftStart)
                .FirstOrDefaultAsync();

            var upcomingShootDto = new UpcomingShootDto { HasUpcoming = false };
            if (upcomingShoot != null)
            {
                upcomingShootDto = new UpcomingShootDto
                {
                    HasUpcoming = true,
                    Date = upcomingShoot.ShiftStart,
                    CallTime = upcomingShoot.ShiftStart.ToString("HH:mm"),
                    LocationName = upcomingShoot.BaseLocation?.LocationName ?? "TBD",
                    ScenesCount = upcomingShoot.SceneSchedules.Count
                };
            }

            return Ok(new DashboardDto
            {
                ProjectSummary = new ProjectSummaryDto { Title = project.Title, Synopsis = project.Synopsis ?? "", TeamMembersCount = teamMembersCount },
                ScriptProgress = new ScriptProgressDto { CompletedScenes = (int)stats.CompletedScenes, DraftScenes = draftScenes, TotalScenes = (int)stats.TotalScenes, ProgressPercentage = scriptProgressPct },
                CastingProgress = new CastingProgressDto { CastRoles = (int)stats.CastRoles, PendingRoles = pendingRoles, TotalRoles = (int)stats.TotalRoles, ProgressPercentage = castingProgressPct },
                UpcomingShoot = upcomingShootDto,
                QuickStats = new QuickStatsDto { TotalScenes = (int)stats.TotalScenes, TotalRoles = (int)stats.TotalRoles, TotalLocations = (int)stats.TotalLocations, TotalProps = (int)stats.TotalProps, UnscheduledScenes = draftScenes, PendingInvites = (int)stats.PendingInvites }
            });
        }
    }
}