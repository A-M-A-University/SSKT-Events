using Microsoft.AspNetCore.Mvc;
using SSKT_Events.Models;

namespace SSKT_Events.Controllers;

public class EventsController : Controller
{
    // Temporary sample data, no database yet
    public static readonly List<EventRecord> events = new()
    {

    };

    // Create an event
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(EventRecord newEvent)
    {
        if (!ModelState.IsValid)
        {
            return View(newEvent);
        }

        newEvent.Id = events.Count == 0 ? 1 : events.Max(e => e.Id) + 1;
        newEvent.CustomerId = 1;
        newEvent.Status = EventStatus.Draft;
        newEvent.CreatedAt = DateTime.Now;

        events.Add(newEvent);

        return RedirectToAction("Index");
    }

    // EVENT DETAILS
    public IActionResult Details(int id)
    {
        var eventRecord = events.FirstOrDefault(e => e.Id == id);

        if (eventRecord == null)
            return NotFound();

        eventRecord.UpdateStatus();

        return View(eventRecord);
    }

    // EDIT EVENT
    [HttpGet]
    public IActionResult Edit(int id)
    {
        var eventRecord = events.FirstOrDefault(e => e.Id == id);

        if (eventRecord == null)
            return NotFound();

        return View(eventRecord);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(EventRecord updated)
    {
        if (!ModelState.IsValid)
        {
            return View(updated);
        }

        var eventRecord = events.FirstOrDefault(e => e.Id == updated.Id);

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

        return RedirectToAction("Details", new { id = eventRecord.Id });
    }

    // CANCEL EVENT
    [HttpGet]
    public IActionResult Cancel(int id)
    {
        var eventRecord = events.FirstOrDefault(e => e.Id == id);

        if (eventRecord == null)
            return NotFound();

        if (eventRecord.Status == EventStatus.Cancelled)
            return RedirectToAction("Details", new { id });

        return View(eventRecord);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cancel(int id, string? reason)
    {
        var eventRecord = events.FirstOrDefault(e => e.Id == id);

        if (eventRecord == null)
            return NotFound();

        if (eventRecord.Status != EventStatus.Cancelled)
        {
            eventRecord.Cancel(reason ?? "");
        }

        return RedirectToAction("Details", new { id = eventRecord.Id });
    }

    // ADD PAYMENT
    [HttpGet]
    public IActionResult AddPayment(int id)
    {
        var eventRecord = events.FirstOrDefault(e => e.Id == id);

        if (eventRecord == null)
            return NotFound();

        if (eventRecord.Status == EventStatus.Cancelled)
        {
            TempData["Error"] = "Payments cannot be added to cancelled events.";
            return RedirectToAction("Details", new { id });
        }

        if (!eventRecord.HasConfirmedBookings)
        {
            TempData["Error"] = "You can add payments once a supplier accepts a booking.";
            return RedirectToAction("Details", new { id });
        }

        return View(eventRecord);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddPayment(
        int eventId,
        decimal amount,
        bool isDownPayment,
        DateTime dueDate,
        bool markPaid)
    {
        var eventRecord = events.FirstOrDefault(e => e.Id == eventId);

        if (eventRecord == null)
            return NotFound();

        if (eventRecord.Status == EventStatus.Cancelled)
        {
            TempData["Error"] = "Payments cannot be added to cancelled events.";
            return RedirectToAction("Details", new { id = eventId });
        }

        if (!eventRecord.HasConfirmedBookings)
        {
            TempData["Error"] = "You can add payments once a supplier accepts a booking.";
            return RedirectToAction("Details", new { id = eventId });
        }

        if (amount <= 0)
        {
            TempData["Error"] = "Payment amount must be greater than zero.";
            return RedirectToAction("AddPayment", new { id = eventId });
        }

        if (amount > eventRecord.OutstandingBalance)
        {
            TempData["Error"] = "Payment can't be more than the outstanding balance (₱"
                + eventRecord.OutstandingBalance.ToString("N2") + ").";

            return RedirectToAction("AddPayment", new { id = eventId });
        }

        if (dueDate == default)
        {
            TempData["Error"] = "Please provide a valid payment due date.";
            return RedirectToAction("AddPayment", new { id = eventId });
        }

        var allPayments = events.SelectMany(e => e.Payments).ToList();

        var newId = allPayments.Count == 0
            ? 1
            : allPayments.Max(p => p.Id) + 1;

        var payment = new EventPayment
        {
            Id = newId,
            EventId = eventId,
            Amount = amount,
            IsDownPayment = isDownPayment,
            DueDate = dueDate
        };

        if (markPaid)
        {
            payment.PaidDate = DateTime.Today;
            payment.ReceiptNumber = "RCPT-" + newId.ToString("0000");
        }

        eventRecord.Payments.Add(payment);

        TempData["Success"] = "Payment successfully added.";

        return RedirectToAction("Details", new { id = eventId });
    }

    // Index
    public IActionResult Index()
    {
        foreach (var e in events)
        {
            e.UpdateStatus();
        }

        return View(events);
    }
}
