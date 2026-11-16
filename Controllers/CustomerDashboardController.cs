using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSKT_Events.Data;
using SSKT_Events.Models;

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
            // Client's profile
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            // Find the client's customer profile
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
            {
                return NotFound("Customer profile not found.");
            }

            // Retrieve events tied to specific customer
            var events = await _context.Events
                .Where(e => e.CustomerId == customer.Id)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            // Dashboard information
            ViewBag.CustomerName = customer.Name;
            ViewBag.TotalEvents = events.Count;

            ViewBag.UpcomingEvents = events.Count(e =>
                e.EventDate.Date >= DateTime.Today &&
                e.Status == EventStatus.Upcoming);

            ViewBag.PendingApproval = events.Count(e =>
                e.Status == EventStatus.Pending);

            ViewBag.CompletedEvents = events.Count(e =>
                e.Status == EventStatus.Completed);

            ViewBag.RecentEvents = events
                .Take(5)
                .ToList();

            return View();
        }
    }
}
