using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    public bool IsFromPackage => PackageId.HasValue;

    public decimal Price { get; set; }
    public DateTime BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public string ModificationRequest { get; set; } = "";
}