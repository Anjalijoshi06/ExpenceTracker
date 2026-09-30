using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpenseTrackerApp.Data;
using ExpenseTrackerApp.Models;
using ExpenseTrackerApp.ViewModels;

namespace ExpenseTrackerApp.Controllers
{
    [Authorize]
    public class ExpensesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExpensesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Expenses
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var userRole = userRoles.FirstOrDefault() ?? "Employee";

            IQueryable<Expense> expensesQuery = _context.Expenses
                .Include(e => e.Category)
                .Include(e => e.User);

            // Super Admin can see all expenses across all organizations
            if (User.IsInRole("Admin") && user.OrganizationId == null)
            {
                // Super admin sees all expenses
            }
            // Organization users can only see expenses from their organization
            else if (user.OrganizationId.HasValue)
            {
                expensesQuery = expensesQuery.Where(e => e.OrganizationId == user.OrganizationId);

                // Employees can only see their own expenses
                if (userRole == "Employee")
                {
                    expensesQuery = expensesQuery.Where(e => e.UserId == user.Id);
                }
            }
            else
            {
                // User has no organization and is not super admin - show no expenses
                expensesQuery = expensesQuery.Where(e => false);
            }

            var expenses = await expensesQuery
                .OrderByDescending(e => e.ExpenseDate)
                .Select(e => new ExpenseViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    Amount = e.Amount,
                    ExpenseDate = e.ExpenseDate,
                    CategoryId = e.CategoryId,
                    CategoryName = e.Category.Name,
                    UserName = $"{e.User.FirstName} {e.User.LastName}",
                    CreatedDate = e.CreatedDate,
                    UserId = e.UserId
                })
                .ToListAsync();

            ViewBag.CanDelete = UserRole.CanDelete(userRole);
            ViewBag.CanEdit = UserRole.CanEdit(userRole);

            return View(expenses);
        }

        // GET: Expenses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user!);
            var userRole = userRoles.FirstOrDefault() ?? "Employee";

            var expense = await _context.Expenses
                .Include(e => e.Category)
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (expense == null)
            {
                return NotFound();
            }

            // Check if user can view this expense
            if (userRole == "Employee" && expense.UserId != user!.Id)
            {
                return Forbid();
            }

            var viewModel = new ExpenseViewModel
            {
                Id = expense.Id,
                Title = expense.Title,
                Description = expense.Description,
                Amount = expense.Amount,
                ExpenseDate = expense.ExpenseDate,
                CategoryId = expense.CategoryId,
                CategoryName = expense.Category.Name,
                UserName = $"{expense.User.FirstName} {expense.User.LastName}",
                CreatedDate = expense.CreatedDate,
                UserId = expense.UserId
            };

            return View(viewModel);
        }

        // GET: Expenses/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new ExpenseViewModel();
            await PopulateCategories(viewModel);
            return View(viewModel);
        }

        // POST: Expenses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExpenseViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var expense = new Expense
                {
                    Title = viewModel.Title,
                    Description = viewModel.Description,
                    Amount = viewModel.Amount,
                    ExpenseDate = viewModel.ExpenseDate,
                    CategoryId = viewModel.CategoryId,
                    UserId = user.Id,
                    OrganizationId = user.OrganizationId, // Assign to user's organization
                    CreatedDate = DateTime.UtcNow
                };

                _context.Add(expense);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Expense created successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateCategories(viewModel);
            return View(viewModel);
        }

        // GET: Expenses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user!);
            var userRole = userRoles.FirstOrDefault() ?? "Employee";

            if (!UserRole.CanEdit(userRole))
            {
                return Forbid();
            }

            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null)
            {
                return NotFound();
            }

            // Check if user can edit this expense
            if (userRole == "Employee" && expense.UserId != user!.Id)
            {
                return Forbid();
            }

            var viewModel = new ExpenseViewModel
            {
                Id = expense.Id,
                Title = expense.Title,
                Description = expense.Description,
                Amount = expense.Amount,
                ExpenseDate = expense.ExpenseDate,
                CategoryId = expense.CategoryId,
                UserId = expense.UserId
            };

            await PopulateCategories(viewModel);
            return View(viewModel);
        }

        // POST: Expenses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ExpenseViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user!);
            var userRole = userRoles.FirstOrDefault() ?? "Employee";

            if (!UserRole.CanEdit(userRole))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var expense = await _context.Expenses.FindAsync(id);
                    if (expense == null)
                    {
                        return NotFound();
                    }

                    // Check if user can edit this expense
                    if (userRole == "Employee" && expense.UserId != user!.Id)
                    {
                        return Forbid();
                    }

                    expense.Title = viewModel.Title;
                    expense.Description = viewModel.Description;
                    expense.Amount = viewModel.Amount;
                    expense.ExpenseDate = viewModel.ExpenseDate;
                    expense.CategoryId = viewModel.CategoryId;
                    expense.ModifiedDate = DateTime.UtcNow;

                    _context.Update(expense);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Expense updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExpenseExists(viewModel.Id))
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

            await PopulateCategories(viewModel);
            return View(viewModel);
        }

        // GET: Expenses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user!);
            var userRole = userRoles.FirstOrDefault() ?? "Employee";

            if (!UserRole.CanDelete(userRole))
            {
                return Forbid();
            }

            var expense = await _context.Expenses
                .Include(e => e.Category)
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (expense == null)
            {
                return NotFound();
            }

            var viewModel = new ExpenseViewModel
            {
                Id = expense.Id,
                Title = expense.Title,
                Description = expense.Description,
                Amount = expense.Amount,
                ExpenseDate = expense.ExpenseDate,
                CategoryId = expense.CategoryId,
                CategoryName = expense.Category.Name,
                UserName = $"{expense.User.FirstName} {expense.User.LastName}",
                CreatedDate = expense.CreatedDate,
                UserId = expense.UserId
            };

            return View(viewModel);
        }

        // POST: Expenses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user!);
            var userRole = userRoles.FirstOrDefault() ?? "Employee";

            if (!UserRole.CanDelete(userRole))
            {
                return Forbid();
            }

            var expense = await _context.Expenses.FindAsync(id);
            if (expense != null)
            {
                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Expense deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ExpenseExists(int id)
        {
            return _context.Expenses.Any(e => e.Id == id);
        }

        private async Task PopulateCategories(ExpenseViewModel viewModel)
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            viewModel.Categories = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = c.Id == viewModel.CategoryId
            }).ToList();
        }
    }
}
