using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using WebApplication6.Data;
using WebApplication6.Models;

[ApiController]
[Route("api/players")]
public class PlayerProfilesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<PlayerProfilesController> _logger;
    private readonly IHostEnvironment _env;

    public PlayerProfilesController(AppDbContext context, ILogger<PlayerProfilesController> logger, IHostEnvironment env)
    {
        _context = context;
        _logger = logger;
        _env = env;
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
        if (player is null)
            return BadRequest(new { error = "Missing request body" });

        if (string.IsNullOrWhiteSpace(player.Name))
            return BadRequest(new { error = "Name is required" });

        if (string.IsNullOrWhiteSpace(player.Sport))
            return BadRequest(new { error = "Sport is required" });

        // Prevent clients from forcing a specific primary key value.
        player.Id = 0;

        try
        {
            _context.PlayerProfiles.Add(player);
            await _context.SaveChangesAsync();
            return Ok(player);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to create PlayerProfile. Inner: {Inner}", ex.InnerException?.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Database error while saving player",
                detail = _env.IsDevelopment() ? ex.InnerException?.Message ?? ex.Message : null
            });
        }
    }

    // 🔹 Update player (Admin / Premium)
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PlayerProfile request)
    {
        var player = await _context.PlayerProfiles.FindAsync(id);
        if (player == null) return NotFound();

        if (request is null)
            return BadRequest(new { error = "Missing request body" });

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required" });

        if (string.IsNullOrWhiteSpace(request.Sport))
            return BadRequest(new { error = "Sport is required" });

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

        try
        {
            await _context.SaveChangesAsync();
            return Ok(player);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update PlayerProfile {Id}. Inner: {Inner}", id, ex.InnerException?.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Database error while updating player",
                detail = _env.IsDevelopment() ? ex.InnerException?.Message ?? ex.Message : null
            });
        }
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
