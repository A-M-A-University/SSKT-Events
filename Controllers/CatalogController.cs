using Microsoft.AspNetCore.Mvc;
using SSKT_Events.Models;

namespace SSKT_Events.Controllers;

public class CatalogController : Controller
{
    // Temporary sample data, no database yet
    public static readonly List<Supplier> suppliers = new()
    {
        new Supplier { Id = 1, Name = "Sound Masters", Email = "sound@example.com", Phone = "0917-000-0001" },
        new Supplier { Id = 2, Name = "Sweet Bites Catering", Email = "sweet@example.com", Phone = "0917-000-0002" },
        new Supplier { Id = 3, Name = "Lens & Light Photography", Email = "lens@example.com", Phone = "0917-000-0003" }
    };

    public static readonly List<SupplierService> services = new()
    {
        new SupplierService { Id = 1, SupplierId = 1, Name = "Sound System", Description = "Speakers, mixer and operator", Price = 15000 },
        new SupplierService { Id = 2, SupplierId = 2, Name = "Catering (100 pax)", Description = "Buffet with 3 mains and dessert", Price = 60000 },
        new SupplierService { Id = 3, SupplierId = 3, Name = "Event Photography", Description = "6 hours, edited photos", Price = 20000 },
        new SupplierService { Id = 4, SupplierId = 1, Name = "Stage Lighting", Description = "Basic stage and mood lighting", Price = 12000 }
    };

    public static readonly List<EventPackage> packages = new()
    {
        new EventPackage
        {
            Id = 1, OrganizerId = 1, Name = "Debut Package",
            Description = "Sound, catering and photography for a debut",
            Price = 85000, ServiceIds = new List<int> { 1, 2, 3 }, IsPublished = true
        },
        new EventPackage
        {
            Id = 2, OrganizerId = 1, Name = "Basic Birthday",
            Description = "Catering and sound for small parties",
            Price = 70000, ServiceIds = new List<int> { 1, 2 }, IsPublished = true
        },
        new EventPackage
        {
            Id = 3, OrganizerId = 1, Name = "Corporate Package (draft)",
            Description = "Not published yet, clients can't see this",
            Price = 40000, ServiceIds = new List<int> { 1, 4 }, IsPublished = false
        }
    };

    // eventId = the event the client is booking for
    public IActionResult Index(int eventId)
    {
        var eventRecord = EventsController.events.FirstOrDefault(e => e.Id == eventId);

        if (eventRecord == null)
            return NotFound();

        ViewBag.Event = eventRecord;
        ViewBag.Packages = packages.Where(p => p.IsPublished).ToList();
        ViewBag.Services = services.Where(s => s.IsActive).ToList();
        ViewBag.Suppliers = suppliers;

        return View();
    }

    [HttpPost]
    public IActionResult BookService(int eventId, int serviceId)
    {
        var ev = EventsController.events.FirstOrDefault(e => e.Id == eventId);
        var service = services.FirstOrDefault(s => s.Id == serviceId);

        if (ev == null || service == null)
            return NotFound();

        var error = CheckBookable(ev, service);

        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction("Index", new { eventId });
        }

        ev.Bookings.Add(CreateBooking(ev, service, service.Price, null));

        // Clear any previous error message after a successful booking
        TempData.Remove("Error");

        TempData["Success"] = service.Name + " requested. Waiting for the supplier to accept.";

        return RedirectToAction("Details", "Events", new { id = eventId });
    }

    [HttpPost]
    public IActionResult BookPackage(int eventId, int packageId)
    {
        var ev = EventsController.events.FirstOrDefault(e => e.Id == eventId);
        var package = packages.FirstOrDefault(p => p.Id == packageId && p.IsPublished);

        if (ev == null || package == null)
            return NotFound();

        // Get the package's selected services
        var serviceIds = package.ServiceIds.Distinct().ToList();

        var included = services
            .Where(s => serviceIds.Contains(s.Id) && s.IsActive)
            .ToList();

        // Make sure all package services are available
        if (included.Count == 0 || included.Count != serviceIds.Count)
        {
            TempData["Error"] = "One or more services in this package are unavailable.";
            return RedirectToAction("Index", new { eventId });
        }

        // Check every service before creating any bookings
        foreach (var service in included)
        {
            var error = CheckBookable(ev, service);

            if (error != null)
            {
                TempData["Error"] = "Can't book " + package.Name + ": " + error;
                return RedirectToAction("Index", new { eventId });
            }
        }

        // Calculate each service's share of the package price
        var totalServicePrice = included.Sum(s => s.Price);
        decimal remainingPrice = package.Price;

        for (int i = 0; i < included.Count; i++)
        {
            var service = included[i];

            decimal share;

            if (i == included.Count - 1)
            {
                // Assign any rounding difference to the last service
                share = remainingPrice;
            }
            else
            {
                share = totalServicePrice == 0
                    ? 0
                    : Math.Round(package.Price * service.Price / totalServicePrice, 2);

                remainingPrice -= share;
            }

            ev.Bookings.Add(CreateBooking(ev, service, share, package.Id));
        }

        // Clear any previous error message after a successful booking
        TempData.Remove("Error");

        TempData["Success"] = package.Name + " requested. Waiting for the suppliers to accept.";

        return RedirectToAction("Details", "Events", new { id = eventId });
    }

    // Returns an error message if the service can't be booked, or null if it's fine
    private string? CheckBookable(EventRecord ev, SupplierService service)
    {
        if (ev.Status == EventStatus.Cancelled)
            return "This event is cancelled.";

        if (!service.IsActive)
            return service.Name + " is currently unavailable.";

        var supplierExists = suppliers.Any(s => s.Id == service.SupplierId);

        if (!supplierExists)
            return "The supplier for " + service.Name + " could not be found.";

        var alreadyBooked = ev.Bookings.Any(b =>
            b.ServiceId == service.Id &&
            (b.Status == BookingStatus.Pending ||
             b.Status == BookingStatus.Accepted));

        if (alreadyBooked)
            return service.Name + " is already booked for this event.";

        // Check whether the supplier has an accepted booking
        // for a DIFFERENT event on the same date.
        var supplierTaken = EventsController.events
            .SelectMany(e => e.Bookings)
            .Any(b =>
                b.SupplierId == service.SupplierId &&
                b.EventId != ev.Id &&
                b.BookingDate.Date == ev.EventDate.Date &&
                b.Status == BookingStatus.Accepted);

        if (supplierTaken)
            return service.Name + " is not available on " +
                ev.EventDate.ToString("MMM dd, yyyy") + ".";

        return null;
    }

    private SupplierBooking CreateBooking(EventRecord ev, SupplierService service, decimal price, int? packageId)
    {
        var allBookings = EventsController.events.SelectMany(e => e.Bookings).ToList();

        return new SupplierBooking
        {
            Id = allBookings.Count == 0 ? 1 : allBookings.Max(b => b.Id) + 1,
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
