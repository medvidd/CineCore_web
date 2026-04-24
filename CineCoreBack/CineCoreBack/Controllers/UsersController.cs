using CineCoreBack.DTOs;
using CineCoreBack.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineCoreBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly DbConfig _context;

        public UsersController(DbConfig context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserResponseDto>> Login(UserLoginDto loginDto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null || user.PasswordHash != loginDto.Password)
            {
                return Unauthorized("Wrong email or password");
            }

            return Ok(new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AvatarTheme = user.AvatarTheme,
                PhoneNum = user.PhoneNum, // Додано
                Birthday = user.Birthday  // Додано
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserProfileDto>> GetUserProfile(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null) return NotFound();

            return Ok(new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNum = user.PhoneNum,          
            });
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserResponseDto>> Register(UserRegisterDto registerDto)
        {
            var user = new User
            {
                Email = registerDto.Email,
                PasswordHash = registerDto.Password, 
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                PhoneNum = registerDto.PhoneNum,
                Birthday = registerDto.Birthday,
                AvatarTheme = registerDto.AvatarTheme ?? "theme-teal",
                RegisteredAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AvatarTheme = user.AvatarTheme,
                PhoneNum = user.PhoneNum, // Додано
                Birthday = user.Birthday  // Додано
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProfile(int id, UserUpdateDto updateDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.FirstName = updateDto.FirstName;
            user.LastName = updateDto.LastName;
            user.Email = updateDto.Email;
            user.PhoneNum = updateDto.PhoneNum;
            user.AvatarTheme = updateDto.AvatarTheme;

            await _context.SaveChangesAsync();
            return Ok(user);
        }

        [HttpPut("{id}/password")]
        public async Task<IActionResult> UpdatePassword(int id, UserPasswordUpdateDto passwordDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (user.PasswordHash != passwordDto.CurrentPassword)
                return BadRequest("Invalid current password");

            user.PasswordHash = passwordDto.NewPassword;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var user = await _context.Users
                .Include(u => u.Projects)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            if (user.Projects.Any())
            {
                _context.Projects.RemoveRange(user.Projects);
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
