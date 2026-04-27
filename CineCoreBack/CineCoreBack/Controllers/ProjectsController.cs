using CineCoreBack.DTOs;
using CineCoreBack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CineCoreBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly DbConfig _context;

        public ProjectsController(DbConfig context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<ProjectResponseDto>> CreateProject(ProjectCreateDto projectDto)
        {
            var project = new Project
            {
                Title = projectDto.Title,
                Synopsis = projectDto.Synopsis,
                StartDate = projectDto.StartDate,
                OwnerId = projectDto.OwnerId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            if (projectDto.GenreIds != null && projectDto.GenreIds.Any())
            {
                foreach (var genreId in projectDto.GenreIds)
                {
                    _context.ProjectGenres.Add(new ProjectGenre
                    {
                        ProjectId = project.Id,
                        GenreId = genreId
                    });
                }
            }

            if (projectDto.CustomGenres != null && projectDto.CustomGenres.Any())
            {
                foreach (var customGenreName in projectDto.CustomGenres)
                {
                    var cleanName = customGenreName.Trim();
                    if (string.IsNullOrEmpty(cleanName)) continue;

                    var existingGenre = await _context.Genres
                        .FirstOrDefaultAsync(g => g.Name.ToLower() == cleanName.ToLower());

                    int targetGenreId;

                    if (existingGenre != null)
                    {
                        targetGenreId = existingGenre.Id;
                    }
                    else
                    {
                        var newGenre = new Genre { Name = cleanName };
                        _context.Genres.Add(newGenre);
                        await _context.SaveChangesAsync();
                        targetGenreId = newGenre.Id;
                    }

                    if (projectDto.GenreIds == null || !projectDto.GenreIds.Contains(targetGenreId))
                    {
                        _context.ProjectGenres.Add(new ProjectGenre { ProjectId = project.Id, GenreId = targetGenreId });
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new ProjectResponseDto
            {
                Id = project.Id,
                Title = project.Title,
                Synopsis = project.Synopsis,
                StartDate = project.StartDate
            });
        }

        // PUT: api/projects/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ProjectResponseDto>> UpdateProject(int id, ProjectUpdateDto updateDto)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectGenres)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound(new { message = "Project not found" });

            // Оновлюємо базові поля
            project.Title = updateDto.Title;
            project.Synopsis = updateDto.Synopsis;
            project.StartDate = updateDto.StartDate;

            // Оновлюємо жанри: спочатку видаляємо старі
            _context.ProjectGenres.RemoveRange(project.ProjectGenres);

            // Стандартні жанри
            if (updateDto.GenreIds != null && updateDto.GenreIds.Any())
            {
                foreach (var genreId in updateDto.GenreIds)
                {
                    _context.ProjectGenres.Add(new ProjectGenre
                    {
                        ProjectId = project.Id,
                        GenreId = genreId
                    });
                }
            }

            // Кастомні жанри
            if (updateDto.CustomGenres != null && updateDto.CustomGenres.Any())
            {
                foreach (var customGenreName in updateDto.CustomGenres)
                {
                    var cleanName = customGenreName.Trim();
                    if (string.IsNullOrEmpty(cleanName)) continue;

                    var existingGenre = await _context.Genres
                        .FirstOrDefaultAsync(g => g.Name.ToLower() == cleanName.ToLower());

                    int targetGenreId;
                    if (existingGenre != null)
                    {
                        targetGenreId = existingGenre.Id;
                    }
                    else
                    {
                        var newGenre = new Genre { Name = cleanName };
                        _context.Genres.Add(newGenre);
                        await _context.SaveChangesAsync();
                        targetGenreId = newGenre.Id;
                    }

                    if (updateDto.GenreIds == null || !updateDto.GenreIds.Contains(targetGenreId))
                    {
                        _context.ProjectGenres.Add(new ProjectGenre { ProjectId = project.Id, GenreId = targetGenreId });
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new ProjectResponseDto
            {
                Id = project.Id,
                Title = project.Title,
                Synopsis = project.Synopsis,
                StartDate = project.StartDate
            });
        }

        [HttpGet("genres")]
        public async Task<ActionResult<IEnumerable<Genre>>> GetGenres()
        {
            return await _context.Genres
                                 .OrderBy(g => g.Name)
                                 .ToListAsync();
        }

        [HttpGet("user/{ownerId}")]
        public async Task<ActionResult<IEnumerable<ProjectResponseDto>>> GetProjectsByUser(int ownerId, [FromQuery] string role = "All")
        {
            var query = _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.User)
                .Include(p => p.ProjectGenres)
                    .ThenInclude(pg => pg.Genre)
                .AsQueryable();

            if (role == "Project owner")
            {
                query = query.Where(p => p.OwnerId == ownerId);
            }
            else if (role != "All" && !string.IsNullOrEmpty(role))
            {
                string searchRole = role.ToLower();
                query = query.Where(p => p.ProjectMembers.Any(pm => pm.UserId == ownerId && pm.SysRole.ToLower() == searchRole));
            }
            else
            {
                query = query.Where(p => p.OwnerId == ownerId || p.ProjectMembers.Any(pm => pm.UserId == ownerId));
            }

            var projects = await query.OrderByDescending(p => p.Id).ToListAsync();

            var result = projects.Select(p =>
            {
                var memberEntry = p.ProjectMembers.FirstOrDefault(pm => pm.UserId == ownerId);
                var crewList = new List<CrewDto>
                {
                    new CrewDto
                    {
                        Name = p.Owner != null ? $"{p.Owner.FirstName} {p.Owner.LastName}" : "Unknown",
                        Role = "Project owner"
                    }
                };

                if (p.ProjectMembers != null)
                {
                    foreach (var member in p.ProjectMembers)
                    {
                        crewList.Add(new CrewDto
                        {
                            Name = member.User != null ? $"{member.User.FirstName} {member.User.LastName}" : member.InvitedEmail,
                            Role = member.JobTitle ?? member.SysRole
                        });
                    }
                }

                string genres = p.ProjectGenres != null && p.ProjectGenres.Any()
                    ? string.Join(", ", p.ProjectGenres.Select(pg => pg.Genre.Name))
                    : "No genre";

                string sysRole = p.OwnerId == ownerId
                    ? "Project owner"
                    : (memberEntry?.SysRole != null ? char.ToUpper(memberEntry.SysRole[0]) + memberEntry.SysRole.Substring(1) : "Actor");

                string jobTitle = p.OwnerId == ownerId
                    ? "Creator"
                    : (memberEntry?.JobTitle ?? "—");

                return new ProjectResponseDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Synopsis = string.IsNullOrEmpty(p.Synopsis) ? "No synopsis provided." : p.Synopsis,
                    StartDate = p.StartDate,
                    Role = sysRole,
                    JobTitle = jobTitle,
                    Genre = genres,
                    Director = p.Owner != null ? $"{p.Owner.FirstName} {p.Owner.LastName}" : "None",
                    TeamSize = (p.ProjectMembers?.Count ?? 0) + 1,
                    Duration = "0",
                    Crew = crewList,
                    IsJoined = p.OwnerId == ownerId || (memberEntry?.JoinedAt != null),
                };
            }).ToList();

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProjectResponseDto>> GetProjectById(int id)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectGenres)
                    .ThenInclude(pg => pg.Genre)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            string genres = project.ProjectGenres != null && project.ProjectGenres.Any()
                ? string.Join(", ", project.ProjectGenres.Select(pg => pg.Genre.Name))
                : "No genre";

            return Ok(new ProjectResponseDto
            {
                Id = project.Id,
                Title = project.Title,
                Synopsis = project.Synopsis,
                StartDate = project.StartDate,
                Genre = genres,
                GenreIds = project.ProjectGenres?.Select(pg => pg.GenreId).ToList() ?? new List<int>()
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectGenres)
                .Include(p => p.ProjectMembers)
                .Include(p => p.Scenes)
                .Include(p => p.ShootDays)
                .Include(p => p.Roles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            _context.ProjectGenres.RemoveRange(project.ProjectGenres);
            _context.ProjectMembers.RemoveRange(project.ProjectMembers);
            _context.Scenes.RemoveRange(project.Scenes);
            _context.ShootDays.RemoveRange(project.ShootDays);
            _context.Roles.RemoveRange(project.Roles);
            _context.Projects.Remove(project);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{projectId}/role/{userId}")]
        public async Task<ActionResult> GetUserRoleInProject(int projectId, int userId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project?.OwnerId == userId) return Ok(new { role = "owner" });

            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

            return Ok(new { role = member?.SysRole ?? "none" });
        }
    }
}