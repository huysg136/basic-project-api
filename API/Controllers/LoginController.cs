using Microsoft.AspNetCore.Mvc;
using API.Models.Entities;
using API.Models.Request;
using API.Models.DTO;
using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using Org.BouncyCastle.Crypto.Generators;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : Controller
    {
        private readonly TechWebContext db;
        private static Dictionary<string, DataOTP> otpStorage = new();
        public LoginController(TechWebContext db)
        {
            this.db = db;
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("TechWebJwtSecretKey1234567890@123456")); // nên để ở appsettings
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim("userId", user.UserId.ToString()),
                new Claim("role", user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "TechWebIssuer",
                audience: "TechWebAudience",
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        // dang nhap
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest lr)
        {
            var user = db.Users.FirstOrDefault(u => u.Username == lr.Username);
            if (user == null)
            {
                return BadRequest("Không tìm thấy tài khoản!");
            }

            var passwordHash = new PasswordHasher<User>();
            var result = passwordHash.VerifyHashedPassword(user, user.Password, lr.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                return BadRequest("Sai mật khẩu!");
            }
            else if (user.IsActive == false)
            {
                return BadRequest($"Tài khoản {user.Username} đã bị khóa! Vui lòng liên hệ thaigiahuy6912@gmail.com để mở khóa.");
            }
            else
            {
                user.LastLogin = DateTime.Now;
                db.SaveChanges();
                return Ok(new
                {
                    Token = GenerateJwtToken(user),
                    UserId = user.UserId,
                    Message = "Đăng nhập thành công!",
                    Role = user.Role == 1 ? "Admin" : user.Role == 2 ? "Customer" : user.Role == 3 ? "Staff" : "Unknown",
                    FullName = user.FullName,
                    Username = user.Username,
                    Email = user.Email,
                    user.IsBought
                });
            }

        }
        // kiem tra mat khau manh
        private bool isStrongPass(string password)
        {
            if (password == null)
            {
                return false;
            }
            if (password.Length < 8)
            {
                return false;
            }
            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                return false;
            }
            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                return false;
            }
            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                return false;
            }
            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?\:{ }|<>""]"))
            {
                return false;
            }
            return true;
        }

        //dang ky
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest rr)
        {
            if (rr.Username.Length < 5)
            {
                return BadRequest("Tên tài khoản quá ngắn!");
            }

            if (!isStrongPass(rr.Password))
            {
                return BadRequest("Mật khẩu quá yếu!");
            }

            var checkUsernameExists = db.Users.FirstOrDefault(u => u.Username == rr.Username);
            if (checkUsernameExists != null)
            {
                return BadRequest("Tên đăng nhập đã tồn tại!");
            }

            var checkEmailExists = db.Users.FirstOrDefault(u => u.Email == rr.Email);
            if (checkEmailExists != null)
            {
                return BadRequest("Email đã được sử dụng!");
            }

            var checkPhoneExists = db.Users.FirstOrDefault(u => u.PhoneNumber == rr.PhoneNumber);
            if (checkPhoneExists != null)
            {
                return BadRequest("Số điện thoại đã được sử dụng!");
            }

            var user = new User
            {
                Username = rr.Username,
                FullName = rr.FullName,
                Email = rr.Email,
                Role = 2,
                Address = rr.Address,
                PhoneNumber = rr.PhoneNumber,
            };

            var passwordHasher = new PasswordHasher<User>();
            user.Password = passwordHasher.HashPassword(user, rr.Password); // ✅ đúng!

            db.Add(user);
            db.SaveChanges();
            return Ok("Đăng ký thành công!");
        }


        private static int GenerateOTP()
        {
            Random random = new Random();
            return random.Next(100000, 999999);
        }

        private void sendMail(string receiveEmail)
        {
            string fromMail = "thaigiahuy6912@gmail.com";
            string fromPassword = "nlboxztxbjxdvkpm";
            int otp = GenerateOTP(); // tạo otp
            otpStorage[receiveEmail] = new DataOTP { OtpCode = otp, GeneratedAt = DateTime.Now };
            var user = db.Users.FirstOrDefault(u => u.Email == receiveEmail);
            string fullname = user != null ? user.FullName : "";

            MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress(fromMail),
                Subject = "OTP",
                IsBodyHtml = true,
                Body = $@"
                <div style='max-width: 500px; margin: auto; padding: 25px; font-family: Arial, sans-serif; border: 1px solid #e0e0e0; border-radius: 12px; background-color: #f9f9f9; box-shadow: 0px 4px 8px rgba(0,0,0,0.1); text-align: center;'>
                    <h2 style='color: #007bff; margin-bottom: 10px;'>🔐 OTP</h2>
                    <hr style='border: none; height: 1px; background-color: #ddd; margin-bottom: 20px;'/>
                    <p style='font-size: 16px; color: #333; margin-bottom: 15px;'>Hello, <strong>{fullname}</strong></p>    
                    <p style='font-size: 16px; color: #555; margin-bottom: 15px;'>Your One-Time Password (OTP) for resetting your password is:</p>
                    <h1 style='color: #28a745; font-size: 40px; margin: 10px 0; letter-spacing: 2px;'>{otp}</h1>
                    <p style='font-size: 14px; color: #777; margin-top: 15px;'>This OTP is valid for a limited time. Please do not share it with anyone.</p>
                    <p style='font-size: 14px; color: #777;'>If you did not request this, please ignore this email.</p>
                    <hr style='border: none; height: 1px; background-color: #ddd; margin: 20px 0;'/>
                    <p style='font-size: 12px; color: #999;'>&copy; {DateTime.Now.Year} Thái Gia Huy. All rights reserved.</p>
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

        private void sendVerificationEmail(string receiveEmail)
        {
            string fromMail = "thaigiahuy6912@gmail.com";
            string fromPassword = "nlboxztxbjxdvkpm";

            int otp = GenerateOTP(); 

            otpStorage[receiveEmail] = new DataOTP { OtpCode = otp, GeneratedAt = DateTime.Now };

            MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress(fromMail),
                Subject = "Mã OTP xác nhận email",
                IsBodyHtml = true,
                Body = $@"
                <div style='max-width: 500px; margin: auto; padding: 25px; font-family: Arial, sans-serif; border: 1px solid #e0e0e0; border-radius: 12px; background-color: #f9f9f9; box-shadow: 0px 4px 8px rgba(0,0,0,0.1); text-align: center;'>
                    <h2 style='color: #007bff; margin-bottom: 10px;'>🔐 Mã OTP xác nhận</h2>
                    <hr style='border: none; height: 1px; background-color: #ddd; margin-bottom: 20px;'/>
                    <p style='font-size: 16px; color: #333; margin-bottom: 15px;'>Xin chào,</p>
                    <p style='font-size: 16px; color: #555; margin-bottom: 15px;'>
                        Đây là mã OTP dùng để xác thực email cho tài khoản TechZone của bạn:
                    </p>
                    <h1 style='color: #28a745; font-size: 40px; margin: 10px 0; letter-spacing: 2px;'>{otp}</h1>
                    <p style='font-size: 14px; color: #777; margin-top: 15px;'>Mã OTP có hiệu lực trong thời gian giới hạn. Vui lòng không chia sẻ mã này với người khác.</p>
                    <p style='font-size: 14px; color: #777;'>Nếu bạn không yêu cầu đăng ký tài khoản, vui lòng bỏ qua email này.</p>
                    <hr style='border: none; height: 1px; background-color: #ddd; margin: 20px 0;'/>
                    <p style='font-size: 12px; color: #999;'>&copy; {DateTime.Now.Year} TechZone. All rights reserved.</p>
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



        [HttpPost("reset-password")]
        public IActionResult SendEmail(string email) // dùng email hay username đều đc
        {
            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user is null)
            {
                return NotFound("Không tìm thấy tài khoản!");
            }

            sendMail(user.Email); // gửi otp tới mail
            return Ok("Đã gửi OTP đến email của bạn!");
        }


        [HttpGet("check-otp/{email}/{otp:int}")]
        public IActionResult CheckOTP(string email, int otp)
        {
            if (otp == null)
                return BadRequest("Vui lòng nhập mã OTP.");

            if (otpStorage.ContainsKey(email))
            {
                var data = otpStorage[email];
                if (data.OtpCode == otp && (DateTime.Now - data.GeneratedAt).TotalMinutes <= 10)
                {
                    otpStorage.Remove(email);
                    return Ok("OTP đã đúng!");
                }
            }
            return BadRequest("Sai OTP hoặc đã hết hạn!");
        }

        [HttpPut("update-password/{email}")]
        public IActionResult UpdateCustomerPassword(string email, [FromBody] ResetPassword rp)  
        {
            if (rp == null || string.IsNullOrEmpty(rp.Password))
            {
                return BadRequest("Mật khẩu không được để trống!");
            }

            var customer = db.Users.FirstOrDefault(c => c.Email == email);
            if (customer is null)
            {
                return NotFound("Email không tồn tại!");
            }

            customer.Password = BCrypt.Net.BCrypt.HashPassword(rp.Password); // hash mk
            try
            {
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

            return Ok("Đổi mật khẩu thành công!");
        }

        [HttpPost("send-otp")]
        public IActionResult SendOtp([FromQuery] string email)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user != null) // email đã có tài khoản rồi
            {
                return BadRequest("Email đã được đăng ký tài khoản.");
            }

            // email chưa có tài khoản, gửi OTP
            sendVerificationEmail(email); // hoặc sendMail(email) tùy bạn
            return Ok("OTP đã được gửi thành công!");
        }
    }
}
