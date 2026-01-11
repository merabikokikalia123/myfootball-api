namespace WebApplication6.RequestDTO;

public class AddPlayerProfileRequest
{
    public string Name { get; set; } = null!;
    public int Age { get; set; }
    public string Sport { get; set; } = null!;

    public string? Position { get; set; }
    public int? Height { get; set; }
    public string? Country { get; set; }
    public string? PhotoUrl { get; set; }
    public string? VideoUrl { get; set; }

    public string? WeightCategory { get; set; }
    public string? Belt { get; set; }

    public string? WeightClass { get; set; }
    public string? Record { get; set; }
}
