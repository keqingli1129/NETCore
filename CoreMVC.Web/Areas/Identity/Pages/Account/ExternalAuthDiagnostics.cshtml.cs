#nullable disable
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace CoreMVC.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalAuthDiagnosticsModel : PageModel
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ExternalAuthDiagnosticsModel> _logger;

        public ExternalAuthDiagnosticsModel(IWebHostEnvironment env, ILogger<ExternalAuthDiagnosticsModel> logger)
        {
            _env = env;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string Provider { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Error { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ErrorDescription { get; set; }

        // For safety we'll not accept arbitrary large payloads here. This could be extended to read server logs instead.
        public string FullFailure { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!_env.IsDevelopment())
            {
                _logger.LogWarning("Attempt to access ExternalAuthDiagnostics in non-development environment.");
                return RedirectToPage("./Login", new { error = "ExternalAuthFailed" });
            }

            // Populate FullFailure from the query if present (URL may carry truncated data only)
            try
            {
                FullFailure = ErrorDescription ?? "(no details)";
                // Keep reasonable length
                if (FullFailure.Length > 4000) FullFailure = FullFailure.Substring(0, 4000) + "...";
            }
            catch
            {
                FullFailure = "(failed to read details)";
            }

            // Clear the external cookie as a recovery step for diagnostics
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            return Page();
        }
    }
}