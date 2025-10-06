using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Models.Entities;
using API.Models.DTO;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShoppingCartController : ControllerBase
    {
        private readonly TechWebContext _context;

        public ShoppingCartController(TechWebContext context)
        {
            _context = context;
        }

        // GET: api/ShoppingCart/ItemCount/5
        [HttpGet("ItemCount/{userId}")]
        public async Task<IActionResult> GetItemCount(int userId)
        {
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
                return Ok(new { itemCount = 0 });

            var itemCount = await _context.CartDetails
                .Where(cd => cd.CartId == cart.CartId)
                .SumAsync(cd => cd.Quantity);

            return Ok(new { itemCount });
        }

        // GET: api/ShoppingCart/GetCartItems/5
        [HttpGet("GetCartItems/{userId}")]
        public async Task<IActionResult> GetCartItems(int userId)
        {
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
                return NotFound("Giỏ hàng không tồn tại.");

            var items = await _context.CartDetails
                .Where(cd => cd.CartId == cart.CartId)
                .Include(cd => cd.Variant)
                    .ThenInclude(pv => pv.Product)
                .Select(cd => new
                {
                    VariantId = cd.VariantId,
                    ProductName = cd.Variant.Product.ProductName,
                    VariantColor = cd.Variant.Color,
                    Image = cd.Variant.Image,
                    Price = cd.UnitPrice,
                    Quantity = cd.Quantity,
                    TotalPrice = cd.Quantity * cd.UnitPrice
                })
                .ToListAsync();

            return Ok(items);
        }

        // POST: api/ShoppingCart/Add
        [HttpPost("Add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            if (dto.UserId <= 0 || dto.ProductVariantId <= 0 || dto.Quantity <= 0)
                return BadRequest("Thông tin không hợp lệ.");

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == dto.UserId);
            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = dto.UserId,
                    CreatedAt = DateTime.Now
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var cartDetail = await _context.CartDetails
                .FirstOrDefaultAsync(cd => cd.CartId == cart.CartId && cd.VariantId == dto.ProductVariantId);

            if (cartDetail != null)
            {
                cartDetail.Quantity += dto.Quantity;
            }
            else
            {
                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.VariantId == dto.ProductVariantId);

                if (variant == null)
                    return NotFound("Không tìm thấy biến thể sản phẩm.");

                cartDetail = new CartDetail
                {
                    CartId = cart.CartId,
                    VariantId = dto.ProductVariantId,
                    Quantity = dto.Quantity,
                    UnitPrice = variant.Product.Price,
                    CreatedAt = DateTime.Now
                };

                _context.CartDetails.Add(cartDetail);
            }

            await _context.SaveChangesAsync();
            return Ok("Đã thêm sản phẩm vào giỏ hàng.");
        }

        // DELETE: api/ShoppingCart/Delete?userId=1&productVariantId=5
        [HttpDelete("Delete")]
        public async Task<IActionResult> DeleteCartItem(int userId, int productVariantId)
        {
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
                return NotFound("Không tìm thấy giỏ hàng.");

            var cartDetail = await _context.CartDetails
                .FirstOrDefaultAsync(cd => cd.CartId == cart.CartId && cd.VariantId == productVariantId);

            if (cartDetail == null)
                return NotFound("Không tìm thấy sản phẩm trong giỏ hàng.");

            _context.CartDetails.Remove(cartDetail);
            await _context.SaveChangesAsync();

            return Ok("Đã xóa sản phẩm khỏi giỏ hàng.");
        }

        // DELETE: api/ShoppingCart/Clear?userId=1
        [HttpDelete("Clear")]
        public async Task<IActionResult> ClearCart(int userId)
        {
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
                return NotFound("Không tìm thấy giỏ hàng.");

            var cartDetails = await _context.CartDetails
                .Where(cd => cd.CartId == cart.CartId)
                .ToListAsync();

            if (cartDetails == null || !cartDetails.Any())
                return Ok("Giỏ hàng đã trống.");

            _context.CartDetails.RemoveRange(cartDetails);
            await _context.SaveChangesAsync();

            return Ok("Đã xóa toàn bộ sản phẩm trong giỏ hàng.");
        }

    }
}
