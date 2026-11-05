using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSKT_Events.Models;

public class Organizer
{
    [Key]
    public int Id { get; set; }

    [Display(Name = "Organizer Name")]
    [Required(ErrorMessage = "Enter the organizer's name. - Juan")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; } = "";

    [Display(Name = "Email Address")]
    [Required(ErrorMessage = "Enter the email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string Email { get; set; } = "";

    [Display(Name = "Confirm Email")]
    [Required(ErrorMessage = "Please confirm the email address.")]
    [NotMapped]
    [Compare("Email", ErrorMessage = "Email addresses do not match.")]
    public string ConfirmEmail { get; set; } = "";

    [Display(Name = "Phone Number")]
    [Required(ErrorMessage = "Enter a phone number.")]
    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    public string Phone { get; set; } = "";
}
