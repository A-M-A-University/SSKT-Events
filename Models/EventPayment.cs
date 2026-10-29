using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSKT_Events.Models;

public class EventPayment
{
    [Key]
    public int Id { get; set; }

    public int EventId { get; set; }
    [ForeignKey("EventId")]
    public EventRecord? Event { get; set; }

    // Optional: our payments are per event, not per booking
    public int? BookingId { get; set; }
    [ForeignKey("BookingId")]
    public SupplierBooking? Booking { get; set; }

    public decimal Amount { get; set; }
    public bool IsDownPayment { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public string ReceiptNumber { get; set; } = "";

    public bool IsPaid => PaidDate.HasValue;
    public bool IsOverdue => !IsPaid && DueDate.Date < DateTime.Today;
}
