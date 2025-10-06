using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Net.payOS;
using Net.payOS.Types;
using API.Models.Entities;
using API.Models.Request;
using API.Models.DTO;

namespace WebApp.Controllers
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebHost.CreateDefaultBuilder(args)
              .UseUrls("http://localhost:3030/")
              .UseWebRoot("public")
              .UseStartup<Startup>()
              .Build()
              .Run();
        }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
            app.UseRouting();
            app.UseStaticFiles();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/", context =>
                {
                    context.Response.Redirect("/index.html");
                    return Task.CompletedTask;
                });
            });
        }
    }

    [Route("api/create-payment-link")]
    public class CheckoutApiController : Controller
    {
        private readonly TechWebContext _context;
        private readonly PayOS _payOS;
        private readonly string _domain = "https://localhost:7031";

        public CheckoutApiController(TechWebContext context)
        {
            _context = context;
            _payOS = new PayOS(
                "f2b421c9-c7ac-4633-ba54-782cc91afbcf",
                "5ee08569-2dfd-43e6-b802-612f948ae4fc",
                "e6d93f47e6ef05c86dd68d8a15dda60ab2754607bc8a2d6180c3ecc5de9f2854"
            );
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePaymentRequest paymentRequest)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                        .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(o => o.OrderId == paymentRequest.OrderId);

            if (order == null)
            {
                return NotFound(new { message = "Đơn hàng không tồn tại!" });
            }

            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == order.OrderId);

            if (existingPayment != null)
            {
                return BadRequest(new { message = "Đơn hàng này đã có thanh toán!" });
            }

            // ✅ Lấy số tiền từ frontend gửi lên
            int paymentAmount = paymentRequest.Amount > 0
                ? paymentRequest.Amount
                : (int)order.TotalAmount;   // fallback

            var description = $"Đơn hàng #{order.OrderId}";

            var items = order.OrderItems.Select(oi =>
                new ItemData(
                    name: $"{oi.Variant.Product.ProductName} - {oi.Variant.Color}",
                    quantity: oi.Quantity,
                    price: (int)Math.Round(oi.UnitPrice * oi.Quantity)
                )
            ).ToList();

            var paymentLinkRequest = new PaymentData(
                orderCode: order.OrderId,
                amount: paymentAmount,  // 💰 Sử dụng số tiền do frontend chỉ định
                description: description,
                items: items,
                returnUrl: $"https://0m3kkrdv-3000.asse.devtunnels.ms/payment-result?orderId={order.OrderId}&status=success",
                cancelUrl: $"https://0m3kkrdv-3000.asse.devtunnels.ms/payment-result?orderId={order.OrderId}&status=cancel"
            );

            var response = await _payOS.createPaymentLink(paymentLinkRequest);

            if (response == null || string.IsNullOrEmpty(response.checkoutUrl))
            {
                return StatusCode(500, new { message = "Không thể tạo link thanh toán!" });
            }

            return Ok(new { checkoutUrl = response.checkoutUrl });
        }


        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmPayment([FromBody] int orderId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound(new { message = "Đơn hàng không tồn tại!" });
            }

            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == orderId);

            if (existingPayment != null)
            {
                return BadRequest(new { message = "Đơn hàng này đã được thanh toán!" });
            }

            var payment = new Payment
            {
                OrderId = orderId,
                Amount = order.TotalAmount,
                Method = 1,
                Note = "Thanh toán QR VietQR"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xác nhận thanh toán thành công!" });
        }

    }
}
