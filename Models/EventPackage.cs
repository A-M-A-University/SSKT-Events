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

    [Display(Name = "Package Name")]
    [Required(ErrorMessage = "Please enter the package name. - Sound System Package")]
    [StringLength(100, ErrorMessage = "Package name cannot exceed 100 characters.")]
    public string Name { get; set; } = "";

    [Display(Name = "Package Description")]
    [Required(ErrorMessage = "Input the package description.")]
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1,000 characters.")]
    public string Description { get; set; } = "";

    [Display(Name = "Package Price")]
    [Required(ErrorMessage = "Enter the package price.")]
    [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "Package price must be greater than zero.")]
    public decimal Price { get; set; }

    [Display(Name = "Publish Package")]
    public bool IsPublished { get; set; }

    // TEMPORARY: our pages still read this list. It breaks 1NF (many values in one
    // field), so it's replaced by PackageServices when we move to the database.
    [NotMapped]
    public List<int> ServiceIds { get; set; } = new();

    public List<PackageService> PackageServices { get; set; } = new();
}
