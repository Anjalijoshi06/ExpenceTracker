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
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var userRole = userRoles.FirstOrDefault() ?? "Employee";

            var viewModel = new DashboardViewModel
            {
                UserRole = userRole,
                CanDelete = UserRole.CanDelete(userRole),
                CanEdit = UserRole.CanEdit(userRole),
                CanManageUsers = UserRole.CanManageUsers(userRole),
                CanManageCategories = UserRole.CanManageCategories(userRole)
            };

            // Get expenses based on user role and organization
            IQueryable<Expense> expensesQuery = _context.Expenses.Include(e => e.Category);

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

            var expenses = await expensesQuery.ToListAsync();

            // Calculate totals
            viewModel.TotalExpenses = expenses.Sum(e => e.Amount);
            viewModel.TotalTransactions = expenses.Count;

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            viewModel.MonthlyExpenses = expenses
                .Where(e => e.ExpenseDate.Month == currentMonth && e.ExpenseDate.Year == currentYear)
                .Sum(e => e.Amount);

            var currentWeek = DateTime.Now.AddDays(-7);
            viewModel.WeeklyExpenses = expenses
                .Where(e => e.ExpenseDate >= currentWeek)
                .Sum(e => e.Amount);

            // Category summaries
            viewModel.CategorySummaries = expenses
                .GroupBy(e => new { e.Category.Name, e.Category.Color })
                .Select(g => new CategorySummary
                {
                    CategoryName = g.Key.Name,
                    Amount = g.Sum(e => e.Amount),
                    Color = g.Key.Color ?? "#007bff",
                    Count = g.Count()
                })
                .OrderByDescending(c => c.Amount)
                .Take(5)
                .ToList();

            // Recent expenses
            viewModel.RecentExpenses = expenses
                .OrderByDescending(e => e.CreatedDate)
                .Take(5)
                .ToList();

            // Monthly chart data (last 6 months)
            var monthlyData = expenses
                .Where(e => e.ExpenseDate >= DateTime.Now.AddMonths(-6))
                .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
                .Select(g => new MonthlyExpense
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Amount = g.Sum(e => e.Amount)
                })
                .OrderBy(m => m.Month)
                .ToList();

            viewModel.MonthlyExpenseChart = monthlyData;

            return View(viewModel);
        }
    }
}
