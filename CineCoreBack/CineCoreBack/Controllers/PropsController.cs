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

        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<IEnumerable<PropResponseDto>>> GetPropsByProject(
            int projectId, [FromQuery] string category = "All", [FromQuery] string search = "")
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

        [HttpPost]
        public async Task<ActionResult<PropResponseDto>> CreateProp(PropCreateUpdateDto dto)
        {
            // ВАЛІДАЦІЯ: Перевірка на унікальність назви реквізиту в проекті
            bool exists = await _context.Props
                .AnyAsync(p => p.IdNavigation.ProjectId == dto.ProjectId && p.PropName.ToLower() == dto.PropName.ToLower());

            if (exists) return BadRequest(new { message = "A prop with this name already exists in this project." });

            var resource = new Resource { ProjectId = dto.ProjectId };
            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProp(int id, PropCreateUpdateDto dto)
        {
            var prop = await _context.Props.FindAsync(id);
            if (prop == null) return NotFound(new { message = "Prop not found" });

            // ВАЛІДАЦІЯ: Унікальність при перейменуванні
            bool exists = await _context.Props
                .AnyAsync(p => p.IdNavigation.ProjectId == dto.ProjectId && p.Id != id && p.PropName.ToLower() == dto.PropName.ToLower());

            if (exists) return BadRequest(new { message = "A prop with this name already exists in this project." });

            prop.PropName = dto.PropName;
            prop.Description = dto.Description;
            prop.AcquisitionType = dto.AcquisitionType;
            prop.PropStatus = dto.PropStatus;
            prop.PropType = dto.PropType;

            await _context.SaveChangesAsync();
            return NoContent();
        }

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