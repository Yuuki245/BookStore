// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace BookStore.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                var emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9fafb;'>
                    <div style='background-color: white; border-radius: 8px; padding: 30px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                        <h2 style='color: #3b61eb; margin-top: 0;'>
                            <i class='bi bi-key'></i> Đặt lại mật khẩu
                        </h2>
                        <p style='color: #374151; font-size: 16px; line-height: 1.6;'>Xin chào,</p>
                        <p style='color: #374151; font-size: 16px; line-height: 1.6;'>
                            Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản BookStore của bạn.
                        </p>
                        <p style='color: #374151; font-size: 16px; line-height: 1.6;'>
                            Vui lòng click vào nút bên dưới để đặt lại mật khẩu:
                        </p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' 
                               style='background-color: #3b61eb; color: white; padding: 14px 28px; 
                                      text-decoration: none; border-radius: 6px; display: inline-block; 
                                      font-weight: 600; font-size: 16px;'>
                                Đặt lại mật khẩu
                            </a>
                        </div>
                        <p style='color: #6b7280; font-size: 14px; line-height: 1.6;'>
                            Hoặc copy và paste link sau vào trình duyệt của bạn:
                        </p>
                        <p style='color: #3b61eb; font-size: 12px; word-break: break-all; background-color: #f3f4f6; padding: 10px; border-radius: 4px;'>
                            {HtmlEncoder.Default.Encode(callbackUrl)}
                        </p>
                        <p style='color: #6b7280; font-size: 14px; line-height: 1.6; margin-top: 30px;'>
                            Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này. 
                            Mật khẩu của bạn sẽ không thay đổi.
                        </p>
                        <p style='color: #9ca3af; font-size: 12px; margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e7eb;'>
                            <strong>Lưu ý:</strong> Link này sẽ hết hạn sau 24 giờ.
                        </p>
                    </div>
                    <div style='text-align: center; margin-top: 20px; color: #9ca3af; font-size: 12px;'>
                        <p>BookStore - Cửa hàng sách trực tuyến</p>
                        <p>© {DateTime.Now.Year} BookStore. All rights reserved.</p>
                    </div>
                </div>";

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Đặt lại mật khẩu - BookStore",
                    emailBody);

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
