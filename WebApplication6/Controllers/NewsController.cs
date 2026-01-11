using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication6.Data;
using WebApplication6.Models;

namespace WebApplication6.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NewsController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ GET all news (ყველას შეუძლია)
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var newsList = await _context.News
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            return Ok(newsList);
        }

        // ✅ GET single news (ყველას შეუძლია)
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();
            return Ok(news);
        }

        // ✅ POST news (Admin / Premium)
        [Authorize(Roles = "Admin,Premium")]
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] News news)
        {
            news.CreatedAt = DateTime.Now;
            _context.News.Add(news);
            await _context.SaveChangesAsync();
            return Ok(news);
        }

        // ✅ PUT news (Admin / Premium)
        [Authorize(Roles = "Admin,Premium")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] News updatedNews)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();

            news.Title = updatedNews.Title;
            news.Content = updatedNews.Content;
            news.Category = updatedNews.Category;

            await _context.SaveChangesAsync();
            return Ok(news);
        }

        // ✅ DELETE news (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();

            _context.News.Remove(news);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
