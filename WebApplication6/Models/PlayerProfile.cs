namespace WebApplication6.Models;

public class PlayerProfile
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;
    public int Age { get; set; }
    public string Sport { get; set; } = null!;

    // Football / Basketball
    public string? Position { get; set; }
    public int? Height { get; set; }

    // Shared
    public string? Country { get; set; }
    public string? PhotoUrl { get; set; }
    public string? VideoUrl { get; set; }

    // Judo
    public string? WeightCategory { get; set; }
    public string? Belt { get; set; }

    // MMA
    public string? WeightClass { get; set; }
    public string? Record { get; set; }
}
