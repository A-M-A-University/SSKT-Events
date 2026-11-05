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

    [Display(Name = "Payment Amount")]
    [Required(ErrorMessage = "Please enter the payment amount.")]
    [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "Payment amount must be greater than zero.")]
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

    public bool IsPaid => PaidDate.HasValue;

    public bool IsOverdue => !IsPaid && DueDate.Date < DateTime.Today;
}
