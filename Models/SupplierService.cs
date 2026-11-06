using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSKT_Events.Models;

public class SupplierService
{
    [Key]
    public int Id { get; set; }

    public int SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public Supplier? Supplier { get; set; }

    [Required(ErrorMessage = "Service Name is required.")]
    [StringLength(100, ErrorMessage = "Service Name cannot exceed 100 characters.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "Price is required.")]
    [Range(0.01, 1000000, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;
}