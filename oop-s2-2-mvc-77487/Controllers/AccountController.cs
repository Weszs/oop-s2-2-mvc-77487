using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using oop_s2_2_mvc_77487.Services;

namespace oop_s2_2_mvc_77487.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuditTrailService _auditTrailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IAuditTrailService auditTrailService,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _auditTrailService = auditTrailService;
            _logger = logger;
        }

        // ---------- LOGIN ----------

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Username and password are required.");
                return View();
            }

            try
            {
                var result = await _signInManager.PasswordSignInAsync(username, password, isPersistent: false, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByNameAsync(username);
                    var roles = user != null ? await _userManager.GetRolesAsync(user) : [];

                    _logger.LogInformation("User {UserName} logged in successfully. Roles: {Roles}",
                        username, string.Join(", ", roles));

                    await _auditTrailService.LogActionAsync(
                        user?.Id ?? "unknown", "Login", "Account", null,
                        $"User {username} logged in. Roles: {string.Join(", ", roles)}");

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Dashboard");
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User {UserName} account locked out", username);
                    await _auditTrailService.LogActionAsync(
                        "unknown", "LoginLockedOut", "Account", null,
                        $"Locked out login attempt for user {username}");
                    return View("Lockout");
                }

                _logger.LogWarning("Invalid login attempt for user {UserName}", username);
                await _auditTrailService.LogActionAsync(
                    "unknown", "LoginFailed", "Account", null,
                    $"Failed login attempt for user {username}");
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {UserName}", username);
                ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
            }

            return View();
        }

        // ---------- REGISTER ----------

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string email, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError(string.Empty, "All fields are required.");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return View();
            }

            try
            {
                var user = new IdentityUser { UserName = username, Email = email };
                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // New registrations default to Viewer role
                    if (await _roleManager.RoleExistsAsync("Viewer"))
                    {
                        await _userManager.AddToRoleAsync(user, "Viewer");
                    }

                    _logger.LogInformation("New user {UserName} registered with Viewer role", username);
                    await _auditTrailService.LogActionAsync(
                        user.Id, "Register", "Account", null,
                        $"New user {username} registered and assigned Viewer role");

                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return RedirectToAction("Index", "Dashboard");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                _logger.LogWarning("Registration failed for user {UserName}: {Errors}",
                    username, string.Join("; ", result.Errors.Select(e => e.Description)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user {UserName}", username);
                ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again.");
            }

            return View();
        }

        // ---------- LOGOUT ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name ?? "Unknown";

            try
            {
                var user = await _userManager.FindByNameAsync(username);
                await _auditTrailService.LogActionAsync(
                    user?.Id ?? "unknown", "Logout", "Account", null,
                    $"User {username} logged out");

                await _signInManager.SignOutAsync();
                _logger.LogInformation("User {UserName} logged out", username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user {UserName}", username);
                await _signInManager.SignOutAsync();
            }

            return RedirectToAction("Index", "Home");
        }

        // ---------- ACCESS DENIED ----------

        [HttpGet]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            _logger.LogWarning("Access denied for user {UserName} attempting to access {ReturnUrl}",
                User.Identity?.Name ?? "Unknown", returnUrl ?? "unknown");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // ---------- LOCKOUT ----------

        [HttpGet]
        public IActionResult Lockout()
        {
            return View();
        }
    }
}
