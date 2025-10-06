using API.Models.DTO;
using API.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiscountsController : Controller
    {
        private readonly TechWebContext _context;

        public DiscountsController(TechWebContext context)
        {
            _context = context;
        }

        // GET: api/discounts
        [HttpGet]
        public async Task<IActionResult> GetAllDiscounts()
        {
            var discounts = await _context.Discounts
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return Ok(discounts);
        }


        // GET: api/discounts/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDiscount(int id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null) return NotFound();

            return Ok(discount);
        }

        // PUT: api/discounts/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDiscount(int id, [FromBody] DiscountUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null) return NotFound();

            // Kiểm tra nếu DiscountCode thay đổi thì phải đảm bảo không trùng
            if (!string.Equals(discount.DiscountCode, dto.DiscountCode, StringComparison.OrdinalIgnoreCase))
            {
                var codeExists = await _context.Discounts
                    .AnyAsync(d => d.DiscountCode == dto.DiscountCode && d.DiscountId != id);
                if (codeExists)
                    return Conflict("Another discount with this code already exists.");

                discount.DiscountCode = dto.DiscountCode;
            }

            discount.DiscountValue = dto.DiscountValue;
            discount.IsValid = dto.IsValid;

            await _context.SaveChangesAsync();
            return Ok(discount);
        }

        // POST: api/discounts
        [HttpPost]
        public async Task<IActionResult> CreateDiscount([FromBody] DiscountCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var exists = await _context.Discounts.AnyAsync(d => d.DiscountCode == dto.DiscountCode);
            if (exists) return Conflict("Mã giảm giá đã tồn tại.");

            var newDiscount = new Discount
            {
                DiscountCode = dto.DiscountCode,
                DiscountValue = dto.DiscountValue,
                IsValid = dto.IsValid,
                CreatedAt = DateTime.UtcNow
            };

            _context.Discounts.Add(newDiscount);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDiscount), new { id = newDiscount.DiscountId }, newDiscount);
        }


    }
}
