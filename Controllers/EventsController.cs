using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSKT_Events.Data;
using SSKT_Events.Models;
using SSKT_Events.Services;

namespace SSKT_Events.Controllers;

[Authorize(Roles = "Client")]
public class EventsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly PayMongoService _payMongoService;

    public EventsController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        PayMongoService payMongoService)
    {
        _context = context;
        _userManager = userManager;
        _payMongoService = payMongoService;
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

    // PAY WITH PAYMONGO (GET)
    [HttpGet]
    public async Task<IActionResult> AddPayment(int id)
    {
        var eventRecord = await GetCustomerEventAsync(id);

        if (eventRecord == null)
            return NotFound();

        if (eventRecord.Status == EventStatus.Cancelled)
        {
            TempData["Error"] = "Payments cannot be made for cancelled events.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!eventRecord.HasConfirmedBookings)
        {
            TempData["Error"] = "You can pay once a supplier accepts a booking.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (eventRecord.OutstandingBalance <= 0)
        {
            TempData["Success"] = "This event is already fully paid.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(eventRecord);
    }

    // INITIATE PAYMONGO CHECKOUT SESSION (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPayment(
        int eventId,
        decimal amount,
        bool isDownPayment)
    {
        var eventRecord = await GetCustomerEventAsync(eventId);

        if (eventRecord == null)
            return NotFound();

        if (eventRecord.Status == EventStatus.Cancelled)
        {
            TempData["Error"] = "Payments cannot be made for cancelled events.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        if (!eventRecord.HasConfirmedBookings)
        {
            TempData["Error"] = "You can pay once a supplier accepts a booking.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        if (amount < 20m)
        {
            TempData["Error"] = "PayMongo requires a minimum payment of ₱20.00.";
            return RedirectToAction(nameof(AddPayment), new { id = eventId });
        }

        if (amount > eventRecord.OutstandingBalance)
        {
            TempData["Error"] =
                "Payment cannot exceed the outstanding balance of ₱" +
                eventRecord.OutstandingBalance.ToString("N2") + ".";
            return RedirectToAction(nameof(AddPayment), new { id = eventId });
        }

        // Create pending (unpaid) payment record first
        var payment = new EventPayment
        {
            EventId = eventId,
            Amount = amount,
            IsDownPayment = isDownPayment,
            DueDate = DateTime.Today,
            PaidDate = null,
            ReceiptNumber = ""
        };

        _context.EventPayments.Add(payment);
        await _context.SaveChangesAsync();

        var successUrl = Url.Action(
            nameof(PaymentSuccess),
            "Events",
            new { paymentId = payment.Id },
            Request.Scheme)!;

        var cancelUrl = Url.Action(
            nameof(PaymentCancel),
            "Events",
            new { paymentId = payment.Id },
            Request.Scheme)!;

        try
        {
            var session = await _payMongoService.CreateCheckoutSessionAsync(
                payment.Id,
                eventRecord.Name,
                amount,
                isDownPayment,
                successUrl,
                cancelUrl);

            payment.PayMongoCheckoutSessionId = session.CheckoutSessionId;
            await _context.SaveChangesAsync();

            // Redirect user directly to PayMongo's Hosted Checkout page
            return Redirect(session.CheckoutUrl);
        }
        catch (Exception ex)
        {
            // Clean up pending record if PayMongo session creation fails
            _context.EventPayments.Remove(payment);
            await _context.SaveChangesAsync();

            TempData["Error"] = "Unable to connect to PayMongo: " + ex.Message;
            return RedirectToAction(nameof(AddPayment), new { id = eventId });
        }
    }

    // PAYMONGO SUCCESS CALLBACK
    [HttpGet]
    public async Task<IActionResult> PaymentSuccess(int paymentId)
    {
        var customerId = await GetCurrentCustomerIdAsync();
        if (customerId == null)
            return Forbid();

        var payment = await _context.EventPayments
            .Include(p => p.Event)
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.Event!.CustomerId == customerId);

        if (payment == null)
            return NotFound();

        if (!payment.IsPaid && !string.IsNullOrEmpty(payment.PayMongoCheckoutSessionId))
        {
            bool isPaid = await _payMongoService.IsCheckoutSessionPaidAsync(payment.PayMongoCheckoutSessionId);
            if (isPaid)
            {
                payment.PaidDate = DateTime.Today;
                payment.ReceiptNumber = "PM-" + payment.Id.ToString("0000");
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Payment of ₱{payment.Amount:N2} confirmed via PayMongo! Receipt: {payment.ReceiptNumber}";
            }
            else
            {
                TempData["Error"] = "Payment was not completed on PayMongo.";
            }
        }

        return RedirectToAction(nameof(Details), new { id = payment.EventId });
    }

    // PAYMONGO CANCEL CALLBACK
    [HttpGet]
    public async Task<IActionResult> PaymentCancel(int paymentId)
    {
        var customerId = await GetCurrentCustomerIdAsync();
        if (customerId == null)
            return Forbid();

        var payment = await _context.EventPayments
            .Include(p => p.Event)
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.Event!.CustomerId == customerId);

        if (payment == null)
            return NotFound();

        int eventId = payment.EventId;

        if (!payment.IsPaid)
        {
            _context.EventPayments.Remove(payment);
            await _context.SaveChangesAsync();
        }

        TempData["Error"] = "PayMongo payment was cancelled.";
        return RedirectToAction(nameof(Details), new { id = eventId });
    }

    [HttpGet]
    public async Task<IActionResult> MockCheckout(int paymentId, string sessionId)
    {
        var customerId = await GetCurrentCustomerIdAsync();
        if (customerId == null)
            return Forbid();

        var payment = await _context.EventPayments
            .Include(p => p.Event)
            .FirstOrDefaultAsync(p =>
                p.Id == paymentId &&
                p.Event!.CustomerId == customerId &&
                p.PayMongoCheckoutSessionId == sessionId);

        if (payment == null)
            return NotFound();

        return View(payment);
    }
}