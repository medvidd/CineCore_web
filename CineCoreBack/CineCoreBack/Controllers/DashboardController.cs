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
            if (project == null)
            {
                return NotFound(new { message = "Project not found" });
            }

            // 1. Project Summary
            var teamMembersCount = await _context.ProjectMembers.CountAsync(pm => pm.ProjectId == projectId);

            // 2. Script Progress
            // Сцена вважається "completed" якщо:
            //   - має прив'язану локацію (Location через Resources)
            //   - І була запланована у підтвердженому (IsConfirmed == true) shoot day
            var allScenes = await _context.Scenes
                .Include(s => s.SceneSchedules)
                    .ThenInclude(ss => ss.ShootDay)
                .Include(s => s.SceneResources)
                    .ThenInclude(sr => sr.Resource)
                .Where(s => s.ProjectId == projectId)
                .ToListAsync();

            var totalScenes = allScenes.Count;

            var completedScenes = allScenes.Count(s =>
            {
                // Перевірка 1: чи є прив'язана локація
                bool hasLocation = s.SceneResources != null &&
                    s.SceneResources.Any(sr => sr.Resource != null && sr.Resource.Type == "location");

                // Перевірка 2: чи є у підтвердженому shoot day
                bool isScheduledInConfirmedDay = s.SceneSchedules != null &&
                    s.SceneSchedules.Any(ss => ss.ShootDay != null && ss.ShootDay.IsConfirmed == true);

                return hasLocation && isScheduledInConfirmedDay;
            });

            var draftScenes = totalScenes - completedScenes;
            var scriptProgressPct = totalScenes > 0
                ? (int)Math.Round((double)completedScenes / totalScenes * 100)
                : 0;

            // 3. Casting
            var totalRoles = await _context.Roles.CountAsync(r => r.ProjectId == projectId);
            var castRoles = await _context.Roles
                .Where(r => r.ProjectId == projectId && r.Castings.Any(c => c.CastStatus == "approved"))
                .CountAsync();
            var pendingRoles = totalRoles - castRoles;
            var castingProgressPct = totalRoles > 0
                ? (int)Math.Round((double)castRoles / totalRoles * 100)
                : 0;

            // 4. Upcoming Shoot — ТІЛЬКИ підтверджені дні (IsConfirmed == true)
            var upcomingShoot = await _context.ShootDays
                .Include(sd => sd.BaseLocation)
                .Include(sd => sd.SceneSchedules)
                .Where(sd =>
                    sd.ProjectId == projectId &&
                    sd.ShiftStart > DateTime.UtcNow &&
                    sd.IsConfirmed == true)
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

            // 5. Quick Stats
            var totalLocations = await _context.Locations
                .CountAsync(l => l.IdNavigation.ProjectId == projectId);

            var totalProps = await _context.Props
                .CountAsync(p => p.IdNavigation.ProjectId == projectId);

            var unscheduledScenes = await _context.Scenes
                .Where(s => s.ProjectId == projectId && !s.SceneSchedules.Any())
                .CountAsync();

            var pendingInvites = await _context.ProjectInvitations
                .CountAsync(pi => pi.ProjectId == projectId);

            var dashboardData = new DashboardDto
            {
                ProjectSummary = new ProjectSummaryDto
                {
                    Title = project.Title,
                    Synopsis = project.Synopsis ?? "No synopsis available.",
                    TeamMembersCount = teamMembersCount
                },
                ScriptProgress = new ScriptProgressDto
                {
                    CompletedScenes = completedScenes,
                    DraftScenes = draftScenes,
                    TotalScenes = totalScenes,
                    ProgressPercentage = scriptProgressPct
                },
                CastingProgress = new CastingProgressDto
                {
                    CastRoles = castRoles,
                    PendingRoles = pendingRoles,
                    TotalRoles = totalRoles,
                    ProgressPercentage = castingProgressPct
                },
                UpcomingShoot = upcomingShootDto,
                QuickStats = new QuickStatsDto
                {
                    TotalScenes = totalScenes,
                    TotalRoles = totalRoles,
                    TotalLocations = totalLocations,
                    TotalProps = totalProps,
                    UnscheduledScenes = unscheduledScenes,
                    PendingInvites = pendingInvites
                }
            };

            return Ok(dashboardData);
        }
    }
}