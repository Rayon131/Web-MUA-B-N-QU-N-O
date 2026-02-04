using DATNN.Models;
using DATNN.Service;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DATNN.ViewModel;
using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;


namespace DATNN.Controllers
{
    public class LoginController : Controller
    {
        private readonly MyDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;

        public LoginController(MyDbContext context, IEmailService emailService, IMemoryCache cache)
        {
            _context = context;
            _emailService = emailService;
            _cache = cache;
        }

        public IActionResult Index()
        {
            ViewBag.ActiveTab = "login";
            return View();
        }
        public IActionResult Register()
        {
            var chinhSach = _context.ChinhSaches.FirstOrDefault(x => x.id == 1);

            var model = new DangKyViewModel
            {
                NoiDungChinhSach = chinhSach?.NoiDung ?? "Không tìm thấy nội dung chính sách."
            };

            return View(model);
        }

       
        public IActionResult ForgotPassword()
        {
            ViewBag.ActiveTab = "forgot";
            return View();
        }
        private async Task<bool> IsCaptchaValid(string token)
        {
            var secretKey = "6LcTvvErAAAAAPd1o2m-uzX3Qsn57npNXMjNEg_2"; 
            using var client = new HttpClient();
            var response = await client.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}",
                null);
            var json = await response.Content.ReadAsStringAsync();
            return json.Contains("\"success\": true");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string tenDangNhap, string matKhau)
        {
            var captchaToken = Request.Form["g-recaptcha-response"];
            if (string.IsNullOrEmpty(captchaToken) || !await IsCaptchaValid(captchaToken))
            {
                TempData["LoginError"] = "Vui lòng xác nhận bạn không phải là robot.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(tenDangNhap) || string.IsNullOrWhiteSpace(matKhau))
            {
                TempData["LoginError"] = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
                return RedirectToAction("Index");
            }

            var user = await _context.NguoiDungs
                .Include(u => u.Quyen)
                .FirstOrDefaultAsync(u => u.TenDangNhap == tenDangNhap);

            if (user == null)
            {
                TempData["LoginError"] = "Tên đăng nhập hoặc mật khẩu không chính xác.";
                return RedirectToAction("Index");
            }

            // 🔐 Kiểm tra mật khẩu đã băm
            var hasher = new PasswordHasher<NguoiDung>();
            var result = hasher.VerifyHashedPassword(user, user.MatKhau, matKhau);

            if (result == PasswordVerificationResult.Failed)
            {
                TempData["LoginError"] = "Tên đăng nhập hoặc mật khẩu không chính xác.";
                return RedirectToAction("Index");
            }

            if (user.TrangThai == 0)
            {
                TempData["LoginError"] = user.Quyen.MaVaiTro.ToLower() == "nhanvien"
                    ? "Tài khoản nhân viên của bạn đã bị vô hiệu hóa."
                    : "Tài khoản của bạn đã bị khóa.";
                return RedirectToAction("Index");
            }

			// ✅ Đăng nhập bằng Cookie
			var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.TenDangNhap),
        new Claim(ClaimTypes.Role, user.Quyen.MaVaiTro.ToLower()),
        new Claim(ClaimTypes.NameIdentifier, user.MaNguoiDung.ToString())
    };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            bool thongTinKhongDayDu = string.IsNullOrEmpty(user.HoTen)
                            || user.NgaySinh == null
                            || string.IsNullOrEmpty(user.GioiTinh)
                            || string.IsNullOrEmpty(user.SoDienThoai);

            if (thongTinKhongDayDu)
            {
                TempData["ToastMessage"] = "🎉 Đăng nhập thành công! Vui lòng cập nhật đầy đủ thông tin tại hồ sơ.";
            }
            else
            {
                TempData["ToastMessage"] = $"🎉 Đăng nhập thành công! Xin chào {user.HoTen}";
            }
            return user.Quyen.MaVaiTro.ToLower() switch
            {
                "admin" => RedirectToAction("Index", "AdminDashboard", new { area = "Admin" }),
                "nhanvien" => RedirectToAction("Index", "NhanVienDashboard", new { area = "NhanVien" }),
                "khachhang" => RedirectToAction("Index", "Home", new { area = "" }),
                _ => RedirectToAction("Index", "Home")
            };
        }


        [HttpPost]
        public async Task<IActionResult> Register(DangKyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

			var regex = new Regex("^(?=.*[a-z])(?=.*[A-Z])[a-zA-Z0-9]{8,15}$");
			if (string.IsNullOrWhiteSpace(model.MatKhau) || !regex.IsMatch(model.MatKhau))
			{
				TempData["ToastMessage"] = "⚠️ Mật khẩu không hợp lệ. Vui lòng nhập mật khẩu từ 8–15 ký tự, có chữ hoa, chữ thường và không chứa ký tự đặc biệt.";
				return RedirectToAction("Register");
			}

			var captchaToken = Request.Form["g-recaptcha-response"];
            if (string.IsNullOrEmpty(captchaToken) || !await IsCaptchaValid(captchaToken))
            {
                TempData["ToastMessage"] = "❌ Vui lòng xác nhận bạn không phải là robot.";
                return RedirectToAction("Register");
            }

            if (_context.NguoiDungs.Any(u => u.TenDangNhap == model.TenDangNhap))
            {
                TempData["ToastMessage"] = "❌ Tên đăng nhập đã tồn tại.";
                return RedirectToAction("Register");
            }

            if (_context.NguoiDungs.Any(u => u.Email == model.Email))
            {
                TempData["ToastMessage"] = "❌ Email đã được sử dụng.";
                return RedirectToAction("Register");
            }

            // Tạo mã xác nhận
            var code = new Random().Next(100000, 999999).ToString();
            var nguoiDung = new NguoiDung
            {
                TenDangNhap = model.TenDangNhap,
                Email = model.Email,
                MatKhau = model.MatKhau
            };

            // Lưu tạm mã và thông tin người dùng
            _cache.Set("verify_" + model.Email, code, TimeSpan.FromMinutes(10));
            _cache.Set("pending_" + model.Email, nguoiDung, TimeSpan.FromMinutes(10));

            // Tạo nội dung email HTML đẹp
            var body = $@"
            <html>
            <head>
                <meta charset='UTF-8'>
                <style>
                    body {{
                        font-family: 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
                        background-color: #f2f5f9;
                        margin: 0;
                        padding: 0;
                    }}
                    .email-wrapper {{
                        max-width: 600px;
                        margin: 40px auto;
                        background-color: #ffffff;
                        border-radius: 12px;
                        box-shadow: 0 8px 20px rgba(0,0,0,0.08);
                        overflow: hidden;
                        border: 1px solid #e0e6ed;
                    }}
                    .email-header {{
                        background: linear-gradient(90deg, #007bff, #00b4ff);
                        color: #ffffff;
                        padding: 25px;
                        text-align: center;
                    }}
                    .email-header h2 {{
                        margin: 0;
                        font-size: 22px;
                        letter-spacing: 0.5px;
                    }}
                    .email-body {{
                        padding: 35px 30px;
                        color: #333333;
                    }}
                    .email-body p {{
                        line-height: 1.7;
                        margin-bottom: 15px;
                        font-size: 15px;
                    }}
                    .code-box {{
                        background-color: #f1f6ff;
                        border: 1px dashed #007bff;
                        color: #007bff;
                        font-size: 24px;
                        font-weight: bold;
                        text-align: center;
                        letter-spacing: 5px;
                        padding: 15px 0;
                        margin: 25px 0;
                        border-radius: 8px;
                    }}
                    .email-footer {{
                        padding: 18px;
                        font-size: 12px;
                        color: #999999;
                        text-align: center;
                        background-color: #f8fafc;
                    }}
                </style>
            </head>
            <body>
                <div class='email-wrapper'>
                    <div class='email-header'>
                        <h2>📨 Xác nhận đăng ký tài khoản</h2>
                    </div>
                    <div class='email-body'>
                        <p>Xin chào <strong>{model.TenDangNhap}</strong>,</p>
                        <p>Cảm ơn bạn đã đăng ký tài khoản với chúng tôi.</p>
                        <p>Đây là mã xác nhận của bạn (có hiệu lực trong 10 phút):</p>
                        <div class='code-box'>{code}</div>
                        <p>Vui lòng nhập mã này vào trang xác nhận để hoàn tất đăng ký.</p>
                        <p>Nếu bạn không thực hiện hành động này, vui lòng bỏ qua email này.</p>
                    </div>
                    <div class='email-footer'>
                        Đây là email tự động, vui lòng không trả lời.<br>
                        Nếu cần hỗ trợ, hãy liên hệ với Bộ phận Chăm sóc Khách hàng.
                    </div>
                </div>
            </body>
            </html>
            ";

            // Gửi email HTML
            await _emailService.SendEmailAsync(model.Email, "Xác nhận đăng ký tài khoản", body);

            TempData["ToastMessage"] = $"📩 Mã xác nhận đã được gửi đến email: {model.Email}";
            TempData["Email"] = model.Email;
            TempData["ShowXacNhan"] = true;
            return RedirectToAction("XacNhanEmail");
        }


        [HttpGet]
        public IActionResult XacNhanEmail()
        {
            if (TempData["Email"] == null)
                return RedirectToAction("Register");

            ViewBag.Email = TempData["Email"];
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> XacNhanEmail(string email, string maXacNhan)
        {
            if (_cache.TryGetValue("verify_" + email, out string expectedCode) &&
                _cache.TryGetValue("pending_" + email, out NguoiDung model))
            {
                if (maXacNhan == expectedCode)
                {
                    var hasher = new PasswordHasher<NguoiDung>();
                    model.MatKhau = hasher.HashPassword(model, model.MatKhau);

                    model.NgayTao = DateTime.Now;
                    model.TrangThai = 1;
                    model.MaQuyen = 3;

                    _context.NguoiDungs.Add(model);
                    await _context.SaveChangesAsync();

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, model.TenDangNhap),
                        new Claim(ClaimTypes.Email, model.Email),
                        new Claim(ClaimTypes.Role, "khachhang"),
                        new Claim(ClaimTypes.NameIdentifier, model.MaNguoiDung.ToString())
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    _cache.Remove("verify_" + email);
                    _cache.Remove("pending_" + email);

                    // ✅ Thông báo chào mừng
                    TempData["ToastMessage"] = $"🎉 Tạo tài khoản thành công! Xin chào {model.TenDangNhap}";

                    return RedirectToAction("Index", "Home");
                }

            }

            TempData["ToastMessage"] = "Mã xác nhận không đúng hoặc đã hết hạn.";
            TempData["Email"] = email;
            TempData["ShowXacNhan"] = true;
            return RedirectToAction("Index", "Login");
        }


        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var captchaToken = Request.Form["g-recaptcha-response"];
            if (string.IsNullOrEmpty(captchaToken) || !await IsCaptchaValid(captchaToken))
            {
                TempData["ToastMessage"] = "Vui lòng xác nhận bạn không phải là robot.";
                return RedirectToAction("Index");
            }
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                TempData["ToastMessage"] = "Email không tồn tại trong hệ thống.";
                return RedirectToAction("Index", "Login");
            }

            var token = Guid.NewGuid().ToString();
            user.ResetToken = token;
            user.TokenExpiry = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            var resetLink = Url.Action("ResetPassword", "Login", new { token = token }, Request.Scheme);

            var subject = "Khôi phục mật khẩu";
            var body = $@"
            <html>
            <head>
                <meta charset='UTF-8'>
                <style>
                    body {{
                        font-family: 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
                        background-color: #f2f5f9;
                        margin: 0;
                        padding: 0;
                    }}
                    .email-wrapper {{
                        max-width: 600px;
                        margin: 40px auto;
                        background-color: #ffffff;
                        border-radius: 12px;
                        box-shadow: 0 8px 20px rgba(0,0,0,0.08);
                        overflow: hidden;
                        border: 1px solid #e0e6ed;
                    }}
                    .email-header {{
                        background: linear-gradient(90deg, #007bff, #00b4ff);
                        color: #ffffff;
                        padding: 25px;
                        text-align: center;
                    }}
                    .email-header h2 {{
                        margin: 0;
                        font-size: 22px;
                        letter-spacing: 0.5px;
                    }}
                    .email-body {{
                        padding: 35px 30px;
                        color: #333333;
                    }}
                    .email-body p {{
                        line-height: 1.7;
                        margin-bottom: 15px;
                        font-size: 15px;
                    }}
                    .highlight {{
                        color: #007bff;
                        font-weight: 600;
                    }}
                    .btn {{
                        display: inline-block;
                        margin-top: 25px;
                        padding: 12px 30px;
                        background-color: #007bff;
                        color: #ffffff !important;
                        text-decoration: none;
                        border-radius: 6px;
                        font-weight: 600;
                        letter-spacing: 0.3px;
                        transition: background-color 0.3s ease;
                    }}
                    .btn:hover {{
                        background-color: #0056b3;
                    }}
                    .divider {{
                        margin: 30px 0;
                        border-top: 1px solid #e0e6ed;
                    }}
                    .email-footer {{
                        padding: 18px;
                        font-size: 12px;
                        color: #999999;
                        text-align: center;
                        background-color: #f8fafc;
                    }}
                </style>
            </head>
            <body>
                <div class='email-wrapper'>
                    <div class='email-header'>
                        <h2>🔐 Khôi phục mật khẩu</h2>
                    </div>
                    <div class='email-body'>
                        <p>Xin chào <strong class='highlight'>{user.TenDangNhap}</strong>,</p>
                        <p>Bạn đã gửi yêu cầu khôi phục mật khẩu cho tài khoản của mình.</p>
                        <p>Vui lòng nhấn vào nút bên dưới để đặt lại mật khẩu:</p>
                        <p style='text-align:center;'>
                            <a href='{resetLink}' class='btn'>Đặt lại mật khẩu</a>
                        </p>
                        <div class='divider'></div>
                        <p>⏰ Liên kết này chỉ có hiệu lực trong vòng <strong>1 giờ</strong>.</p>
                        <p>Nếu bạn không yêu cầu khôi phục, vui lòng bỏ qua email này.</p>
                    </div>
                    <div class='email-footer'>
                        Đây là email tự động, vui lòng không trả lời.<br>
                        Cần hỗ trợ? Hãy liên hệ với <strong>Bộ phận Chăm sóc Khách hàng</strong>.
                    </div>
                </div>
            </body>
            </html>
            ";


            await _emailService.SendEmailAsync(email, subject, body);

            TempData["ToastMessage"] = "Liên kết đặt lại mật khẩu đã được gửi về email.";
            return RedirectToAction("Index", "Login");
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.ResetToken == token && u.TokenExpiry > DateTime.UtcNow);
            if (user == null)
            {
                TempData["ToastMessage"] = "Liên kết không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction("Index", "Login");
            }

            ViewBag.Token = token;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string token, string newPassword)
        {
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.ResetToken == token && u.TokenExpiry > DateTime.UtcNow);
            if (user == null)
            {
                TempData["ToastMessage"] = "❌ Liên kết không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction("Index", "Login");
            }

            // ✅ Kiểm tra định dạng mật khẩu theo model
            var regex = new Regex("^(?=.*[a-z])(?=.*[A-Z])[a-zA-Z0-9]{8,15}$");
            if (string.IsNullOrWhiteSpace(newPassword) || !regex.IsMatch(newPassword))
            {
                TempData["ToastMessage"] = "⚠️ Mật khẩu không hợp lệ. Vui lòng nhập mật khẩu từ 8–15 ký tự, có chữ hoa, chữ thường và không chứa ký tự đặc biệt.";
                return RedirectToAction("ResetPasswordForm", new { token }); // hoặc trang nhập lại
            }

            // ✅ Mã hóa mật khẩu
            var hasher = new PasswordHasher<NguoiDung>();
            user.MatKhau = hasher.HashPassword(user, newPassword);

            // ✅ Xóa token
            user.ResetToken = null;
            user.TokenExpiry = null;
            await _context.SaveChangesAsync();

            // ✅ Tự động đăng nhập
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.TenDangNhap),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.MaNguoiDung.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // ✅ Thông báo thành công
            TempData["ToastMessage"] = $"✅ Mật khẩu đã được cập nhật. Xin chào {user.TenDangNhap}!";
            return RedirectToAction("Index", "Home");
        }




        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Login");
        }


    }
}
