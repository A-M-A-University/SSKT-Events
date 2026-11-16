using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSKT_Events.Data;
using SSKT_Events.Models;

namespace SSKT_Events.Controllers;

[Authorize(Roles = "Client")]
public class CatalogController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public CatalogController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Customer's profile
    private async Task<Customer?> GetCurrentCustomerAsync()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return null;

        return await _context.Customers
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    // Get event that are tied to that specific customer acc
    private async Task<EventRecord?> GetCustomerEventAsync(int eventId)
    {
        var customer = await GetCurrentCustomerAsync();

        if (customer == null)
            return null;

        return await _context.Events
            .Include(e => e.Bookings)
            .FirstOrDefaultAsync(e =>
                e.Id == eventId &&
                e.CustomerId == customer.Id);
    }

    // Catalog
    public async Task<IActionResult> Index(int eventId)
    {
        var ev = await GetCustomerEventAsync(eventId);

        if (ev == null)
            return NotFound();

        var packages = await _context.EventPackages
            .Include(p => p.PackageServices)
                .ThenInclude(ps => ps.Service)
            .Where(p => p.IsPublished)
            .ToListAsync();

        foreach (var package in packages)
        {
            package.ServiceIds = package.PackageServices
                .Select(ps => ps.ServiceId)
                .ToList();
        }

        ViewBag.Event = ev;
        ViewBag.Packages = packages;

        ViewBag.Services = await _context.SupplierServices
            .Where(s => s.IsActive)
            .ToListAsync();

        ViewBag.Suppliers = await _context.Suppliers
            .ToListAsync();

        return View();
    }

    // Individual services
    [HttpPost]
    public async Task<IActionResult> BookService(int eventId, int serviceId)
    {
        var ev = await GetCustomerEventAsync(eventId);

        var service = await _context.SupplierServices
            .Include(s => s.Supplier)
            .FirstOrDefaultAsync(s => s.Id == serviceId);

        if (ev == null || service == null)
            return NotFound();

        var error = await CheckBookableAsync(ev, service);

        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index), new { eventId });
        }

        var booking = CreateBooking(ev, service, service.Price, null);

        _context.SupplierBookings.Add(booking);

        await _context.SaveChangesAsync();

        TempData.Remove("Error");
        TempData["Success"] = service.Name +
            " requested. Waiting for the supplier to accept.";

        return RedirectToAction("Details", "Events", new { id = eventId });
    }

    // Book package
    [HttpPost]
    public async Task<IActionResult> BookPackage(int eventId, int packageId)
    {
        var ev = await GetCustomerEventAsync(eventId);

        var package = await _context.EventPackages
            .Include(p => p.PackageServices)
                .ThenInclude(ps => ps.Service)
                    .ThenInclude(s => s.Supplier)
            .FirstOrDefaultAsync(p =>
                p.Id == packageId &&
                p.IsPublished);

        if (ev == null || package == null)
            return NotFound();

        // Services included in package
        var serviceIds = package.PackageServices
            .Select(ps => ps.ServiceId)
            .Distinct()
            .ToList();

        var included = package.PackageServices
            .Select(ps => ps.Service)
            .Where(s => s != null && s.IsActive)
            .DistinctBy(s => s!.Id)
            .ToList();

        // Checks if a service inside package is unavailable
        if (included.Count == 0 || included.Count != serviceIds.Count)
        {
            TempData["Error"] =
                "One or more services in this package are unavailable.";

            return RedirectToAction(nameof(Index), new { eventId });
        }

    // Get organizer's accepted partner suppliers
    var partnerIds = await _context.OrganizerSuppliers
        .Where(p =>
            p.OrganizerId == package.OrganizerId &&
            p.Status == "Accepted")
        .Select(p => p.SupplierId)
        .ToListAsync();

    // Make sure all package suppliers are accepted partners
    if (included.Any(s => !partnerIds.Contains(s!.SupplierId)))
    {
        TempData["Error"] =
            "One or more suppliers in this package are no longer accepted partners.";

        return RedirectToAction(nameof(Index), new { eventId });
    }

        // Check every service before creating any bookings
        foreach (var service in included)
        {
            var error = await CheckBookableAsync(ev, service!);

            if (error != null)
            {
                TempData["Error"] = "Can't book " +
                    package.Name + ": " + error;

                return RedirectToAction(nameof(Index), new { eventId });
            }
        }

        // Calculate each service's share of the package price
        var totalServicePrice = included.Sum(s => s!.Price);
        decimal remainingPrice = package.Price;

        for (int i = 0; i < included.Count; i++)
        {
            var service = included[i]!;

            decimal share;

            if (i == included.Count - 1)
            {
                share = remainingPrice;
            }
            else
            {
                share = totalServicePrice == 0
                    ? 0
                    : Math.Round(
                        package.Price * service.Price / totalServicePrice,
                        2);

                remainingPrice -= share;
            }

            var booking = CreateBooking(
                ev,
                service,
                share,
                package.Id);

            _context.SupplierBookings.Add(booking);
        }

        // Save all package bookings together
        await _context.SaveChangesAsync();

        TempData.Remove("Error");
        TempData["Success"] = package.Name +
            " requested. Waiting for the suppliers to accept.";

        return RedirectToAction("Details", "Events", new { id = eventId });
    }
    
    // Service check booking
    private async Task<string?> CheckBookableAsync(
        EventRecord ev,
        SupplierService service)
    {
        if (ev.Status == EventStatus.Cancelled)
            return "This event is cancelled.";

        if (ev.EventDate.Date < DateTime.Today)
            return "You cannot book services for past events.";

        if (!service.IsActive)
            return service.Name + " is currently unavailable.";

        if (service.Supplier == null)
            return "The supplier for " + service.Name +
                " could not be found.";

        var alreadyBooked = ev.Bookings.Any(b =>
            b.ServiceId == service.Id &&
            (b.Status == BookingStatus.Pending ||
            b.Status == BookingStatus.Accepted));

        if (alreadyBooked)
            return service.Name +
                " is already booked for this event.";

        // Check overlapping accepted bookings
        DateTime bookingDate = ev.EventDate.Date;
        DateTime nextDate = bookingDate.AddDays(1);

        var existingBookings = await _context.SupplierBookings
            .Where(b =>
                b.SupplierId == service.SupplierId &&
                b.EventId != ev.Id &&
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
        bool supplierTaken = existingBookings.Any(b =>
            ev.StartTime < b.EndTime &&
            ev.EndTime > b.StartTime);

        if (supplierTaken)
        {
            return service.Name +
                " is not available during your event schedule.";
        }

        return null;
    }

    // Create booking
    private SupplierBooking CreateBooking(
        EventRecord ev,
        SupplierService service,
        decimal price,
        int? packageId)
    {
        return new SupplierBooking
        {
            EventId = ev.Id,
            SupplierId = service.SupplierId,
            ServiceId = service.Id,
            ServiceName = service.Name,
            PackageId = packageId,
            Price = price,
            BookingDate = ev.EventDate,
            StartTime = ev.StartTime,
            EndTime = ev.EndTime,
            Status = BookingStatus.Pending
        };
    }
}
