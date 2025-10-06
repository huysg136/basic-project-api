using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Models.Entities;
using API.Models.DTO;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : Controller
    {
        private readonly TechWebContext _context;

        public ProductController(TechWebContext context)
        {
            _context = context;
        }

        // GET: api/Product
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductVariants)
                .ToListAsync();

            return Ok(products);
        }

        // GET: api/Product/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null)
                return NotFound();

            return product;
        }

        // GET: api/Product/GetColors/5
        [HttpGet("GetColors/{productId}")]
        public async Task<ActionResult<IEnumerable<string>>> GetProductColors(int productId)
        {
            var colors = await _context.ProductVariants
                .Where(v => v.ProductId == productId)
                .Select(v => v.Color)
                .Distinct()
                .ToListAsync();

            if (colors == null || colors.Count == 0)
            {
                return NotFound();
            }

            return Ok(colors);
        }


        // POST: api/Product
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDto request)
        {
            // 1. Tạo product chính
            var product = new Product
            {
                ProductName = request.ProductName,
                Description = request.Description,
                OriginalPrice = request.OriginalPrice,
                Price = request.Price,
                Discount = request.Discount,
                CategoryId = request.CategoryId,
                Image = request.ImagePath,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // 2. Tạo các variant
            var variants = request.ProductVariants.Select(v => new ProductVariant
            {
                ProductId = product.ProductId,
                Color = v.Color,
                Status = v.Status,
                Image = v.ImagePath 
            }).ToList();

            _context.ProductVariants.AddRange(variants);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
        }



        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpdateDto dto)
        {
            var product = await _context.Products
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            // Cập nhật thông tin sản phẩm
            product.ProductName = dto.ProductName;
            product.Description = dto.Description;
            product.OriginalPrice = dto.OriginalPrice;
            product.Price = dto.Price;
            product.Discount = (byte)dto.Discount;
            product.CategoryId = dto.CategoryId;
            product.Image = dto.ImagePath;
            product.UpdatedAt = DateTime.Now;

            // Xóa các variant đã bị đánh dấu xóa
            if (dto.DeletedVariants != null)
            {
                var variantsToDelete = product.ProductVariants.Where(v => dto.DeletedVariants.Contains(v.VariantId)).ToList();
                _context.ProductVariants.RemoveRange(variantsToDelete);
            }

            // Cập nhật hoặc thêm mới biến thể
            foreach (var v in dto.ProductVariants)
            {
                if (v.IsNew || v.VariantId == 0 || v.VariantId == null)
                {
                    product.ProductVariants.Add(new ProductVariant
                    {
                        Color = v.Color,
                        Image = v.ImagePath,
                        Status = v.Status,
                        CreatedAt = DateTime.Now
                    });
                }
                else
                {
                    var variant = product.ProductVariants.FirstOrDefault(x => x.VariantId == v.VariantId);
                    if (variant != null)
                    {
                        variant.Color = v.Color;
                        variant.Image = v.ImagePath;
                        variant.Status = v.Status;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }


        // DELETE: api/Product/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            // Nếu cần, xóa luôn các Variant con (nếu Cascade không bật sẵn)
            _context.ProductVariants.RemoveRange(product.ProductVariants);

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Product/GetByCategory/100033
        [HttpGet("GetByCategory/{categoryId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetProductsByCategory(int categoryId)
        {
            var products = await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .Select(p => new
                {
                    p.ProductId,
                    p.ProductName,
                    p.Price
                })
                .ToListAsync();

            if (products == null || products.Count == 0)
            {
                return NotFound();
            }

            return Ok(products);
        }

    }
}
