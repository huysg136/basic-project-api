using API.Models.DTO;
using API.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : Controller
    {
        private readonly TechWebContext _context;

        public CategoryController(TechWebContext context)
        {
            _context = context;
        }

        //lay tat ca category
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.CategoryName,
                    ParentCategoryId = c.ParentCategoryId,
                    Image = c.Image,
                })
                .ToListAsync();
            return Ok(categories);
        }


        //lay category con
        [HttpGet("has-parent")]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategoriesWithParent()
        {
            var categoriesWithParent = await _context.Categories
                .Where(c => c.ParentCategoryId != null)
                .ToListAsync();
            return Ok(categoriesWithParent);
        }

        
        //lay category cha
        [HttpGet("no-parent")]
        public async Task<ActionResult<IEnumerable<Category>>> GetParentCategories()
        {
            var parentCategories = await _context.Categories
                .Where(c => c.ParentCategoryId == null)
                .ToListAsync();

            return Ok(parentCategories);
        }

        [HttpGet("by-category/{categoryId}")]
        public async Task<IActionResult> GetProductsByCategory(int categoryId)
        {
            var products = await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .Select(p => new {
                    p.ProductId,
                    p.ProductName,
                    p.Price,
                    p.OriginalPrice,
                    p.Discount,
                    p.Image
                })
                .ToListAsync();

            return Ok(products);
        }

        // POST: api/category
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryDto dto)
        {
            if (dto == null)
                return BadRequest("Dữ liệu không hợp lệ");

            // Kiểm tra ParentCategoryId có tồn tại (nếu không null)
            if (dto.ParentCategoryId != null)
            {
                var parentExists = await _context.Categories.AnyAsync(c => c.CategoryId == dto.ParentCategoryId);
                if (!parentExists)
                    return BadRequest("Danh mục cha không tồn tại");
            }

            var category = new Category
            {
                CategoryName = dto.Name,
                ParentCategoryId = dto.ParentCategoryId,
                Image = dto.Image
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategories), new { id = category.CategoryId }, category);
        }

        // PUT: api/category/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDto dto)
        {
            if (dto == null || id != dto.CategoryId)
                return BadRequest("Dữ liệu không hợp lệ");

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound("Danh mục không tồn tại");

            // Kiểm tra ParentCategoryId không trùng chính nó (kiểm tra vòng lặp)
            if (dto.ParentCategoryId == id)
                return BadRequest("Danh mục cha không thể trùng danh mục hiện tại");

            // Kiểm tra ParentCategoryId có tồn tại (nếu không null)
            if (dto.ParentCategoryId != null)
            {
                var parentExists = await _context.Categories.AnyAsync(c => c.CategoryId == dto.ParentCategoryId);
                if (!parentExists)
                    return BadRequest("Danh mục cha không tồn tại");
            }

            category.CategoryName = dto.Name;
            category.ParentCategoryId = dto.ParentCategoryId;
            category.Image = dto.Image;

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/category/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound("Danh mục không tồn tại");

            // Kiểm tra có danh mục con không, không cho xóa nếu có
            var hasChild = await _context.Categories.AnyAsync(c => c.ParentCategoryId == id);
            if (hasChild)
                return BadRequest("Không thể xóa danh mục vì còn danh mục con");

            // TODO: nếu có liên kết sản phẩm, có thể cần kiểm tra thêm trước khi xóa

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/category/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var category = await _context.Categories
                .Where(c => c.CategoryId == id)
                .Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.CategoryName,
                    ParentCategoryId = c.ParentCategoryId,
                    Image = c.Image
                })
                .FirstOrDefaultAsync();

            if (category == null)
                return NotFound("Không tìm thấy danh mục");

            return Ok(category);
        }
    }
}
