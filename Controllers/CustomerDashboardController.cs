
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSKT_Events.Data;

namespace SSKT_Events.Controllers
{
    [Authorize(Roles = "Client")]
    public class CustomerDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CustomerDashboardController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Get the currently logged-in client
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            // Find the customer profile connected to this account
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
                return NotFound("Customer profile not found.");

            // Retrieve only this client's events
            var events = await _context.Events
                .Where(e => e.CustomerId == customer.Id)
                .ToListAsync();

            // Dashboard information
            ViewBag.CustomerName = customer.Name;
            ViewBag.TotalEvents = events.Count;

            ViewBag.UpcomingEvents = events.Count(e =>
                e.EventDate.Date >= DateTime.Today &&
                e.Status.ToString() == "Upcoming");

            ViewBag.PendingApproval = events.Count(e =>
                e.Status.ToString() == "Pending");

            ViewBag.CompletedEvents = events.Count(e =>
                e.Status.ToString() == "Completed");

            ViewBag.RecentEvents = events
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .ToList();

            return View();
        }
    }
}
