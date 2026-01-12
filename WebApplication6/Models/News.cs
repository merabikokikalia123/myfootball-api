namespace WebApplication6.Models;
public class News
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Category { get; set; }   // მაგალითად: "Football", "Judo", "Basketball", "MMA"

}

