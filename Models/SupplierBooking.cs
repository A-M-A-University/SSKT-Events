namespace SSKT_Events.Models;

public class SupplierBooking
{
    public int Id { get; set; }
    public int EventId { get; set; }

    public int SupplierId { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = "";

    public int? PackageId { get; set; }
    public bool IsFromPackage => PackageId.HasValue;

    public decimal Price { get; set; }
    public DateTime BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public string ModificationRequest { get; set; } = "";
}