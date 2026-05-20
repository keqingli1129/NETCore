using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using FBISD;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.IO;
using System.Text;

namespace CoreMVC.Web.Areas.Identity.Pages.Account;

[IgnoreAntiforgeryToken]
public class AcsModel : PageModel
{
    private readonly SamlOptions _options;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public AcsModel(IOptions<SamlOptions> opts, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
    {
        _options = opts.Value;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var samlResponse = Request.Form["SAMLResponse"].ToString();
        if (string.IsNullOrEmpty(samlResponse))
            return BadRequest();

        // Load certificate content either from configured path or fallback to in-memory string
        string certContent = _options.IdpCertificate ?? string.Empty;

        if (!string.IsNullOrEmpty(_options.IdpCertificatePath))
        {
            var path = Path.IsPathRooted(_options.IdpCertificatePath)
                ? _options.IdpCertificatePath
                : Path.Combine(Directory.GetCurrentDirectory(), _options.IdpCertificatePath);

            if (!System.IO.File.Exists(path))
            {
                return Forbid();
            }

            byte[] certBytes;
            try
            {
                certBytes = await System.IO.File.ReadAllBytesAsync(path);
            }
            catch
            {
                return Forbid();
            }

            var text = Encoding.UTF8.GetString(certBytes);
            if (text.Contains("-----BEGIN"))
            {
                // PEM text file
                certContent = text;
            }
            else
            {
                // Binary DER/CRT - convert to base64 so the Response constructor can handle it as a string
                certContent = Convert.ToBase64String(certBytes);
            }
        }

        var resp = new Response(certContent, samlResponse);
        if (!resp.IsValid())
            return Forbid();

        var nameId = resp.GetNameID() ?? resp.GetEmail() ?? Guid.NewGuid().ToString();
        var email = resp.GetEmail();

        IdentityUser? user = null;
        if (!string.IsNullOrEmpty(email))
        {
            user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new IdentityUser { UserName = email!, Email = email, EmailConfirmed = true };
                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return Forbid();
                }
            }
        }

        if (user == null)
        {
            // fallback: create a local user with nameId as username
            user = new IdentityUser { UserName = nameId, Email = nameId };
            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                return Forbid();
        }

        // Link external login
        var userLogins = await _userManager.GetLoginsAsync(user);
        var alreadyLinked = userLogins.Any(l => l.LoginProvider == "SAML" && l.ProviderKey == nameId);
        if (!alreadyLinked)
        {
            var info = new UserLoginInfo("SAML", nameId, "SAML");
            await _userManager.AddLoginAsync(user, info);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);

        var relay = Request.Form["RelayState"].ToString();
        if (!string.IsNullOrEmpty(relay) && Url.IsLocalUrl(relay))
            return LocalRedirect(relay);

        return RedirectToPage("/Index");
    }
}
