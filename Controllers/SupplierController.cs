using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSKT_Events.Data;
using SSKT_Events.Models;

namespace SSKT_Events.Controllers;

[Authorize(Roles = "Supplier")]
public class SupplierController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public SupplierController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Supplier's profile
    private async Task<Supplier?> GetCurrentSupplierAsync()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return null;

        return await _context.Suppliers
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    // View partnership reqs
    public async Task<IActionResult> Partnerships()
    {
        var supplier = await GetCurrentSupplierAsync();

        if (supplier == null)
            return NotFound();

        var requests = await _context.OrganizerSuppliers
            .Include(p => p.Organizer)
            .Where(p => p.SupplierId == supplier.Id)
            .ToListAsync();

        ViewBag.Supplier = supplier;

        return View(requests);
    }

    // Accept partnership
    [HttpPost]
    public async Task<IActionResult> AcceptPartnership(int id)
    {
        var supplier = await GetCurrentSupplierAsync();

        if (supplier == null)
            return NotFound();

        var partnership = await _context.OrganizerSuppliers
            .FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.SupplierId == supplier.Id);

        if (partnership == null)
            return NotFound();

        if (partnership.Status != "Pending")
            return RedirectToAction(nameof(Partnerships));

        partnership.Status = "Accepted";

        await _context.SaveChangesAsync();

        TempData["Success"] = "Partnership request accepted.";

        return RedirectToAction(nameof(Partnerships));
    }

    // Reject partnership
    [HttpPost]
    public async Task<IActionResult> RejectPartnership(int id)
    {
        var supplier = await GetCurrentSupplierAsync();

        if (supplier == null)
            return NotFound();

        var partnership = await _context.OrganizerSuppliers
            .FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.SupplierId == supplier.Id);

        if (partnership == null)
            return NotFound();

        if (partnership.Status != "Pending")
            return RedirectToAction(nameof(Partnerships));

        partnership.Status = "Rejected";

        await _context.SaveChangesAsync();

        TempData["Success"] = "Partnership request rejected.";

        return RedirectToAction(nameof(Partnerships));
    }

    // Bookings
    public async Task<IActionResult> Index()
    {
        var supplier = await GetCurrentSupplierAsync();

        if (supplier == null)
            return NotFound();

        var bookings = await _context.SupplierBookings
            .Include(b => b.Event)
            .Include(b => b.Service)
            .Include(b => b.Package)
            .Where(b => b.SupplierId == supplier.Id)
            .OrderBy(b => b.BookingDate)
            .ToListAsync();

        ViewBag.Supplier = supplier;

        ViewBag.TotalBookings = bookings.Count;
        ViewBag.PendingBookings = bookings.Count(b => b.Status == BookingStatus.Pending);
        ViewBag.AcceptedBookings = bookings.Count(b => b.Status == BookingStatus.Accepted);
        ViewBag.RejectedBookings = bookings.Count(b => b.Status == BookingStatus.Rejected);

        return View(bookings);
    }

    // Accept Bookings
    [HttpPost]
    public async Task<IActionResult> Accept(int bookingId)
    {
        var supplier = await GetCurrentSupplierAsync();

        if (supplier == null)
            return NotFound();

        var booking = await _context.SupplierBookings
            .Include(b => b.Event)
            .FirstOrDefaultAsync(b =>
                b.Id == bookingId &&
                b.SupplierId == supplier.Id);

        if (booking == null)
            return NotFound();

        if (booking.Status != BookingStatus.Pending)
            return RedirectToAction(nameof(Index));

        // Can't accept bookings for canceled or non existing events
        if (booking.Event == null ||
            booking.Event.Status == EventStatus.Cancelled)
        {
            TempData["Error"] = "This event has been cancelled. The booking cannot be accepted.";
            return RedirectToAction(nameof(Index));
        }

        
        // Check overlapping dates
        DateTime bookingDate = booking.BookingDate.Date;
        DateTime nextDate = bookingDate.AddDays(1);

        var existingBookings = await _context.SupplierBookings
            .Where(b =>
                b.SupplierId == supplier.Id &&
                b.EventId != booking.EventId &&
                b.Id != booking.Id &&
                b.BookingDate >= bookingDate &&
                b.BookingDate < nextDate &&
                b.Status == BookingStatus.Accepted)
            .Select(b => new
            {
                b.StartTime,
                b.EndTime
            })
            .ToListAsync();

        // Check overlapping times in C#
        bool hasConflict = existingBookings.Any(b =>
            booking.StartTime < b.EndTime &&
            booking.EndTime > b.StartTime);


        if (hasConflict)
        {
            TempData["Error"] =
                "You already have an accepted booking that overlaps with this time slot.";

            return RedirectToAction(nameof(Index));
        }

        booking.Status = BookingStatus.Accepted;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Booking successfully accepted.";

        return RedirectToAction(nameof(Index));
    }

    
    // Reject booking
    [HttpPost]
    public async Task<IActionResult> Decline(int bookingId)
    {
        var supplier = await GetCurrentSupplierAsync();

        if (supplier == null)
            return NotFound();

        var booking = await _context.SupplierBookings
            .FirstOrDefaultAsync(b =>
                b.Id == bookingId &&
                b.SupplierId == supplier.Id);

        if (booking == null)
            return NotFound();

        if (booking.Status == BookingStatus.Pending)
        {
            booking.Status = BookingStatus.Rejected;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking successfully declined.";
        }

        return RedirectToAction(nameof(Index));
    }

    // View services
    public async Task<IActionResult> Services()
    {
        var supplier = await GetCurrentSupplierAsync();

        if (supplier == null)
            return NotFound();

        ViewBag.Supplier = supplier;

        var services = await _context.SupplierServices
            .Where(s =>
                s.SupplierId == supplier.Id &&
                s.IsActive)
            .ToListAsync();

        return View(services);
    }

    // Add service
    [HttpGet]
    public async Task<IActionResult> AddService()
    {
        var supplier = await GetCurrentSupplierAsync();

        if (supplier == null)
            return NotFound();

        ViewBag.SupplierId = supplier.Id;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddService(
        [Bind("Name,Description,Price")] SupplierService service)
    {
        var supplier = await GetCurrentSupplierAsync();

        if (supplier == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.SupplierId = supplier.Id;
            return View(service);
        }

        service.SupplierId = supplier.Id;
        service.IsActive = true;

        _context.SupplierServices.Add(service);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Services));
    }

    // Edit service
    [HttpGet]
    public async Task<IActionResult> EditService(int id)
    {
        var supplier = await GetCurrentSupplierAsync();

        if (supplier == null)
            return NotFound();

        var service = await _context.SupplierServices
            .FirstOrDefaultAsync(s =>
                s.Id == id &&
                s.SupplierId == supplier.Id &&
                s.IsActive);

        if (service == null)
            return NotFound();

        ViewBag.SupplierId = supplier.Id;

        return View(service);
    }

    [HttpPost]
    public async Task<IActionResult> EditService(
        [Bind("Id,Name,Description,Price")] SupplierService updatedService)
    {
        var supplier = await GetCurrentSupplierAsync();

        if (supplier == null)
            return NotFound();

        var service = await _context.SupplierServices
            .FirstOrDefaultAsync(s =>
                s.Id == updatedService.Id &&
                s.SupplierId == supplier.Id &&
                s.IsActive);

        if (service == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.SupplierId = supplier.Id;
            return View(updatedService);
        }

        service.Name = updatedService.Name;
        service.Description = updatedService.Description;
        service.Price = updatedService.Price;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Services));
    }

    // Remove service
    [HttpPost]
    public async Task<IActionResult> DeleteService(int id)
    {
        var supplier = await GetCurrentSupplierAsync();

        if (supplier == null)
            return NotFound();

        var service = await _context.SupplierServices
            .FirstOrDefaultAsync(s =>
                s.Id == id &&
                s.SupplierId == supplier.Id &&
                s.IsActive);

        if (service == null)
            return NotFound();

        service.IsActive = false;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Services));
    }
}
