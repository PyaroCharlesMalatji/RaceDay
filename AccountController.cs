using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApiService _api;

    public AccountController(ApiService api)
    {
        _api = api;
    }

    // ---------- REGISTER ----------
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var res = await _api.PostAsync("api/auth/register", new
            {
                model.FirstName,
                model.LastName,
                model.Email,
                model.Password,
                model.Role
            });

            if (!res.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Registration failed. That email may already be registered.");
                return View(model);
            }

            TempData["Success"] = "Account created. Please log in.";
            return RedirectToAction("Login");
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "The service is unavailable right now. Please try again later.");
            return View(model);
        }
    }

    // ---------- LOGIN ----------
    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var res = await _api.PostAsync("api/auth/login", new { model.Email, model.Password });

            if (!res.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            // Read the API's reply and pick out the fields we need
            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Some APIs wrap the details in a "user" object
            var details = root;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("user", out var u))
                details = u;

            var token = Pick(root, "token", "accessToken", "jwt");
            var role = Pick(details, "role") ?? Pick(root, "role");
            var userId = Pick(details, "userId", "id") ?? Pick(root, "userId", "id");
            var first = Pick(details, "firstName");
            var last = Pick(details, "lastName");
            var fullName = Pick(details, "fullName", "name") ?? $"{first} {last}".Trim();

            if (string.IsNullOrEmpty(role))
            {
                ModelState.AddModelError("", "Login worked but no role was returned by the service.");
                return View(model);
            }

            // Remember who the user is for every page (session)
            HttpContext.Session.SetString("Role", role);
            HttpContext.Session.SetString("FullName", string.IsNullOrWhiteSpace(fullName) ? model.Email : fullName);
            HttpContext.Session.SetString("UserId", userId ?? "");
            if (!string.IsNullOrEmpty(token))
                HttpContext.Session.SetString("Token", token);

            // Send each role to its own home page
            return role == "Organiser"
                ? RedirectToAction("Index", "OrganiserDashboard")
                : RedirectToAction("Index", "Home");
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "The service is unavailable right now. Please try again later.");
            return View(model);
        }
    }

    // ---------- LOGOUT ----------
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();       // ends the session
        return RedirectToAction("Login");  // back to the login page
    }

    public IActionResult AccessDenied() => View();

    // Finds a value in the JSON by any of the given names (ignores upper/lower case)
    private static string? Pick(JsonElement el, params string[] names)
    {
        if (el.ValueKind != JsonValueKind.Object) return null;
        foreach (var p in el.EnumerateObject())
        {
            if (names.Any(n => string.Equals(n, p.Name, StringComparison.OrdinalIgnoreCase)))
                return p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : p.Value.ToString();
        }
        return null;
    }
}