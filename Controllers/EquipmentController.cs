
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LabEquipmentApi.Data;
using LabEquipmentApi.Domain;
using LabEquipmentApi.Contracts.Equipment;
using Mapster;
using FluentValidation;

namespace LabEquipmentApi.Controllers
{
    [Route("api/equipment")]
    [ApiController]
    public class EquipmentController : ControllerBase
    {
        private readonly LabDbContext _context;
        private readonly IValidator<CreateEquipmentRequest> _createValidator;

        public EquipmentController(
            LabDbContext context,
            IValidator<CreateEquipmentRequest> createValidator)
        {
            _context = context;
            _createValidator = createValidator;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Equipment>>> GetEquipmentItems()
        {
            return await _context.EquipmentItems.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Equipment>> GetEquipment(int id)
        {
            var equipment = await _context.EquipmentItems.FindAsync(id);

            if (equipment == null)
            {
                return NotFound();
            }

            return equipment;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutEquipment(int id, Equipment equipment)
        {
            if (id != equipment.Id)
            {
                return BadRequest();
            }

            _context.Entry(equipment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EquipmentExists(id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<Equipment>> PostEquipment(CreateEquipmentRequest request)
        {
            var validationResult = await _createValidator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }

                return ValidationProblem(ModelState);
            }

            var equipment = new Equipment
            {
                Name = request.Name,
                Room = request.Room,
                IsAvailable = true
            };

            _context.EquipmentItems.Add(equipment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEquipment),
                new { id = equipment.Id },
                equipment);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEquipment(int id)
        {
            var equipment = await _context.EquipmentItems.FindAsync(id);

            if (equipment == null)
            {
                return NotFound();
            }

            _context.EquipmentItems.Remove(equipment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<EquipmentListItemResponse>>> GetAvailableEquipment()
        {
            var equipment = await _context.EquipmentItems
                .Where(e => e.IsAvailable)
                .OrderBy(e => e.Name)
                .ProjectToType<EquipmentListItemResponse>()
                .ToListAsync();

            return Ok(equipment);
        }

        private bool EquipmentExists(int id)
        {
            return _context.EquipmentItems.Any(e => e.Id == id);
        }
    }
}
