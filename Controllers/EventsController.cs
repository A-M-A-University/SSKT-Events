using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSKT_Events.Data;
using SSKT_Events.Models;

namespace SSKT_Events.Controllers;

[Authorize(Roles = "Client")]
public class EventsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public EventsController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Get the currently logged-in Client's profile
    private async Task<int?> GetCurrentCustomerIdAsync()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return null;

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.UserId == userId);

        return customer?.Id;
    }

    // Load an event only if it belongs to the current Client
    private async Task<EventRecord?> GetCustomerEventAsync(int id)
    {
        var customerId = await GetCurrentCustomerIdAsync();

        if (customerId == null)
            return null;

        return await _context.Events
            .Include(e => e.Payments)
            .Include(e => e.Bookings)
            .FirstOrDefaultAsync(e =>
                e.Id == id &&
                e.CustomerId == customerId);
    }

    // INDEX
    public async Task<IActionResult> Index()
    {
        var customerId = await GetCurrentCustomerIdAsync();

        if (customerId == null)
            return Forbid();

        var customerEvents = await _context.Events
            .Where(e => e.CustomerId == customerId)
            .ToListAsync();

        foreach (var e in customerEvents)
        {
            e.UpdateStatus();
        }

        await _context.SaveChangesAsync();

        return View(customerEvents);
    }

    // CREATE EVENT
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,Type,EventDate,StartTime,EndTime,Location,TotalBudget,ExpectedGuests,Description,SpecialRequirements")]
        EventRecord newEvent)
    {
        if (!ModelState.IsValid)
            return View(newEvent);

        var customerId = await GetCurrentCustomerIdAsync();

        if (customerId == null)
            return Forbid();

        newEvent.CustomerId = customerId.Value;
        newEvent.Status = EventStatus.Draft;
        newEvent.CreatedAt = DateTime.Now;

        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // EVENT DETAILS
    public async Task<IActionResult> Details(int id)
    {
        var eventRecord = await GetCustomerEventAsync(id);

        if (eventRecord == null)
            return NotFound();

        eventRecord.UpdateStatus();
        await _context.SaveChangesAsync();

        return View(eventRecord);
    }

    // EDIT EVENT
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var eventRecord = await GetCustomerEventAsync(id);

        if (eventRecord == null)
            return NotFound();

        return View(eventRecord);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        [Bind("Id,Name,Type,EventDate,StartTime,EndTime,Location,TotalBudget,ExpectedGuests,Description,SpecialRequirements")]
        EventRecord updated)
    {
        if (!ModelState.IsValid)
            return View(updated);

        var eventRecord = await GetCustomerEventAsync(updated.Id);

        if (eventRecord == null)
            return NotFound();

        eventRecord.Name = updated.Name;
        eventRecord.Type = updated.Type;
        eventRecord.EventDate = updated.EventDate;
        eventRecord.StartTime = updated.StartTime;
        eventRecord.EndTime = updated.EndTime;
        eventRecord.Location = updated.Location;
        eventRecord.TotalBudget = updated.TotalBudget;
        eventRecord.ExpectedGuests = updated.ExpectedGuests;
        eventRecord.Description = updated.Description;
        eventRecord.SpecialRequirements = updated.SpecialRequirements;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details),
            new { id = eventRecord.Id });
    }

    // CANCEL EVENT
    [HttpGet]
    public async Task<IActionResult> Cancel(int id)
    {
        var eventRecord = await GetCustomerEventAsync(id);

        if (eventRecord == null)
            return NotFound();

        if (eventRecord.Status == EventStatus.Cancelled)
            return RedirectToAction(nameof(Details), new { id });

        return View(eventRecord);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason)
    {
        var eventRecord = await GetCustomerEventAsync(id);

        if (eventRecord == null)
            return NotFound();

        if (eventRecord.Status != EventStatus.Cancelled)
        {
            eventRecord.Cancel(reason ?? "");
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details),
            new { id = eventRecord.Id });
    }

    // ADD PAYMENT
    [HttpGet]
    public async Task<IActionResult> AddPayment(int id)
    {
        var eventRecord = await GetCustomerEventAsync(id);

        if (eventRecord == null)
            return NotFound();

        if (eventRecord.Status == EventStatus.Cancelled)
        {
            TempData["Error"] =
                "Payments cannot be added to cancelled events.";

            return RedirectToAction(nameof(Details), new { id });
        }

        if (!eventRecord.HasConfirmedBookings)
        {
            TempData["Error"] =
                "You can add payments once a supplier accepts a booking.";

            return RedirectToAction(nameof(Details), new { id });
        }

        return View(eventRecord);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPayment(
        int eventId,
        decimal amount,
        bool isDownPayment,
        DateTime dueDate,
        bool markPaid)
    {
        var eventRecord = await GetCustomerEventAsync(eventId);

        if (eventRecord == null)
            return NotFound();

        if (eventRecord.Status == EventStatus.Cancelled)
        {
            TempData["Error"] =
                "Payments cannot be added to cancelled events.";

            return RedirectToAction(nameof(Details),
                new { id = eventId });
        }

        if (!eventRecord.HasConfirmedBookings)
        {
            TempData["Error"] =
                "You can add payments once a supplier accepts a booking.";

            return RedirectToAction(nameof(Details),
                new { id = eventId });
        }

        if (amount <= 0)
        {
            TempData["Error"] =
                "Payment amount must be greater than zero.";

            return RedirectToAction(nameof(AddPayment),
                new { id = eventId });
        }

        if (dueDate == default || dueDate.Date < DateTime.Today)
        {
            TempData["Error"] =
                "Please provide a valid payment due date that is not in the past.";

            return RedirectToAction(nameof(AddPayment),
                new { id = eventId });
        }

        // Prevent identical payment entries
        bool duplicatePayment = eventRecord.Payments.Any(p =>
            p.Amount == amount &&
            p.DueDate.Date == dueDate.Date &&
            p.IsDownPayment == isDownPayment &&
            p.IsPaid == markPaid);

        if (duplicatePayment)
        {
            TempData["Error"] =
                "This payment entry already exists.";

            return RedirectToAction(nameof(AddPayment),
                new { id = eventId });
        }

        // Prevent exceeding the outstanding balance
        if (amount > eventRecord.OutstandingBalance)
        {
            TempData["Error"] =
                "Payment cannot exceed the outstanding balance of ₱" +
                eventRecord.OutstandingBalance.ToString("N2") + ".";

            return RedirectToAction(nameof(AddPayment),
                new { id = eventId });
        }

        var payment = new EventPayment
        {
            EventId = eventId,
            Amount = amount,
            IsDownPayment = isDownPayment,
            DueDate = dueDate,
            ReceiptNumber = ""
        };

        if (markPaid)
        {
            payment.PaidDate = DateTime.Today;
        }

        _context.EventPayments.Add(payment);

        await _context.SaveChangesAsync();

        // Generate the receipt using the database-generated payment ID
        if (markPaid)
        {
            payment.ReceiptNumber =
                "RCPT-" + payment.Id.ToString("0000");

            await _context.SaveChangesAsync();
        }

        TempData["Success"] = "Payment successfully added.";

        return RedirectToAction(nameof(Details),
            new { id = eventId });
    }
}
