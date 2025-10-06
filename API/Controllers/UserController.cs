using API.Models.DTO;
using API.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly TechWebContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public UserController(TechWebContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        [HttpDelete("delete/{username}")]
        public IActionResult DeleteUser(string username)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                return NotFound("Không tìm thấy tài khoản!");
            }

            _context.Users.Remove(user);
            _context.SaveChanges();

            return Ok($"Tài khoản {username} đã được xóa thành công.");
        }

        [HttpGet("get/{userId:int}")]
        public async Task<IActionResult> GetUser(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return Ok(new
            {
                user.UserId,
                user.Username,
                user.FullName,
                user.Email,
                user.Address,
                user.PhoneNumber,
                user.Role,
                user.IsActive,
                user.IsBought
            });
        }

        [HttpPut("update/{userId:int}")]
        public async Task<IActionResult> UpdateUserProfile(int userId, [FromBody] UpdateUserProfileDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User không tồn tại");

            // Kiểm tra Email duy nhất
            if (user.Email != dto.Email)
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != userId);
                if (emailExists)
                    return BadRequest("Email đã được sử dụng bởi người khác");
                user.Email = dto.Email;
                // TODO: Gửi email xác thực nếu cần
            }

            // Kiểm tra số điện thoại duy nhất
            if (user.PhoneNumber != dto.PhoneNumber)
            {
                var phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber && u.UserId != userId);
                if (phoneExists)
                    return BadRequest("Số điện thoại đã được sử dụng bởi người khác");
                user.PhoneNumber = dto.PhoneNumber;
            }

            user.FullName = dto.FullName;
            user.Address = dto.Address;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok("Cập nhật thông tin thành công");
        }


        [HttpPut("change-password/{userId:int}/changepassword")]
        public async Task<IActionResult> ChangePassword(int userId, [FromBody] ChangePasswordDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User không tồn tại");

            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, dto.CurrentPassword);
            if (result == PasswordVerificationResult.Failed)
                return BadRequest("Mật khẩu hiện tại không đúng");

            user.Password = _passwordHasher.HashPassword(user, dto.NewPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok("Đổi mật khẩu thành công");
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    u.Address,
                    u.Role,
                    u.IsActive,
                    u.IsBought,
                    u.CreatedAt,
                    u.LastLogin
                })
                .ToListAsync();

            return Ok(users);
        }


        [HttpPut("{userId:int}/role")]
        public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] byte newRole)
        {
            if (newRole < 1 || newRole > 3)
                return BadRequest("Role không hợp lệ");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("Người dùng không tồn tại");

            user.Role = newRole;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok("Cập nhật quyền thành công");
        }

        [HttpPut("{userId:int}/toggle-active")]
        public async Task<IActionResult> ToggleActiveStatus(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("Người dùng không tồn tại");

            user.IsActive = !user.IsActive;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok($"Tài khoản đã được {(user.IsActive == true ? "mở khóa" : "khóa")}"); 
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers(string? keyword)
        {
            if (string.IsNullOrEmpty(keyword))
                return await GetAllUsers(); // Reuse method

            var users = await _context.Users
                .Where(u => u.Username.Contains(keyword) || u.Email.Contains(keyword))
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    u.Address,
                    u.Role,
                    u.IsActive,
                    u.IsBought,
                    u.CreatedAt,
                    u.LastLogin
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}
