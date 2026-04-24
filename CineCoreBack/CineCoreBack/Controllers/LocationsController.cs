using CineCoreBack.DTOs;
using CineCoreBack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineCoreBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationsController : ControllerBase
    {
        private readonly DbConfig _context;

        public LocationsController(DbConfig context)
        {
            _context = context;
        }

        // GET: api/Locations
        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<IEnumerable<LocationResponseDto>>> GetLocationsByProject(int projectId)
        {
            var locations = await _context.Locations
                .Include(l => l.IdNavigation)
                    .ThenInclude(r => r.SceneResources)
                .Where(l => l.IdNavigation.ProjectId == projectId) // ФІЛЬТР ЗА ПРОЕКТОМ
                .Select(l => new LocationResponseDto
                {
                    Id = l.Id,
                    Name = l.LocationName,
                    Desc = $"{l.Street}, {l.City}".Trim(new char[] { ' ', ',' }),
                    Type = l.LocationType,
                    Manager = l.ContactName,
                    Phone = l.ContactPhone,
                    Usage = l.IdNavigation.SceneResources.Count
                })
                .ToListAsync();

            return Ok(locations);
        }

        // POST: api/Locations
        [HttpPost]
        public async Task<ActionResult<LocationResponseDto>> CreateLocation(LocationCreateUpdateDto dto)
        {
            // 1. Створюємо ресурс з прив'язкою до проекту
            var resource = new Resource
            {
                ProjectId = dto.ProjectId
            };
            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

            // 2. Створюємо локацію
            var location = new Location
            {
                Id = resource.Id,
                LocationName = dto.LocationName,
                City = dto.City,
                Street = dto.Street,
                ContactName = dto.ContactName,
                ContactPhone = dto.ContactPhone,
                LocationType = dto.LocationType
            };

            _context.Locations.Add(location);
            await _context.SaveChangesAsync();

            return Ok(new { Id = location.Id, Message = "Location created successfully" });
        }

        // PUT: api/Locations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLocation(int id, LocationCreateUpdateDto dto)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return NotFound();

            location.LocationName = dto.LocationName;
            location.City = dto.City;
            location.Street = dto.Street;
            location.ContactName = dto.ContactName;
            location.ContactPhone = dto.ContactPhone;
            location.LocationType = dto.LocationType;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Locations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            // Шукаємо ресурс (оскільки Location залежить від Resource)
            var resource = await _context.Resources
                .Include(r => r.Location)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (resource == null) return NotFound();

            // Видалення Resource автоматично видалить і Location (якщо налаштований Cascade),
            // але для надійності видалимо явно:
            if (resource.Location != null)
            {
                _context.Locations.Remove(resource.Location);
            }
            _context.Resources.Remove(resource);

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}