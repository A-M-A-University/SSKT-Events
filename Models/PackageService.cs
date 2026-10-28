using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SSKT_Events.Models;

public class PackageService
{
    [Key]
    public int Id { get; set; }
    public int PackageId { get; set; }
    [ForeignKey("PackageId")]
    public EventPackage? Package { get; set; }
    public int ServiceId { get; set; }
    [ForeignKey("ServiceId")]
    public SupplierService? Service { get; set; }
}