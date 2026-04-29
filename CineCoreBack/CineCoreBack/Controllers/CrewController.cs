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

        // 1. ПОШУК КОРИСТУВАЧА ЗА EMAIL
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
        [HttpPost("invite")]
        public async Task<IActionResult> InviteMember(CreateInvitationDto dto)
        {
            var targetEmail = dto.Email.ToLower();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == targetEmail);

            if (user != null)
            {
                var existingMember = await _context.ProjectMembers
                    .FirstOrDefaultAsync(pm => pm.ProjectId == dto.ProjectId && pm.UserId == user.Id);

                if (existingMember != null)
                {
                    if (existingMember.MemberStatus == "active")
                        return BadRequest(new { message = "This user is already an active member of the project." });
                    if (existingMember.MemberStatus == "pending")
                        return BadRequest(new { message = "An invitation is already pending for this user." });

                    // Якщо був відхилений - оновлюємо статус і відправляємо знову
                    existingMember.MemberStatus = "pending";
                    existingMember.InvitedAt = DateTime.UtcNow;
                    existingMember.InvitedByUserId = dto.InvitedById;
                    existingMember.SysRole = dto.SysRole.ToLower();
                    existingMember.JobTitle = dto.JobTitle;
                    existingMember.Department = dto.Department;
                }
                else
                {
                    var newMember = new ProjectMember
                    {
                        ProjectId = dto.ProjectId,
                        UserId = user.Id,
                        SysRole = dto.SysRole.ToLower(),
                        JobTitle = dto.JobTitle,
                        Department = dto.Department,
                        InvitedEmail = user.Email,
                        InvitedByUserId = dto.InvitedById,
                        InvitedAt = DateTime.UtcNow,
                        MemberStatus = "pending" // Додаємо як очікуючого
                    };
                    _context.ProjectMembers.Add(newMember);
                }
            }
            else
            {
                var hasPendingInvite = await _context.ProjectInvitations
                    .AnyAsync(pi => pi.ProjectId == dto.ProjectId && pi.Email.ToLower() == targetEmail);
                if (hasPendingInvite) return BadRequest(new { message = "An invitation has already been sent to this email." });

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
            return Ok(new { message = "Invitation sent successfully." });
        }

        // 3. ВИДАЛЕННЯ ЗАПРОШЕННЯ
        [HttpDelete("invites/{id}")]
        public async Task<IActionResult> DeleteInvite(int id)
        {
            var invite = await _context.ProjectInvitations.FindAsync(id);
            if (invite == null) return NotFound();

            _context.ProjectInvitations.Remove(invite);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 4. ОТРИМАННЯ СПИСКІВ УЧАСНИКІВ ТА ЗАПРОШЕНЬ
        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetProjectCrew(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return NotFound();

            var activeMembers = await _context.ProjectMembers
                .Include(pm => pm.User)
                .Where(pm => pm.ProjectId == projectId && pm.UserId != null && pm.MemberStatus == "active")
                .Select(pm => new ActiveMemberResponseDto
                {
                    UserId = pm.UserId.Value,
                    Email = pm.User!.Email,
                    FullName = $"{pm.User.FirstName} {pm.User.LastName}".Trim(),
                    SysRole = pm.SysRole,
                    JobTitle = pm.JobTitle,
                    Department = pm.Department,
                    JoinedDate = pm.JoinedAt ?? DateTime.UtcNow,
                    AvatarTheme = pm.User.AvatarTheme // ДОДАНО
                }).ToListAsync();

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
                var ownerInList = activeMembers.First(m => m.UserId == project.OwnerId);
                ownerInList.SysRole = "owner";
            }

            var pendingInvites = new List<PendingInvitationResponseDto>();

            var externalInvites = await _context.ProjectInvitations
                .Include(pi => pi.InvitedBy)
                .Where(pi => pi.ProjectId == projectId)
                .Select(pi => new PendingInvitationResponseDto
                {
                    InviteId = pi.Id,
                    Email = pi.Email,
                    SysRole = pi.SysRole,
                    JobTitle = pi.JobTitle,
                    Department = pi.Department,
                    InvitedBy = $"{pi.InvitedBy.FirstName} {pi.InvitedBy.LastName}".Trim(),
                    DateSent = pi.DateSent,
                    Status = "pending"
                }).ToListAsync();
            pendingInvites.AddRange(externalInvites);

            // Додаємо внутрішні запрошення (ProjectMembers зі статусом pending/declined)
            var internalInvites = await _context.ProjectMembers
                .Include(pm => pm.InvitedByUser)
                .Include(pm => pm.User)
                .Where(pm => pm.ProjectId == projectId && (pm.MemberStatus == "pending" || pm.MemberStatus == "declined"))
                .Select(pm => new PendingInvitationResponseDto
                {
                    UserId = pm.UserId,
                    Email = pm.InvitedEmail,
                    SysRole = pm.SysRole,
                    JobTitle = pm.JobTitle,
                    Department = pm.Department,
                    InvitedBy = pm.InvitedByUser != null ? $"{pm.InvitedByUser.FirstName} {pm.InvitedByUser.LastName}".Trim() : "Unknown",
                    DateSent = pm.InvitedAt ?? DateTime.UtcNow,
                    Status = pm.MemberStatus,
                    AvatarTheme = pm.User != null ? pm.User.AvatarTheme : null // ДОДАНО
                }).ToListAsync();
            pendingInvites.AddRange(internalInvites);

            return Ok(new { ActiveMembers = activeMembers, PendingInvites = pendingInvites.OrderByDescending(i => i.DateSent) });
        }

        // 5. РЕДАГУВАННЯ ДАНИХ УЧАСНИКА
        [HttpPut("project/{projectId}/member/{targetUserId}")]
        public async Task<IActionResult> UpdateMember(int projectId, int targetUserId, [FromQuery] int currentUserId, [FromBody] UpdateMemberDto dto)
        {
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
            member.SysRole = dto.SysRole;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 6. ВИДАЛЕННЯ УЧАСНИКА
        [HttpDelete("project/{projectId}/member/{targetUserId}")]
        public async Task<IActionResult> RemoveMember(int projectId, int targetUserId, [FromQuery] int currentUserId)
        {
            var currentUserRole = await GetUserRoleInProject(projectId, currentUserId);
            if (currentUserRole != "owner" && currentUserRole != "manager")
            {
                return Forbid();
            }

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

        private async Task<string> GetUserRoleInProject(int projectId, int userId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project?.OwnerId == userId) return "owner";

            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

            return member?.SysRole ?? "none";
        }

        [HttpPost("project/{projectId}/member/{userId}/accept")]
        public async Task<IActionResult> AcceptInvite(int projectId, int userId)
        {
            var member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
            if (member != null)
            {
                member.MemberStatus = "active";
                member.JoinedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost("project/{projectId}/member/{userId}/reject")]
        public async Task<IActionResult> RejectInvite(int projectId, int userId)
        {
            var member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
            if (member != null)
            {
                member.MemberStatus = "declined";
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
    }
}