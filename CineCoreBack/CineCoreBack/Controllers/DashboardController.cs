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
            // Примітка: якщо у моделі Scene є поле Status, розкоментуйте логіку для draft/complete.
            // Поки що я імітую перевірку, або можна вважати всі сцени без певних даних як draft.
            var allScenes = await _context.Scenes.Where(s => s.ProjectId == projectId).ToListAsync();
            var totalScenes = allScenes.Count;
            // var completedScenes = allScenes.Count(s => s.Status == "complete"); 
            // var draftScenes = totalScenes - completedScenes;
            var completedScenes = 0; // Тимчасово: замініть на перевірку статусу, якщо він є у моделі Scene
            var draftScenes = totalScenes;

            var scriptProgressPct = totalScenes > 0 ? (int)Math.Round((double)completedScenes / totalScenes * 100) : 0;

            // 3. Casting
            var totalRoles = await _context.Roles.CountAsync(r => r.ProjectId == projectId);
            // Рахуємо ролі, у яких є хоча б один кастинг зі статусом "approved"
            var castRoles = await _context.Roles
                .Where(r => r.ProjectId == projectId && r.Castings.Any(c => c.CastStatus == "approved"))
                .CountAsync();
            var pendingRoles = totalRoles - castRoles;
            var castingProgressPct = totalRoles > 0 ? (int)Math.Round((double)castRoles / totalRoles * 100) : 0;

            // 4. Upcoming Shoot
            var upcomingShoot = await _context.ShootDays
                .Include(sd => sd.BaseLocation)
                .Include(sd => sd.SceneSchedules)
                .Where(sd => sd.ProjectId == projectId && sd.ShiftStart > DateTime.UtcNow)
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
            // Location та Prop дістаємо через зв'язок з Resource
            var totalLocations = await _context.Locations
                .CountAsync(l => l.IdNavigation.ProjectId == projectId);

            var totalProps = await _context.Props
                .CountAsync(p => p.IdNavigation.ProjectId == projectId);

            var unscheduledScenes = await _context.Scenes
                .Where(s => s.ProjectId == projectId && !s.SceneSchedules.Any())
                .CountAsync();

            var pendingInvites = await _context.ProjectInvitations
                .CountAsync(pi => pi.ProjectId == projectId);

            // Формуємо фінальний DTO
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