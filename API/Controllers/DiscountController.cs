using API.Models.DTO;
using API.Models.Entities;
using API.Models.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiscountController : Controller
    {
        private readonly TechWebContext _context;

        public DiscountController(TechWebContext context)
        {
            _context = context;
        }

        [HttpPost("Validate")]
        public async Task<IActionResult> ValidateDiscount([FromBody] DiscountRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                return BadRequest("Bạn chưa nhập mã giảm giá.");
            var discounts = await _context.Discounts.Where(d => d.IsValid == true).ToListAsync();
            var discount = discounts.FirstOrDefault(d =>
                d.DiscountCode.Equals(request.Code, StringComparison.Ordinal));

            if (discount == null)
                return NotFound("Mã giảm giá không tồn tại hoặc đã hết hạn.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId);
            if (user == null)
                return NotFound("Không tìm thấy người dùng.");

            if (user.IsBought != false && request.Code == "NGUOIMOI")
                return BadRequest("Bạn không thuộc đối tượng được áp dụng mã giảm giá này.");

            return Ok(new
            {
                discount.DiscountId,
                discount.DiscountCode,
                discount.DiscountValue
            });
        }

    }
}
