using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ExpenseTrackerApp.Data;
using ExpenseTrackerApp.Models;
using ExpenseTrackerApp.ViewModels;

namespace ExpenseTrackerApp.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CategoriesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user!);
            var userRole = userRoles.FirstOrDefault() ?? "Employee";

            if (!UserRole.CanManageCategories(userRole))
            {
                return Forbid();
            }

            IQueryable<Category> categoriesQuery = _context.Categories.Include(c => c.Expenses);

            // Super Admin can see all categories
            if (User.IsInRole("Admin") && user!.OrganizationId == null)
            {
                // Super admin sees all categories
            }
            // Organization users see global categories + their org categories
            else if (user!.OrganizationId.HasValue)
            {
                categoriesQuery = categoriesQuery.Where(c => c.OrganizationId == null || c.OrganizationId == user.OrganizationId);
            }
            else
            {
                // User has no organization - only show global categories
                categoriesQuery = categoriesQuery.Where(c => c.OrganizationId == null);
            }

            var categories = await categoriesQuery
                .OrderBy(c => c.Name)
                .Select(c => new CategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Color = c.Color,
                    IsActive = c.IsActive,
                    CreatedDate = c.CreatedDate,
                    ExpenseCount = c.Expenses.Count()
                })
                .ToListAsync();

            return View(categories);
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user!);
            var userRole = userRoles.FirstOrDefault() ?? "Employee";

            if (!UserRole.CanManageCategories(userRole))
            {
                return Forbid();
            }

            var category = await _context.Categories
                .Include(c => c.Expenses)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            var viewModel = new CategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Color = category.Color,
                IsActive = category.IsActive,
                CreatedDate = category.CreatedDate,
                ExpenseCount = category.Expenses.Count()
            };

            return View(viewModel);
        }

        // GET: Categories/Create
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user!);
            var userRole = userRoles.FirstOrDefault() ?? "Employee";

            if (!UserRole.CanManageCategories(userRole))
            {
                return Forbid();
            }

            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel viewModel)
        {
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user!);
            var userRole = userRoles.FirstOrDefault() ?? "Employee";

            if (!UserRole.CanManageCategories(userRole))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var category = new Category
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    Color = viewModel.Color,
                    IsActive = viewModel.IsActive,
                    OrganizationId = user!.OrganizationId, // Assign to user's organization (null for super admin = global category)
                    CreatedDate = DateTime.UtcNow
                };

                _context.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Category created successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(viewModel);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user!);
            var userRole = userRoles.FirstOrDefault() ?? "Employee";

            if (!UserRole.CanManageCategories(userRole))
            {
                return Forbid();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var viewModel = new CategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Color = category.Color,
                IsActive = category.IsActive
            };

            return View(viewModel);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user!);
            var userRole = userRoles.FirstOrDefault() ?? "Employee";

            if (!UserRole.CanManageCategories(userRole))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var category = await _context.Categories.FindAsync(id);
                    if (category == null)
                    {
                        return NotFound();
                    }

                    category.Name = viewModel.Name;
                    category.Description = viewModel.Description;
                    category.Color = viewModel.Color;
                    category.IsActive = viewModel.IsActive;

                    _context.Update(category);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Category updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            return View(viewModel);
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user!);
            var userRole = userRoles.FirstOrDefault() ?? "Employee";

            if (!UserRole.CanManageCategories(userRole))
            {
                return Forbid();
            }

            var category = await _context.Categories
                .Include(c => c.Expenses)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            var viewModel = new CategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Color = category.Color,
                IsActive = category.IsActive,
                CreatedDate = category.CreatedDate,
                ExpenseCount = category.Expenses.Count()
            };

            return View(viewModel);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user!);
            var userRole = userRoles.FirstOrDefault() ?? "Employee";

            if (!UserRole.CanManageCategories(userRole))
            {
                return Forbid();
            }

            var category = await _context.Categories.Include(c => c.Expenses).FirstOrDefaultAsync(c => c.Id == id);
            if (category != null)
            {
                if (category.Expenses.Any())
                {
                    TempData["ErrorMessage"] = "Cannot delete category that has expenses. Please delete all expenses in this category first.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Category deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}
