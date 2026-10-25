namespace SSKT_Events.Models;

public class EventPackage
{
    public int Id { get; set; }
    public int OrganizerId { get; set; }

    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }

    public List<int> ServiceIds { get; set; } = new();

    public bool IsPublished { get; set; }
}