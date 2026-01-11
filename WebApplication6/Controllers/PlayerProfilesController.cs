using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication6.Data;
using WebApplication6.Models;

[ApiController]
[Route("api/players")]
public class PlayerProfilesController : ControllerBase
{
    private readonly AppDbContext _context;

    public PlayerProfilesController(AppDbContext context)
    {
        _context = context;
    }

    // 🔹 GET all players (ყველას შეუძლია ნახვა)
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _context.PlayerProfiles.ToListAsync());

    // 🔹 Add player (Admin / Premium)
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] PlayerProfile player)
    {
        _context.PlayerProfiles.Add(player);
        await _context.SaveChangesAsync();
        return Ok(player);
    }

    // 🔹 Update player (Admin / Premium)
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PlayerProfile request)
    {
        var player = await _context.PlayerProfiles.FindAsync(id);
        if (player == null) return NotFound();

        // Map fields
        player.Name = request.Name;
        player.Age = request.Age;
        player.Sport = request.Sport;
        player.Position = request.Position;
        player.Height = request.Height;
        player.Country = request.Country;
        player.PhotoUrl = request.PhotoUrl;
        player.VideoUrl = request.VideoUrl;
        player.WeightCategory = request.WeightCategory;
        player.Belt = request.Belt;
        player.WeightClass = request.WeightClass;
        player.Record = request.Record;

        await _context.SaveChangesAsync();
        return Ok(player);
    }

    // 🔹 Delete player (Admin only)
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var player = await _context.PlayerProfiles.FindAsync(id);
        if (player == null) return NotFound();

        _context.PlayerProfiles.Remove(player);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
