using System.Security.Claims;
using BlueHarbor.Security;
using BlueHarbor.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueHarbor.Controllers;

/// <summary>
/// Pagine trasversali: dashboard, login/logout e l'azione condivisa "Next Day".
/// </summary>
public class HomeController : Controller
{
    private readonly TerminalClockService _clockService;
    private readonly InMemoryUserStore _userStore;

    public HomeController(TerminalClockService clockService, InMemoryUserStore userStore)
    {
        _clockService = clockService;
        _userStore = userStore;
    }

    [HttpGet("/")]
    [Authorize]
    public IActionResult Dashboard()
    {
        return View();
    }

    [HttpGet("/login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost("/login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password)
    {
        DemoUser? user = _userStore.Validate(username, password);
        if (user is null)
        {
            // Credenziali non valide: torna al login con il flag d'errore.
            return Redirect("/login?error");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        // defaultSuccessUrl("/"): dopo il login si va sempre alla dashboard.
        return Redirect("/");
    }

    [HttpPost("/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/login?logout");
    }

    [HttpPost("/next-day")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public IActionResult NextDay()
    {
        int day = _clockService.NextDay();
        TempData["message"] = $"Avanzato al giorno {day}.";
        return Redirect(SafeReferer());
    }

    // Torna alla pagina di provenienza (dashboard, elenco navi o board) dopo il Next Day.
    private string SafeReferer()
    {
        string? referer = Request.Headers["Referer"];
        if (referer is not null && referer.Contains("://"))
        {
            string path = referer[(referer.IndexOf("://", StringComparison.Ordinal) + 3)..];
            int slash = path.IndexOf('/');
            return slash >= 0 ? path[slash..] : "/";
        }
        return "/";
    }
}
