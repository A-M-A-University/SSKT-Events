using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSKT_Events.Models;

public class Customer
{
    [Key]
    public int Id { get; set; }

    [Display(Name = "Full Name")]
    [Required(ErrorMessage = "Enter your full name. - Juan")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; } = "";

    [Display(Name = "Email Address")]
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string Email { get; set; } = "";

    [Display(Name = "Confirm Email")]
    [Required(ErrorMessage = "Please confirm your email address.")]
    [NotMapped]
    [Compare("Email", ErrorMessage = "Email addresses do not match.")]
    public string ConfirmEmail { get; set; } = "";
}
