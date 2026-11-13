using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SSKT_Events.Validation;

namespace SSKT_Events.Models;

public class EventRecord
{
    [Key]
    public int Id { get; set; }

    public int CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public Customer? Customer { get; set; }

    [Display(Name = "Event Name")]
    [Required(ErrorMessage = "Enter the event name. - Juan's Event")]
    [StringLength(100, ErrorMessage = "Event name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Event Type")]
    [Required(ErrorMessage = "Select an event type.")]
    public EventType Type { get; set; } = EventType.Other;

    [Display(Name = "Event Date")]
    [Required(ErrorMessage = "Choose an event date.")]
    [DataType(DataType.Date)]
    [NotPastDate(ErrorMessage = "The event date cannot be in the past.")]
    public DateTime EventDate { get; set; }

    [Display(Name = "Start Time")]
    [Required(ErrorMessage = "Enter the event start time.")]
    public TimeSpan StartTime { get; set; }

    [Display(Name = "End Time")]
    [Required(ErrorMessage = "Enter the event end time.")]
    [TimeAfter("StartTime", ErrorMessage = "End time must be later than start time.")]
    public TimeSpan EndTime { get; set; }

    [Display(Name = "Event Location")]
    [Required(ErrorMessage = "Enter the event location.")]
    [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters.")]
    public string Location { get; set; } = string.Empty;

    [Display(Name = "Expected Guests")]
    [Required(ErrorMessage = "Enter the expected number of guests.")]
    [Range(1, 10000, ErrorMessage = "Expected guests must be between 1 and 10,000.")]
    public int ExpectedGuests { get; set; }

    [Display(Name = "Event Description")]
    [Required(ErrorMessage = "Enter the description of the event.")]
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1,000 characters.")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Special Requirements")]
    [StringLength(1000, ErrorMessage = "Special requirements cannot exceed 1,000 characters.")]
    public string? SpecialRequirements { get; set; }

    // Budget and its status
    [Display(Name = "Total Budget")]
    [Required(ErrorMessage = "Enter the event budget.")]
    [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "Budget must be greater than zero.")]
    public decimal TotalBudget { get; set; }

    public EventStatus Status { get; set; } = EventStatus.Draft;

    // Cancellation
    public string CancellationReason { get; set; } = "";
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Bookings and payments
    public List<SupplierBooking> Bookings { get; set; } = new();
    public List<EventPayment> Payments { get; set; } = new();

    // Budget calculation
    public decimal TotalBookedCost =>
        Bookings.Where(b => b.Status == BookingStatus.Accepted)
                .Sum(b => b.Price);

    public decimal RemainingBudget => TotalBudget - TotalBookedCost;

    public double BudgetUsedPercent =>
        TotalBudget == 0 ? 0 : (double)(TotalBookedCost / TotalBudget * 100);

    public bool IsOverBudget => TotalBookedCost > TotalBudget;

    public decimal TotalPaid =>
        Payments.Where(p => p.IsPaid).Sum(p => p.Amount);

    public decimal OutstandingBalance => TotalBookedCost - TotalPaid;

    public bool HasConfirmedBookings =>
        Bookings.Any(b => b.Status == BookingStatus.Accepted);

    public bool RequiresSupplierApprovalOnChange => HasConfirmedBookings;

    public void UpdateStatus()
    {
        if (Status == EventStatus.Cancelled)
            return;

        if (Status == EventStatus.Draft && !HasConfirmedBookings)
            return;

        DateTime start = EventDate.Date + StartTime;
        DateTime end = EventDate.Date + EndTime;

        if (end <= start)
            end = end.AddDays(1);

        if (DateTime.Now >= end)
            Status = EventStatus.Completed;
        else if (DateTime.Now >= start)
            Status = EventStatus.Ongoing;
        else
            Status = EventStatus.Upcoming;
    }

    public void Cancel(string reason = "")
    {
        Status = EventStatus.Cancelled;
        CancellationReason = reason;
        CancelledAt = DateTime.Now;

        foreach (var booking in Bookings)
        {
            if (booking.Status == BookingStatus.Pending ||
                booking.Status == BookingStatus.Accepted)
            {
                booking.Status = BookingStatus.Cancelled;
            }
        }
    }
}