using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ExpenseTrackerApp.Models;
using ExpenseTrackerApp.ViewModels;
using ExpenseTrackerApp.Data;

namespace ExpenseTrackerApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: /Account/Register - Redirect to Organization Registration
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            // Only organization registration is allowed
            return RedirectToAction("RegisterOrganization");
        }

        // GET: /Account/RegisterOrganization
        [HttpGet]
        public IActionResult RegisterOrganization()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        // POST: /Account/RegisterOrganization
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterOrganization(RegisterOrganizationViewModel model)
        {
            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create the user first
                    var user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        IsOrganizationOwner = true,
                        IsActive = true
                    };

                    var result = await _userManager.CreateAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        // Create the organization
                        var organization = new Organization
                        {
                            Name = model.OrganizationName,
                            Description = model.OrganizationDescription,
                            OwnerId = user.Id,
                            IsActive = true
                        };

                        _context.Organizations.Add(organization);
                        await _context.SaveChangesAsync();

                        // Update user with organization
                        user.OrganizationId = organization.Id;
                        await _userManager.UpdateAsync(user);

                        // Assign Manager role to organization owner
                        await _userManager.AddToRoleAsync(user, "Manager");

                        // Create default permissions for organization owner
                        var permission = new UserPermission
                        {
                            UserId = user.Id,
                            OrganizationId = organization.Id,
                            CanCreateExpenses = true,
                            CanEditOwnExpenses = true,
                            CanEditAllExpenses = true,
                            CanDeleteOwnExpenses = true,
                            CanDeleteAllExpenses = true,
                            CanViewAllExpenses = true,
                            CanManageCategories = true,
                            CanManageUsers = true,
                            CanViewReports = true,
                            CanExportData = true
                        };

                        _context.UserPermissions.Add(permission);
                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();

                        await _signInManager.SignInAsync(user, isPersistent: false);
                        TempData["SuccessMessage"] = "Organization and account created successfully!";
                        return RedirectToAction("Index", "Dashboard");
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError(string.Empty, "An error occurred while creating the organization.");
                }
            }

            return View(model);
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewBag.Roles = UserRole.GetAllRoles();

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Assign role to user
                    var role = string.IsNullOrEmpty(model.Role) ? "Employee" : model.Role;
                    await _userManager.AddToRoleAsync(user, role);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Dashboard");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // GET: /Account/Login
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

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Dashboard");
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: /Account/CreateUser - Removed since users are now created through organizations
        // Individual users can no longer register; only organizations can register and create their users

        // Note: User creation is now handled by Organization Controller
        // Organization owners can create users through Organization/CreateUser
    }
}
