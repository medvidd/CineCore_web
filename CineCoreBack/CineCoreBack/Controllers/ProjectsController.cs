using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CineCoreBack.Models;
using CineCoreBack.DTOs;

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
            // 1. Створюємо основну сутність проекту
            var project = new Project
            {
                Title = projectDto.Title,
                Synopsis = projectDto.Synopsis,
                StartDate = projectDto.StartDate,
                OwnerId = projectDto.OwnerId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync(); // Зберігаємо, щоб отримати Id проекту

            // 2. Додаємо зв'язки з жанрами в проміжну таблицю
            if (projectDto.GenreIds.Any())
            {
                foreach (var genreId in projectDto.GenreIds)
                {
                    _context.ProjectGenres.Add(new ProjectGenre
                    {
                        ProjectId = project.Id,
                        GenreId = genreId
                    });
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new ProjectResponseDto
            {
                Id = project.Id,
                Title = project.Title,
                Synopsis = project.Synopsis,
                StartDate = project.StartDate
            });
        }

        // Додатковий метод для отримання списку жанрів (знадобиться для випадаючого списку в UI)
        [HttpGet("genres")]
        public async Task<ActionResult<IEnumerable<Genre>>> GetGenres()
        {
            return await _context.Genres.ToListAsync();
        }
    }
}