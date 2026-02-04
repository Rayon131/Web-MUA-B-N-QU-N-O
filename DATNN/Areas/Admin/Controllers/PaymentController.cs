using AppView.Models.Service.VNPay;
using DATNN.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATNN.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class PaymentController : Controller
    {
        private readonly MyDbContext _context;
        private readonly IVnPayService _vnPayService;

        public PaymentController(MyDbContext context, IVnPayService vnPayService)
        {
            _context = context;
            _vnPayService = vnPayService;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.DonHangs.ToListAsync());
        }

        public IActionResult CreatePaymentUrlVnpay(PaymentInformationModel model)
        {
            // ===== BẮT ĐẦU SỬA LỖI =====

            // 1. Tạo URL callback một cách linh động.
            //    Action này sẽ trỏ đến nơi bạn xử lý kết quả trả về từ VNPay cho đơn hàng tại quầy.
            var returnUrl = Url.Action("PaymentCallbackVnpay", "Payment", new { Area = "Admin" }, Request.Scheme);

            // 2. Truyền returnUrl làm đối số thứ ba
            var url = _vnPayService.CreatePaymentUrl(model, HttpContext, returnUrl);

            // ===== KẾT THÚC SỬA LỖI =====

            return Redirect(url);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (response == null || !response.Success)
            {
                ViewBag.Message = "Có lỗi xảy ra trong quá trình xác thực giao dịch.";
                ViewBag.Success = false;
                return RedirectToRoleBasedView();
            }

            var txnRef = Request.Query["vnp_TxnRef"].ToString();
            var orderIdStr = txnRef.Split('_')[0];

            if (!int.TryParse(orderIdStr, out int orderId))
            {
                ViewBag.Message = "Không thể xác định mã đơn hàng từ giao dịch.";
                ViewBag.Success = false;
                return RedirectToRoleBasedView();
            }

            var order = await _context.DonHangs.FindAsync(orderId);
            if (order == null)
            {
                ViewBag.Message = $"Không tìm thấy đơn hàng với mã: {orderId}";
                ViewBag.Success = false;
                return RedirectToRoleBasedView();
            }

            if (response.VnPayResponseCode == "00")
            {
                if (order.TrangThaiDonHang != 2)
                {
                    order.TrangThaiDonHang = 2; // Cập nhật trạng thái đơn hàng
                    _context.DonHangs.Update(order);
                    await _context.SaveChangesAsync();
                }

                // Chuyển hướng về trang Bán Hàng để kích hoạt hộp thoại IN HÓA ĐƠN
                return RedirectToAction("Index", "BanHang", new
                {
                    area = "Admin",
                    payment_success = true,
                    orderId = order.MaDonHang
                });
            }
            else
            {
                ViewBag.Message = $"Thanh toán thất bại cho đơn hàng #{orderId}. Mã lỗi VNPay: {response.VnPayResponseCode}";
                ViewBag.Success = false;
                return RedirectToRoleBasedView();
            }
        }

        // Hàm phụ để điều hướng theo vai trò
        private IActionResult RedirectToRoleBasedView()
        {
            if (User.IsInRole("admin"))
            {
                return View("~/Areas/Admin/Views/Payment/PaymentResult.cshtml");
            }
            if (User.IsInRole("nhanvien"))
            {
                return View("~/Areas/nhanvien/Views/Payment/PaymentResult.cshtml");
            }
            else
            {
                return View("LINK");
            }
        }

    }


}
