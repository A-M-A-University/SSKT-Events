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

    public int? BookingId { get; set; }

    [ForeignKey("BookingId")]
    public SupplierBooking? Booking { get; set; }

    [Display(Name = "Payment Amount")]
    [Required(ErrorMessage = "Please enter the payment amount.")]
    [Range(typeof(decimal), "20.00", "999999999", ErrorMessage = "Minimum PayMongo payment is ₱20.00.")]
    public decimal Amount { get; set; }

    [Display(Name = "Down Payment")]
    public bool IsDownPayment { get; set; }

    [Display(Name = "Due Date")]
    [Required(ErrorMessage = "Choose the payment due date.")]
    [DataType(DataType.Date)]
    public DateTime DueDate { get; set; }

    [Display(Name = "Paid Date")]
    [DataType(DataType.Date)]
    public DateTime? PaidDate { get; set; }

    [Display(Name = "Receipt Number")]
    [StringLength(50, ErrorMessage = "Receipt number cannot exceed 50 characters.")]
    public string ReceiptNumber { get; set; } = "";

    [StringLength(100)]
    public string? PayMongoCheckoutSessionId { get; set; }

    public bool IsPaid => PaidDate.HasValue;

    public bool IsOverdue => !IsPaid && DueDate.Date < DateTime.Today;
}