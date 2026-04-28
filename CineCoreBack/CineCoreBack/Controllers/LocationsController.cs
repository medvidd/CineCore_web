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

        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<IEnumerable<LocationResponseDto>>> GetLocationsByProject(
            int projectId, [FromQuery] string type = "All", [FromQuery] string search = "")
        {
            var query = _context.Locations
                .Include(l => l.IdNavigation)
                .Where(l => l.IdNavigation.ProjectId == projectId)
                .AsQueryable();

            if (type != "All" && !string.IsNullOrEmpty(type))
            {
                query = query.Where(l => l.LocationType == type.ToLower());
            }

            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                query = query.Where(l => l.LocationName.ToLower().Contains(s) ||
                                        (l.City != null && l.City.ToLower().Contains(s)) ||
                                        (l.Street != null && l.Street.ToLower().Contains(s)));
            }

            var locations = await query.Select(l => new LocationResponseDto
            {
                Id = l.Id,
                Name = l.LocationName,
                City = l.City,
                Street = l.Street,
                Desc = $"{l.Street}, {l.City}".Trim(new char[] { ' ', ',' }),
                Type = l.LocationType,
                Manager = l.ContactName,
                Phone = l.ContactPhone,
                Usage = l.IdNavigation.SceneResources.Count
            }).ToListAsync();

            return Ok(locations);
        }

        [HttpPost]
        public async Task<ActionResult<LocationResponseDto>> CreateLocation(LocationCreateUpdateDto dto)
        {
            // ВАЛІДАЦІЯ: Перевірка на унікальність назви локації в межах проекту
            bool exists = await _context.Locations
                .AnyAsync(l => l.IdNavigation.ProjectId == dto.ProjectId && l.LocationName.ToLower() == dto.LocationName.ToLower());

            if (exists) return BadRequest(new { message = "A location with this name already exists in this project." });

            var resource = new Resource { ProjectId = dto.ProjectId };
            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLocation(int id, LocationCreateUpdateDto dto)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return NotFound(new { message = "Location not found" });

            // ВАЛІДАЦІЯ: Перевірка на унікальність (якщо перейменовують)
            bool exists = await _context.Locations
                .AnyAsync(l => l.IdNavigation.ProjectId == dto.ProjectId && l.Id != id && l.LocationName.ToLower() == dto.LocationName.ToLower());

            if (exists) return BadRequest(new { message = "A location with this name already exists in this project." });

            location.LocationName = dto.LocationName;
            location.City = dto.City;
            location.Street = dto.Street;
            location.ContactName = dto.ContactName;
            location.ContactPhone = dto.ContactPhone;
            location.LocationType = dto.LocationType;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            var resource = await _context.Resources
                .Include(r => r.Location)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (resource == null) return NotFound();

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