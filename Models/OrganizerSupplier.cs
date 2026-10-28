using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SSKT_Events.Models;

public class OrganizerSupplier
{
    [Key]
    public int Id { get; set; }
    public int OrganizerId { get; set; }
    [ForeignKey("OrganizerId")]
    public Organizer? Organizer { get; set; }
    public int SupplierId { get; set; }
    [ForeignKey("SupplierId")]
    public Supplier? Supplier { get; set; }
}