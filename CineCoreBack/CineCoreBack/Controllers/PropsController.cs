using CineCoreBack.DTOs;
using CineCoreBack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineCoreBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PropsController : ControllerBase
    {
        private readonly DbConfig _context;

        public PropsController(DbConfig context)
        {
            _context = context;
        }

        // GET: api/Props
        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<IEnumerable<PropResponseDto>>> GetPropsByProject(
    int projectId,
    [FromQuery] string category = "All",
    [FromQuery] string search = "")
        {
            var query = _context.Props
                .Include(p => p.IdNavigation)
                .Where(p => p.IdNavigation.ProjectId == projectId)
                .AsQueryable();

            if (category != "All" && !string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.PropType == category.ToLower());
            }

            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                query = query.Where(p => p.PropName.ToLower().Contains(s) ||
                                        (p.Description != null && p.Description.ToLower().Contains(s)));
            }

            var props = await query.Select(p => new PropResponseDto
            {
                Id = p.Id,
                Name = p.PropName,
                Desc = p.Description,
                Category = p.PropType,
                Acquisition = p.AcquisitionType,
                Status = p.PropStatus,
                Scenes = p.IdNavigation.SceneResources.Count
            }).ToListAsync();

            return Ok(props);
        }

        // POST: api/Props
        [HttpPost]
        public async Task<ActionResult<PropResponseDto>> CreateProp(PropCreateUpdateDto dto)
        {
            // 1. Створюємо ресурс з прив'язкою до проекту
            var resource = new Resource
            {
                ProjectId = dto.ProjectId
            };
            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

            // 2. Створюємо проп
            var prop = new Prop
            {
                Id = resource.Id,
                PropName = dto.PropName,
                Description = dto.Description,
                AcquisitionType = dto.AcquisitionType,
                PropStatus = dto.PropStatus,
                PropType = dto.PropType
            };

            _context.Props.Add(prop);
            await _context.SaveChangesAsync();

            return Ok(new { Id = prop.Id, Message = "Prop created successfully" });
        }

        // PUT: api/Props/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProp(int id, PropCreateUpdateDto dto)
        {
            var prop = await _context.Props.FindAsync(id);
            if (prop == null) return NotFound();

            prop.PropName = dto.PropName;
            prop.Description = dto.Description;
            prop.AcquisitionType = dto.AcquisitionType;
            prop.PropStatus = dto.PropStatus;
            prop.PropType = dto.PropType;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Props/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProp(int id)
        {
            var resource = await _context.Resources
                .Include(r => r.Prop)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (resource == null) return NotFound();

            if (resource.Prop != null)
            {
                _context.Props.Remove(resource.Prop);
            }
            _context.Resources.Remove(resource);

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}