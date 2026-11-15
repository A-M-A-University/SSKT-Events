using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SSKT_Events.Models;

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string UserId { get; set; } = "";

    [ForeignKey(nameof(UserId))]
    public IdentityUser? User { get; set; }
}