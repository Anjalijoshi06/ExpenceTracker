using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseTrackerApp.Data;
using ExpenseTrackerApp.Models;
using ExpenseTrackerApp.ViewModels;

namespace ExpenseTrackerApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalOrganizations = await _context.Organizations.CountAsync(),
                TotalUsers = await _context.Users.CountAsync(),
                ActiveUsers = await _context.Users.CountAsync(u => u.IsActive),
                InactiveUsers = await _context.Users.CountAsync(u => !u.IsActive),
                TotalExpenses = await _context.Expenses.SumAsync(e => e.Amount),
                RecentUsers = await _context.Users
                    .Include(u => u.Organization)
                    .OrderByDescending(u => u.CreatedDate)
                    .Take(5)
                    .ToListAsync(),
                RecentOrganizations = await _context.Organizations
                    .Include(o => o.Owner)
                    .OrderByDescending(o => o.CreatedDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Users(int page = 1, string searchTerm = "", string filterStatus = "All", string filterRole = "All")
        {
            const int pageSize = 10;
            var query = _context.Users.Include(u => u.Organization).AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.FirstName.Contains(searchTerm) ||
                                       u.LastName.Contains(searchTerm) ||
                                       (u.Email != null && u.Email.Contains(searchTerm)));
            }

            if (filterStatus != "All")
            {
                var isActive = filterStatus == "Active";
                query = query.Where(u => u.IsActive == isActive);
            }

            var totalUsers = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new UserManagementViewModel
            {
                Users = users,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize),
                SearchTerm = searchTerm,
                FilterStatus = filterStatus,
                FilterRole = filterRole
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Organizations(int page = 1, string searchTerm = "", string filterStatus = "All")
        {
            const int pageSize = 10;
            var query = _context.Organizations.Include(o => o.Owner).AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(o => o.Name.Contains(searchTerm) ||
                                       o.Description!.Contains(searchTerm));
            }

            if (filterStatus != "All")
            {
                var isActive = filterStatus == "Active";
                query = query.Where(o => o.IsActive == isActive);
            }

            var totalOrganizations = await query.CountAsync();
            var organizations = await query
                .OrderByDescending(o => o.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new OrganizationManagementViewModel
            {
                Organizations = organizations,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalOrganizations / (double)pageSize),
                SearchTerm = searchTerm,
                FilterStatus = filterStatus
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _userManager.UpdateAsync(user);

                TempData["SuccessMessage"] = $"User {user.Email} has been {(user.IsActive ? "activated" : "deactivated")}.";
            }
            else
            {
                TempData["ErrorMessage"] = "User not found.";
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleOrganizationStatus(int organizationId)
        {
            var organization = await _context.Organizations.FindAsync(organizationId);
            if (organization != null)
            {
                organization.IsActive = !organization.IsActive;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Organization {organization.Name} has been {(organization.IsActive ? "activated" : "deactivated")}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Organization not found.";
            }

            return RedirectToAction(nameof(Organizations));
        }

        public async Task<IActionResult> UserDetails(string id)
        {
            var user = await _context.Users
                .Include(u => u.Organization)
                .Include(u => u.Permission)
                .Include(u => u.Expenses)
                    .ThenInclude(e => e.Category)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "User";

            var viewModel = new AdminUserDetailsViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = role,
                OrganizationName = user.Organization?.Name,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                RecentExpenses = user.Expenses
                    .OrderByDescending(e => e.ExpenseDate)
                    .Take(10)
                    .Select(e => new RecentExpenseViewModel
                    {
                        Title = e.Title,
                        Amount = e.Amount,
                        ExpenseDate = e.ExpenseDate,
                        CategoryName = e.Category.Name
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> OrganizationDetails(int id)
        {
            var organization = await _context.Organizations
                .Include(o => o.Owner)
                .Include(o => o.Users)
                .Include(o => o.Expenses)
                .Include(o => o.Categories)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organization == null)
            {
                return NotFound();
            }

            return View(organization);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null && !user.IsOrganizationOwner)
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "User deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete user.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Cannot delete organization owner or user not found.";
            }

            return RedirectToAction(nameof(Users));
        }
    }
}
