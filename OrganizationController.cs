using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseTrackerApp.Data;
using ExpenseTrackerApp.Models;
using ExpenseTrackerApp.ViewModels;

namespace ExpenseTrackerApp.Controllers
{
    [Authorize]
    public class OrganizationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrganizationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Organization/Dashboard - For organization owners
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Dashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.OrganizationId == null || !currentUser.IsOrganizationOwner)
            {
                return Forbid();
            }

            var organization = await _context.Organizations
                .Include(o => o.Users)
                .Include(o => o.Expenses)
                .Include(o => o.Categories)
                .FirstOrDefaultAsync(o => o.Id == currentUser.OrganizationId);

            if (organization == null)
            {
                return NotFound();
            }

            var viewModel = new OrganizationDashboardViewModel
            {
                Organization = organization,
                TotalUsers = organization.Users.Count,
                ActiveUsers = organization.Users.Count(u => u.IsActive),
                TotalExpenses = organization.Expenses.Sum(e => e.Amount),
                TotalCategories = organization.Categories.Count,
                RecentUsers = organization.Users.OrderByDescending(u => u.CreatedDate).Take(5).ToList(),
                RecentExpenses = organization.Expenses.OrderByDescending(e => e.CreatedDate).Take(5).ToList()
            };

            return View(viewModel);
        }

        // GET: Organization/Users - Manage organization users
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Users()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.OrganizationId == null || !currentUser.IsOrganizationOwner)
            {
                return Forbid();
            }

            var users = await _context.Users
                .Include(u => u.Permission)
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .OrderByDescending(u => u.CreatedDate)
                .ToListAsync();

            return View(users);
        }

        // POST: Organization/ToggleUserStatus
        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.OrganizationId == null || !currentUser.IsOrganizationOwner)
            {
                return Forbid();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == currentUser.OrganizationId);

            if (user != null && !user.IsOrganizationOwner) // Can't deactivate organization owner
            {
                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"User {user.Email} has been {(user.IsActive ? "activated" : "deactivated")}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Cannot modify this user.";
            }

            return RedirectToAction(nameof(Users));
        }

        // GET: Organization/Settings
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Settings()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.OrganizationId == null || !currentUser.IsOrganizationOwner)
            {
                return Forbid();
            }

            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == currentUser.OrganizationId);

            if (organization == null)
            {
                return NotFound();
            }

            var viewModel = new OrganizationSettingsViewModel
            {
                Id = organization.Id,
                Name = organization.Name,
                Description = organization.Description,
                IsActive = organization.IsActive
            };

            return View(viewModel);
        }

        // POST: Organization/Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Settings(OrganizationSettingsViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.OrganizationId == null || !currentUser.IsOrganizationOwner)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var organization = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.Id == currentUser.OrganizationId);

                if (organization != null)
                {
                    organization.Name = model.Name;
                    organization.Description = model.Description;
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Organization settings updated successfully!";
                    return RedirectToAction(nameof(Settings));
                }
            }

            return View(model);
        }

        // GET: Organization/CreateUser
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateUser()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.OrganizationId == null || !currentUser.IsOrganizationOwner)
            {
                return Forbid();
            }

            var model = new CreateUserViewModel
            {
                UserRole = "Employee" // Set default role
            };

            return View(model);
        }

        // POST: Organization/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.OrganizationId == null || !currentUser.IsOrganizationOwner)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Check if email already exists
                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Email", "A user with this email already exists.");
                        return View(model);
                    }

                    // Create the user
                    var user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        OrganizationId = currentUser.OrganizationId,
                        IsOrganizationOwner = false,
                        IsActive = true
                    };

                    var result = await _userManager.CreateAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        // Assign role based on user type
                        string roleName = model.UserRole == "Manager" ? "Manager" : "Employee";
                        await _userManager.AddToRoleAsync(user, roleName);

                        // Create permissions based on role
                        var permission = new UserPermission
                        {
                            UserId = user.Id,
                            OrganizationId = currentUser.OrganizationId.Value,
                            CanCreateExpenses = true,
                            CanEditOwnExpenses = true,
                            CanEditAllExpenses = model.UserRole == "Manager",
                            CanDeleteOwnExpenses = true,
                            CanDeleteAllExpenses = model.UserRole == "Manager",
                            CanViewAllExpenses = true,
                            CanManageCategories = model.UserRole == "Manager",
                            CanManageUsers = false, // Only organization owner can manage users
                            CanViewReports = true,
                            CanExportData = model.UserRole == "Manager"
                        };

                        _context.UserPermissions.Add(permission);
                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();

                        TempData["SuccessMessage"] = $"User {model.Email} created successfully!";
                        return RedirectToAction(nameof(Users));
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError(string.Empty, "An error occurred while creating the user. Please try again.");
                }
            }

            return View(model);
        }

        // POST: Organization/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.OrganizationId == null || !currentUser.IsOrganizationOwner)
            {
                return Forbid();
            }

            var userToDelete = await _context.Users
                .Include(u => u.Permission)
                .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == currentUser.OrganizationId);

            if (userToDelete != null && !userToDelete.IsOrganizationOwner)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Delete user permissions
                    if (userToDelete.Permission != null)
                    {
                        _context.UserPermissions.Remove(userToDelete.Permission);
                    }

                    // Soft delete or hard delete based on business logic
                    // For now, we'll mark as inactive
                    userToDelete.IsActive = false;
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"User {userToDelete.Email} has been deactivated.";
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "An error occurred while deleting the user.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Cannot delete this user.";
            }

            return RedirectToAction(nameof(Users));
        }
    }
}
