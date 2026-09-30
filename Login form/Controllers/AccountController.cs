using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Login_form.Data;
using Login_form.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Login_form.Controllers;

[AllowAnonymous]
public class AccountController(
    ApplicationDbContext dbContext,
    IPasswordHasher<UserAccount> passwordHasher,
    ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToUpperInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(
            account => account.NormalizedEmail == normalizedEmail);

        if (user is not null && !string.IsNullOrEmpty(user.PasswordHash))
        {
            var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
            if (verification != PasswordVerificationResult.Failed)
            {
                if (verification == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    user.PasswordHash = passwordHasher.HashPassword(user, model.Password);
                    await dbContext.SaveChangesAsync();
                }

                await SignInUserAsync(user, model.RememberMe);

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return LocalRedirect(model.ReturnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var normalizedEmail = email.ToUpperInvariant();
        var emailExists = await dbContext.Users.AnyAsync(
            account => account.NormalizedEmail == normalizedEmail);

        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View(model);
        }

        var user = new UserAccount
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            NormalizedEmail = normalizedEmail
        };
        user.PasswordHash = passwordHasher.HashPassword(user, model.Password);

        dbContext.Users.Add(user);
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Could not create an account for {Email}.", email);
            ModelState.AddModelError(string.Empty, "The account could not be saved. Please try again.");
            return View(model);
        }

        await SignInUserAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    private async Task SignInUserAsync(UserAccount user, bool isPersistent)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Email ?? string.Empty)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = isPersistent });
    }
}
