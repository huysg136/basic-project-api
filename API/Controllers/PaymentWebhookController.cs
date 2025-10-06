using API.Models.DTO;
using API.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/payment/webhook")]
    [ApiController]
    public class PaymentWebhookController : Controller
    {
        private readonly TechWebContext _context;

        public PaymentWebhookController(TechWebContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveWebhook([FromBody] PayOSWebhookRequest request)
        {
            var data = request.Data;

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == data.OrderCode);

            if (order == null)
            {
                return NotFound();
            }

            if (data.Status == "PAID")   // hoặc so sánh kiểu khác tùy PayOS
            {
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == order.OrderId);

                if (existingPayment == null)
                {
                    var payment = new Payment
                    {
                        OrderId = order.OrderId,
                        Amount = order.TotalAmount,
                        Method = 1,
                        Note = "Thanh toán qua PayOS (Webhook)"
                    };

                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { success = true });
        }

    }
}
