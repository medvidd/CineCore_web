using CineCoreBack.DTOs;
using CineCoreBack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineCoreBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CrewController : ControllerBase
    {
        private readonly DbConfig _context;

        public CrewController(DbConfig context)
        {
            _context = context;
        }

        // 1. ПОШУК КОРИСТУВАЧА ЗА EMAIL (для модалки запрошення)
        // GET: api/Crew/search-user?email=test@test.com
        [HttpGet("search-user")]
        public async Task<ActionResult<UserSearchResultDto>> SearchUserByEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return BadRequest("Email is required");

            var user = await _context.Users
                .Where(u => u.Email.ToLower() == email.ToLower())
                .Select(u => new UserSearchResultDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName
                })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound(new { message = "User not found in system." });

            return Ok(user);
        }

        // 2. СТВОРЕННЯ ЗАПРОШЕННЯ АБО ДОДАВАННЯ УЧАСНИКА
        // POST: api/Crew/invite
        [HttpPost("invite")]
        public async Task<IActionResult> InviteMember(CreateInvitationDto dto)
        {
            // 1. Перевіряємо, чи користувач ВЖЕ Є АКТИВНИМ учасником цього проекту
            var isAlreadyMember = await _context.ProjectMembers
                .Include(pm => pm.User)
                .AnyAsync(pm => pm.ProjectId == dto.ProjectId && pm.User.Email.ToLower() == dto.Email.ToLower());

            if (isAlreadyMember) return BadRequest(new { message = "This user is already a member of the project." });

            // 2. Шукаємо, чи зареєстрований такий email у системі загалом
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (user != null)
            {
                // СЦЕНАРІЙ А: Акаунт існує -> Додаємо одразу в ProjectMembers
                var newMember = new ProjectMember
                {
                    ProjectId = dto.ProjectId,
                    UserId = user.Id,
                    SysRole = dto.SysRole.ToLower(),
                    JobTitle = dto.JobTitle,
                    Department = dto.Department,
                    JoinedAt = DateTime.UtcNow,
                    InvitedEmail = user.Email
                };
                _context.ProjectMembers.Add(newMember);
            }
            else
            {
                // СЦЕНАРІЙ Б: Акаунта немає -> Створюємо Invitation (без перевірки на дублікати, щоб можна було слати ще раз)
                var invite = new ProjectInvitation
                {
                    ProjectId = dto.ProjectId,
                    Email = dto.Email,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    SysRole = dto.SysRole.ToLower(),
                    JobTitle = dto.JobTitle,
                    Department = dto.Department,
                    Message = dto.Message,
                    InvitedById = dto.InvitedById,
                    DateSent = DateTime.UtcNow,
                    Token = Guid.NewGuid()
                };
                _context.ProjectInvitations.Add(invite);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Invitation processed successfully." });
        }

        // 3. ВИДАЛЕННЯ ЗАПРОШЕННЯ
        // DELETE: api/Crew/invites/5
        [HttpDelete("invites/{id}")]
        public async Task<IActionResult> DeleteInvite(int id)
        {
            var invite = await _context.ProjectInvitations.FindAsync(id);
            if (invite == null) return NotFound();

            _context.ProjectInvitations.Remove(invite);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 3. ОТРИМАННЯ СПИСКІВ УЧАСНИКІВ ТА ЗАПРОШЕНЬ
        // GET: api/Crew/project/5
        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetProjectCrew(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return NotFound();

            var activeMembers = await _context.ProjectMembers
                .Include(pm => pm.User)
                .Where(pm => pm.ProjectId == projectId && pm.UserId != null)
                .Select(pm => new ActiveMemberResponseDto
                {
                    UserId = pm.UserId.Value,
                    Email = pm.User!.Email,
                    FullName = $"{pm.User.FirstName} {pm.User.LastName}".Trim(),
                    SysRole = pm.SysRole,
                    JobTitle = pm.JobTitle,
                    Department = pm.Department,
                    JoinedDate = pm.JoinedAt ?? DateTime.UtcNow
                })
                .ToListAsync();

            // ДОДАНО: Перевірка, чи є власник у списку. Якщо немає - додаємо його вручну
            if (!activeMembers.Any(m => m.UserId == project.OwnerId))
            {
                activeMembers.Insert(0, new ActiveMemberResponseDto
                {
                    UserId = project.OwnerId,
                    Email = project.Owner.Email,
                    FullName = $"{project.Owner.FirstName} {project.Owner.LastName}".Trim(),
                    SysRole = "owner",
                    JobTitle = "Project Creator",
                    Department = "Administration",
                    JoinedDate = project.CreatedAt ?? DateTime.UtcNow
                });
            }
            else
            {
                // Якщо він там є, переконуємось, що роль встановлена як "owner"
                var ownerInList = activeMembers.First(m => m.UserId == project.OwnerId);
                ownerInList.SysRole = "owner";
            }

            var pendingInvites = await _context.ProjectInvitations
                .Include(pi => pi.InvitedBy)
                .Where(pi => pi.ProjectId == projectId)
                .Select(pi => new PendingInvitationResponseDto
                {
                    Id = pi.Id,
                    Email = pi.Email,
                    SysRole = pi.SysRole,
                    JobTitle = pi.JobTitle,
                    Department = pi.Department,
                    InvitedBy = $"{pi.InvitedBy.FirstName} {pi.InvitedBy.LastName}".Trim(),
                    DateSent = pi.DateSent
                })
                .ToListAsync();

            return Ok(new { ActiveMembers = activeMembers, PendingInvites = pendingInvites });
        }

        // Додайте ці методи в CrewController.cs

        // 4. РЕДАГУВАННЯ ДАНИХ УЧАСНИКА (Тільки для Owner/Manager)
        [HttpPut("project/{projectId}/member/{targetUserId}")]
        public async Task<IActionResult> UpdateMember(int projectId, int targetUserId, [FromQuery] int currentUserId, [FromBody] UpdateMemberDto dto)
        {
            // Перевірка прав того, хто редагує
            var currentUserRole = await GetUserRoleInProject(projectId, currentUserId);
            if (currentUserRole != "owner" && currentUserRole != "manager")
            {
                return Forbid("You don't have permission to edit members.");
            }

            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == targetUserId);

            if (member == null) return NotFound();

            member.JobTitle = dto.JobTitle;
            member.Department = dto.Department;
            member.SysRole = dto.SysRole; // Можна також змінити системну роль

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 5. ВИДАЛЕННЯ УЧАСНИКА (Тільки для Owner/Manager)
        [HttpDelete("project/{projectId}/member/{targetUserId}")]
        public async Task<IActionResult> RemoveMember(int projectId, int targetUserId, [FromQuery] int currentUserId)
        {
            var currentUserRole = await GetUserRoleInProject(projectId, currentUserId);
            if (currentUserRole != "owner" && currentUserRole != "manager")
            {
                return Forbid();
            }

            // Не можна видалити власника проекту
            var project = await _context.Projects.FindAsync(projectId);
            if (project?.OwnerId == targetUserId) return BadRequest("Cannot remove the project owner.");

            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == targetUserId);

            if (member != null)
            {
                _context.ProjectMembers.Remove(member);
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        // Хелпер для визначення ролі
        private async Task<string> GetUserRoleInProject(int projectId, int userId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project?.OwnerId == userId) return "owner";

            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

            return member?.SysRole ?? "none";
        }
    }
}