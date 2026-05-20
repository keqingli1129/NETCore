using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using FBISD;

namespace CoreMVC.Web.Areas.Identity.Pages.Account;

public class SamlInitiateModel : PageModel
{
    private readonly SamlOptions _options;

    public SamlInitiateModel(IOptions<SamlOptions> opts) => _options = opts.Value;

    public IActionResult OnGet(string? returnUrl = null)
    {
        var authReq = new AuthRequest(_options.EntityId, _options.AssertionConsumerServiceUrl);
        var redirectUrl = authReq.GetRedirectUrl(_options.IdpSsoUrl, returnUrl);
        return Redirect(redirectUrl);
    }
}
