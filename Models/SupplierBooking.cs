using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SSKT_Events.Validation;   

namespace SSKT_Events.Models;

public class SupplierBooking
{
    [Key]
    public int Id { get; set; }

    public int EventId { get; set; }

    [ForeignKey(nameof(EventId))]
    public EventRecord? Event { get; set; }

    public int SupplierId { get; set; }

    public int ServiceId { get; set; }

    [ForeignKey(nameof(ServiceId))]
    public SupplierService? Service { get; set; }

    public string ServiceName { get; set; } = "";

    public int? PackageId { get; set; }

    [ForeignKey(nameof(PackageId))]
    public EventPackage? Package { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public Supplier? Supplier { get; set; }

    public bool IsFromPackage => PackageId.HasValue;

    [Required(ErrorMessage = "Enter the booking price.")]
    [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    [Display(Name = "Booking Date")]
    [Required(ErrorMessage = "Choose the booking date.")]
    [DataType(DataType.Date)]
    [NotPastDate(ErrorMessage = "The booking date cannot be in the past.")]
    public DateTime BookingDate { get; set; }

    [Display(Name = "Start Time")]
    [Required(ErrorMessage = "Enter the start time.")]
    public TimeSpan StartTime { get; set; }

    [Display(Name = "End Time")]
    [Required(ErrorMessage = "Enter the end time.")]
    [TimeAfter("StartTime", ErrorMessage = "End time must be later than start time.")]
    public TimeSpan EndTime { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public string ModificationRequest { get; set; } = "";
}