using API.Models.DTO;
using API.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Net.payOS.Types;
using Net.payOS;
using System;
using Net.payOS.Types;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly TechWebContext _context;

        public PaymentsController(TechWebContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPayments()
        {
            var payments = await _context.Payments
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(payments);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Payment>> GetPayment(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return NotFound();

            return Ok(payment);
        }

        [HttpPost]
        public async Task<ActionResult<Payment>> CreatePayment([FromBody] PaymentCreateDTO dto)
        {
            // Kiểm tra đơn hàng có tồn tại không
            var order = await _context.Orders.FindAsync(dto.OrderId);
            if (order == null)
                return BadRequest("Đơn hàng không tồn tại.");

            if (dto.Amount <= 0)
                return BadRequest("Số tiền phải lớn hơn 0.");

            // Tạo payment mới
            var payment = new Payment
            {
                OrderId = dto.OrderId,
                Method = dto.Method,
                Note = dto.Note,
                Amount = dto.Amount,
                CreatedAt = DateTime.Now
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Trả về mã 201 Created cùng với dữ liệu payment mới tạo
            return CreatedAtAction(nameof(GetPayment), new { id = payment.PaymentId }, payment);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePayment(int id, Payment payment)
        {
            if (id != payment.PaymentId)
                return BadRequest();

            var existing = await _context.Payments.FindAsync(id);
            if (existing == null)
                return NotFound();

            existing.Status = payment.Status;
            existing.Method = payment.Method;
            existing.Amount = payment.Amount;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    

        // GET: api/Payments/ByOrder/100001
        [HttpGet("ByOrder/{orderId}")]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPaymentsByOrder(int orderId)
        {
            var exists = await _context.Orders.AnyAsync(o => o.OrderId == orderId);
            if (!exists)
                return NotFound($"Không tìm thấy đơn hàng với ID = {orderId}");

            var payments = await _context.Payments
                .Where(p => p.OrderId == orderId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(payments);
        }
    }
}
