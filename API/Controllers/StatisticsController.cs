using Microsoft.AspNetCore.Mvc;
using API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : Controller
    {
        private readonly TechWebContext _context;

        public StatisticsController(TechWebContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetStatistics(
            [FromQuery] string? preset,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            DateTime now = DateTime.Now;
            DateTime startDate;
            DateTime endDate = now;

            switch (preset)
            {
                case "last7days":
                    startDate = now.AddDays(-7);
                    break;
                case "last30days":
                    startDate = now.AddDays(-30);
                    break;
                case "thismonth":
                    startDate = new DateTime(now.Year, now.Month, 1);
                    break;
                case "alltime":
                    startDate = DateTime.MinValue;
                    break;
                default:
                    if (from.HasValue && to.HasValue)
                    {
                        startDate = from.Value.Date;
                        endDate = to.Value.Date.AddDays(1).AddTicks(-1); // đến hết ngày "to"
                    }
                    else
                    {
                        startDate = DateTime.MinValue;
                    }
                    break;
            }

            // Đặt lại mốc thời gian cho thống kê ngày, tháng
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
            var today = now.Date;

            // Tối ưu hóa điều kiện filter
            var userQuery = _context.Users.Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate);
            var orderQuery = _context.Orders.Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate);
            var paymentQuery = _context.Payments.Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate);
            var productQuery = _context.Products.Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate);
            

            // Thống kê người dùng
            var totalUsers = await userQuery.CountAsync();
            var totalAdmins = await userQuery.Where(u => u.Role == 1).CountAsync();
            var totalCustomers = await userQuery.Where(u => u.Role == 2).CountAsync();
            var totalEmployees = await userQuery.Where(u => u.Role == 3).CountAsync();
            var newUsersThisMonth = await _context.Users.Where(u => u.CreatedAt >= firstDayOfMonth).CountAsync();

            // Thống kê đơn hàng
            var totalOrders = await orderQuery.CountAsync();
            var pendingOrders = await orderQuery.Where(o => o.OrderStatus == 0).CountAsync();
            var confirmedOrders = await orderQuery.Where(o => o.OrderStatus == 1).CountAsync();
            var waitingPickupOrders = await orderQuery.Where(o => o.OrderStatus == 2).CountAsync();
            var waitingDeliveryOrders = await orderQuery.Where(o => o.OrderStatus == 3).CountAsync();
            var completedOrders = await orderQuery.Where(o => o.OrderStatus == 4).CountAsync();
            var cancelledOrders = await orderQuery.Where(o => o.OrderStatus == 5).CountAsync();

            // Doanh thu
            var totalRevenue = await paymentQuery.SumAsync(p => (decimal?)p.Amount) ?? 0;
            var monthlyRevenue = await _context.Payments
                .Where(p => p.CreatedAt >= firstDayOfMonth)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;
            var dailyRevenue = await _context.Payments
                .Where(p => p.CreatedAt >= today)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            // Sản phẩm, danh mục
            //var totalProducts = await productQuery.CountAsync();
            var totalProducts = await _context.Products.CountAsync();
            var totalCategories = await _context.Categories.CountAsync();

            // Top 5 sản phẩm bán chạy nhất (nếu có bảng OrderItems)
            var topProducts = await (
                from oi in _context.OrderItems
                join pv in _context.ProductVariants on oi.VariantId equals pv.VariantId
                join p in _context.Products on pv.ProductId equals p.ProductId
                group oi by new { p.ProductId, p.ProductName } into g
                select new
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    QuantitySold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(5)
                .ToListAsync();


            return Ok(new
            {
                // User stats
                totalUsers,
                totalAdmins,
                totalCustomers,
                totalEmployees,
                newUsersThisMonth,

                // Order stats
                totalOrders,
                pendingOrders,
                confirmedOrders,
                waitingPickupOrders,
                waitingDeliveryOrders,
                completedOrders,
                cancelledOrders,

                // Revenue stats
                totalRevenue,
                monthlyRevenue,
                dailyRevenue,

                // Product stats
                totalProducts,
                totalCategories,

                // Best sellers
                topProducts
            });
        }
    }
}
