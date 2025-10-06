using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using API.Models.DTO;
using API.Models.Entities;
using API.Models.Request;
using System.Net.Mail;
using System.Net;
using QuestPDF.Fluent;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : Controller
    {
        private readonly TechWebContext _context;

        public OrderController(TechWebContext context)
        {
            _context = context;
        }

        // tao don hang
        [HttpPost("Create")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderDTO dto)
        {
            if (dto == null || dto.UserId == 0 || dto.TotalAmount <= 0 || dto.OrderItems == null || !dto.OrderItems.Any())
                return BadRequest("Thông tin đơn hàng không hợp lệ.");

            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
                return NotFound("Người dùng không tồn tại.");

            user.IsBought = true;

            var order = new Order
            {
                UserId = dto.UserId,
                OrderDate = DateTime.Now,
                TotalAmount = dto.TotalAmount,
                OrderStatus = 0,
                OrderType = dto.OrderType,
                DiscountId = dto.DiscountId
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Lưu để có OrderId

            foreach (var item in dto.OrderItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    VariantId = item.VariantId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                };
                _context.OrderItems.Add(orderItem);
            }

            await _context.SaveChangesAsync();

            return Ok(order);
        }

        [HttpPost("SendConfirmEmail")]
        public async Task<IActionResult> SendOrderEmail([FromQuery] int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound("Đơn hàng không tồn tại.");

            var user = await _context.Users.FindAsync(order.UserId);
            if (user == null)
                return NotFound("Người dùng không tồn tại.");

            SendOrderConfirmationEmail(user.Email, order);

            return Ok("Email đã được gửi.");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] OrderUpdateDTO dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound($"Không tìm thấy đơn hàng với ID = {id}");
            if (dto.Status < 0 || dto.Status > 5)
                return BadRequest("Trạng thái không hợp lệ.");
            order.OrderStatus = dto.Status;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, "Lỗi khi cập nhật cơ sở dữ liệu.");
            }
            return NoContent();
        }

        // list order by userId
        [HttpGet("UserOrders/{userId}")]
        public async Task<IActionResult> GetOrdersByUserId(int userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                        .ThenInclude(v => v.Product)
                .Include(o => o.Discount)
                .Include(o => o.Payments)
                .ToListAsync();

            return Ok(orders);
        }


        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderDetail(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User) // 👉 để lấy tên khách hàng
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product) // 👉 để lấy tên sản phẩm
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            return Ok(order);
        }


        // update order status
        [HttpPut("UpdateStatus/{orderId}")]
        public async Task<IActionResult> UpdateStatus(int orderId, [FromBody] OrderStatusUpdateRequest request)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound();

            order.OrderStatus = request.Status;
            await _context.SaveChangesAsync();

            if (request.Status == 4) // Đã giao hàng
            {
                var user = await _context.Users.FindAsync(order.UserId);
                if (user != null)
                {
                    SendInvoiceEmail(user.Email, order);
                }
            }

            return NoContent();
        }

        [HttpDelete("DeleteAllByUser/{userId}")]
        public async Task<IActionResult> DeleteAllByUser(int userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .ToListAsync();

            if (orders == null || orders.Count == 0)
            {
                return NotFound("Không có đơn hàng nào để xóa.");
            }

            _context.Orders.RemoveRange(orders);
            await _context.SaveChangesAsync();

            user.IsBought = false;
            _context.SaveChanges();
            return Ok("Đã xóa tất cả đơn hàng của người dùng.");
            
        }
        // GET: api/Order/All
        [HttpGet("All")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                        .ThenInclude(v => v.Product)
                .Include(o => o.Discount)
                .Include(o => o.Payments)
                .ToListAsync();

            if (orders == null || orders.Count == 0)
                return NotFound("Không có đơn hàng nào.");

            var result = orders.Select(o => new
            {
                o.OrderId,
                o.UserId,
                CustomerName = o.User.FullName,
                o.OrderDate,
                o.TotalAmount,
                o.OrderStatus,
                o.OrderType,
                Discount = o.Discount != null ? new
                {
                    o.Discount.DiscountId,
                    Code = o.Discount.DiscountCode,
                    Value = o.Discount.DiscountValue
                } : null,
                Items = o.OrderItems.Select(oi => new
                {
                    oi.OrderItemId,
                    oi.VariantId,
                    ProductName = oi.Variant.Product.ProductName,
                    Color = oi.Variant.Color,
                    oi.Quantity,
                    oi.UnitPrice
                }),
                Payments = o.Payments.Select(p => new
                {
                    p.PaymentId,
                    p.Method,
                    p.Status,
                    p.Amount,
                })
            });

            return Ok(result);
        }

        [HttpPost("ConfirmOrder")]
        public IActionResult ConfirmOrder(int orderId)
        {
            var order = _context.Orders.FirstOrDefault(x => x.OrderId == orderId);
            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            if (order.OrderStatus != 0)
                return BadRequest("Đơn hàng đã được xác nhận hoặc xử lý.");

            order.OrderStatus = 1;
            _context.SaveChanges();

            return Ok("Đơn hàng đã được xác nhận thành công.");
        }

        private void SendOrderConfirmationEmail(string receiveEmail, Order order)
        {
            string fromMail = "thaigiahuy6912@gmail.com";
            string fromPassword = "nlboxztxbjxdvkpm";

            var user = _context.Users.FirstOrDefault(u => u.Email == receiveEmail);
            string fullname = user != null ? user.FullName : "";

            var orderItems = _context.OrderItems
            .Where(o => o.OrderId == order.OrderId)
            .ToList();

            var itemListHtml = string.Join("", orderItems.Select(item =>
            {
                var variant = _context.ProductVariants.FirstOrDefault(v => v.VariantId == item.VariantId);
                var product = _context.Products.FirstOrDefault(p => p.ProductId == variant.ProductId);

                string productName = product?.ProductName ?? "Sản phẩm không xác định";

                return $"<li>{productName} (Màu: {variant.Color}) - SL: {item.Quantity} - Giá: {item.UnitPrice:N0}₫</li>";
            }));


            string confirmationUrl = $"https://0m3kkrdv-3000.asse.devtunnels.ms/confirm-order?orderId={order.OrderId}";

            MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress(fromMail),
                Subject = $"Xác nhận đơn hàng #{order.OrderId}",
                IsBodyHtml = true,
                Body = $@"
                <div style='font-family: Arial; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px; background-color: #f9f9f9;'>
                    <h2 style='color: #007bff;'>🛒 Đơn hàng #{order.OrderId}</h2>
                    <p>Xin chào <strong>{fullname}</strong>,</p>
                    <p>Bạn vừa đặt hàng với hình thức <strong>thanh toán khi nhận hàng</strong>. Vui lòng kiểm tra lại đơn hàng của bạn:</p>
                    <ul>{itemListHtml}</ul>
                    <p><strong>Tổng cộng: {order.TotalAmount:N0}₫</strong></p>
                    <p>Để xác nhận đơn hàng, vui lòng nhấn vào nút dưới đây:</p>
                    <p style='text-align: center;'>
                        <a href='{confirmationUrl}' style='background-color: #28a745; color: white; padding: 12px 20px; text-decoration: none; border-radius: 5px; display: inline-block; font-size: 16px;'>XÁC NHẬN ĐƠN HÀNG</a>
                    </p>
                    <p>Nếu không phải bạn thực hiện, vui lòng bỏ qua email này.</p>
                    <p style='font-size: 12px; color: #888;'>Cảm ơn bạn đã mua hàng tại TechZone.</p>
                </div>"
            };

            mailMessage.To.Add(new MailAddress(receiveEmail));

            using (var smtpClient = new SmtpClient("smtp.gmail.com", 587))
            {
                smtpClient.Credentials = new NetworkCredential(fromMail, fromPassword);
                smtpClient.EnableSsl = true;
                smtpClient.Send(mailMessage);
            }
        }

        private void SendInvoiceEmail(string receiveEmail, Order order)
        {
            string fromMail = "thaigiahuy6912@gmail.com";
            string fromPassword = "nlboxztxbjxdvkpm";

            var user = _context.Users.FirstOrDefault(u => u.Email == receiveEmail);
            string fullname = user != null ? user.FullName : "";

            var orderItems = _context.OrderItems
                .Where(o => o.OrderId == order.OrderId)
                .ToList();

            var itemListHtml = string.Join("", orderItems.Select(item =>
            {
                var variant = _context.ProductVariants.FirstOrDefault(v => v.VariantId == item.VariantId);
                var product = _context.Products.FirstOrDefault(p => p.ProductId == variant.ProductId);

                string productName = product?.ProductName ?? "Sản phẩm không xác định";
                string color = variant?.Color ?? "Không rõ";

                return $@"
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{productName}</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{color}</td>
                    <td style='padding: 8px; border: 1px solid #ddd; text-align: center;'>{item.Quantity}</td>
                    <td style='padding: 8px; border: 1px solid #ddd; text-align: right;'>{item.UnitPrice:N0}₫</td>
                </tr>";
            }));

            string htmlBody = $@"
            <div style='font-family: Arial; max-width: 700px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px; background-color: #fff;'>
                <h2 style='color: #007bff; text-align: center;'>🧾 HÓA ĐƠN MUA HÀNG</h2>
                <p><strong>Khách hàng:</strong> {fullname}</p>
                <p><strong>Email:</strong> {receiveEmail}</p>
                <p><strong>Mã đơn hàng:</strong> #{order.OrderId}</p>
                <p><strong>Ngày đặt hàng:</strong> {DateTime.Now:dd/MM/yyyy}</p>
                <hr />
                <h3>Chi tiết đơn hàng:</h3>
                <table style='width: 100%; border-collapse: collapse;'>
                    <thead>
                        <tr>
                            <th style='padding: 8px; border: 1px solid #ddd;'>Sản phẩm</th>
                            <th style='padding: 8px; border: 1px solid #ddd;'>Màu</th>
                            <th style='padding: 8px; border: 1px solid #ddd;'>Số lượng</th>
                            <th style='padding: 8px; border: 1px solid #ddd;'>Giá</th>
                        </tr>
                    </thead>
                    <tbody>
                        {itemListHtml}
                    </tbody>
                </table>
                <p style='font-size: 18px; text-align: right;'><strong>Tổng cộng: {order.TotalAmount:N0}₫</strong></p>
                <hr />
                <p style='font-size: 12px; color: #888;'>Đây là hóa đơn điện tử. Cảm ơn bạn đã mua hàng tại TechZone.</p>
            </div>";

            MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress(fromMail),
                Subject = $"🧾 HÓA ĐƠN MUA HÀNG - Đơn #{order.OrderId}",
                IsBodyHtml = true,
                Body = htmlBody
            };

            mailMessage.To.Add(new MailAddress(receiveEmail));

            using (var smtpClient = new SmtpClient("smtp.gmail.com", 587))
            {
                smtpClient.Credentials = new NetworkCredential(fromMail, fromPassword);
                smtpClient.EnableSsl = true;
                smtpClient.Send(mailMessage);
            }
        }

        private void SendProductAvailableEmail(string receiveEmail)
        {
            string fromMail = "thaigiahuy6912@gmail.com";
            string fromPassword = "nlboxztxbjxdvkpm";

            var user = _context.Users.FirstOrDefault(u => u.Email == receiveEmail);
            string fullname = user != null ? user.FullName : "Quý khách";

            string htmlBody = $@"
            <div style='font-family: Arial; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px; background-color: #fff;'>
                <h2 style='color: #28a745; text-align: center;'>🎉 SẢN PHẨM ĐÃ RA MẮT!</h2>
                <p>Xin chào <strong>{fullname}</strong>,</p>
                <p>Sản phẩm bạn đã quan tâm và đặt trước hiện đã có mặt tại cửa hàng.</p>
                <p>Mời bạn đến cửa hàng TechZone để nhận hàng.</p>
                <p>Cảm ơn bạn đã đồng hành cùng TechZone!</p>
                <hr />
                <p style='font-size: 12px; color: #888;'>Đây là email tự động, vui lòng không trả lời.</p>
            </div>";

            MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress(fromMail),
                Subject = $"🎉 ĐÃ CÓ HÀNG - Mời bạn đến nhận",
                IsBodyHtml = true,
                Body = htmlBody
            };

            mailMessage.To.Add(new MailAddress(receiveEmail));

            using (var smtpClient = new SmtpClient("smtp.gmail.com", 587))
            {
                smtpClient.Credentials = new NetworkCredential(fromMail, fromPassword);
                smtpClient.EnableSsl = true;
                smtpClient.Send(mailMessage);
            }
        }

        [HttpPost("notify-preorder-customers")]
        public IActionResult NotifyPreOrderCustomers()
        {
            var customers = (from order in _context.Orders
                             join user in _context.Users on order.UserId equals user.UserId
                             where order.OrderType == 2 && order.OrderStatus == 1
                             select new
                             {
                                 user.Email,
                                 user.FullName
                             })
                            .Distinct()
                            .ToList();

            int successCount = 0;
            int failCount = 0;

            foreach (var customer in customers)
            {
                try
                {
                    SendProductAvailableEmail(customer.Email);
                    successCount++;
                }
                catch
                {
                    failCount++;
                }
            }

            return Ok(new
            {
                Message = $"Đã gửi email thành công cho {successCount} khách hàng. Thất bại: {failCount}."
            });
        }



        [HttpGet("CheckDeposit/{userId}")]
        public IActionResult CheckDeposit(int userId)
        {
            var existingDeposit = _context.Orders
                .FirstOrDefault(o => o.UserId == userId && o.OrderType == 2 && o.OrderStatus != 5 && o.OrderStatus != 4);

            if (existingDeposit != null)
            {
                return Ok(new
                {
                    HasDeposit = true,
                    ExistingOrder = existingDeposit
                });
            }

            return Ok(new { HasDeposit = false });
        }


    }
}
