using System.ComponentModel.DataAnnotations;

namespace SSKT_Events.Models;

public class Customer
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}