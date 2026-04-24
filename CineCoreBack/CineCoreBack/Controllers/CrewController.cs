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

        // 2. СТВОРЕННЯ ЗАПРОШЕННЯ
        // POST: api/Crew/invite
        [HttpPost("invite")]
        public async Task<IActionResult> InviteMember(CreateInvitationDto dto)
        {
            // Перевіряємо, чи цей email вже не є в цьому проекті як активний учасник
            var existingMember = await _context.ProjectMembers
                .Include(pm => pm.User)
                .AnyAsync(pm => pm.ProjectId == dto.ProjectId && pm.User.Email.ToLower() == dto.Email.ToLower());

            if (existingMember) return BadRequest(new { message = "This user is already a member of the project." });

            // Перевіряємо, чи немає вже активного запрошення для цього email
            var existingInvite = await _context.ProjectInvitations
                .AnyAsync(pi => pi.ProjectId == dto.ProjectId && pi.Email.ToLower() == dto.Email.ToLower());

            if (existingInvite) return BadRequest(new { message = "An invitation has already been sent to this email." });

            // Створюємо запрошення
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
                Token = Guid.NewGuid() // Генеруємо унікальний токен для посилання
            };

            _context.ProjectInvitations.Add(invite);
            await _context.SaveChangesAsync();

            // TODO: ТУТ БУДЕ ЛОГІКА ВІДПРАВКИ EMAIL
            // Наприклад: await _emailService.SendInviteEmail(invite.Email, invite.Token, ...);

            // Тимчасово для тестування повертаємо токен у відповіді, щоб бачити, що він згенерувався
            return Ok(new
            {
                message = "Invitation created successfully.",
                inviteId = invite.Id,
                previewToken = invite.Token // ТІЛЬКИ ДЛЯ ДЕБАГУ, потім приберемо
            });
        }

        // 3. ОТРИМАННЯ СПИСКІВ УЧАСНИКІВ ТА ЗАПРОШЕНЬ
        // GET: api/Crew/project/5
        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetProjectCrew(int projectId)
        {
            // Беремо активних учасників (ті, хто вже прийняв запрошення або є власником)
            // Примітка: переконайтеся, що у моделі ProjectMember є поля JobTitle та Department
            // Беремо активних учасників (тільки тих, у кого вже є UserId)
            var activeMembers = await _context.ProjectMembers
                .Include(pm => pm.User)
                .Where(pm => pm.ProjectId == projectId && pm.UserId != null) // Відсіюємо тих, хто ще не приєднався
                .Select(pm => new ActiveMemberResponseDto
                {
                    UserId = pm.UserId.Value, // Виправлено: .Value вирішує проблему конвертації int? в int
                    Email = pm.User!.Email,
                    FullName = $"{pm.User.FirstName} {pm.User.LastName}".Trim(),
                    SysRole = pm.SysRole, // Виправлено: використовуємо додане поле SysRole
                    JobTitle = pm.JobTitle,
                    Department = pm.Department,
                    JoinedDate = pm.JoinedAt ?? DateTime.UtcNow // Виправлено: JoinedAt замість JoinedDate
                })
                .ToListAsync();

            // Беремо очікуючі запрошення
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

            return Ok(new
            {
                ActiveMembers = activeMembers,
                PendingInvites = pendingInvites
            });
        }
    }
}