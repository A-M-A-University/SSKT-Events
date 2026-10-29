using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSKT_Events.Models;

// A package an event organizer builds from their partner suppliers' services
public class EventPackage
{
    [Key]
    public int Id { get; set; }

    public int OrganizerId { get; set; }
    [ForeignKey("OrganizerId")]
    public Organizer? Organizer { get; set; }

    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }

    // Clients only see the package once it's published
    public bool IsPublished { get; set; }

    // TEMPORARY: our pages still read this list. It breaks 1NF (many values in one
    // field), so it's replaced by PackageServices when we move to the database.
    [NotMapped]
    public List<int> ServiceIds { get; set; } = new();

    public List<PackageService> PackageServices { get; set; } = new();
} 