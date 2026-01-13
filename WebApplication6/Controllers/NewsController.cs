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
        private readonly IWebHostEnvironment _env;

        public NewsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ✅ GET all news
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var newsList = await _context.News
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            return Ok(newsList);
        }

        // ✅ GET single news
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();
            return Ok(news);
        }

        // ✅ POST news with optional image
        [Authorize(Roles = "Admin,Premium")]
        [HttpPost]
        public async Task<IActionResult> Add([FromForm] News news, IFormFile? image)
        {
            if (image != null && image.Length > 0)
            {
                var imagesFolder = Path.Combine(_env.WebRootPath, "images");
                if (!Directory.Exists(imagesFolder))
                    Directory.CreateDirectory(imagesFolder);

                var fileName = $"{Guid.NewGuid()}_{image.FileName}";
                var filePath = Path.Combine(imagesFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await image.CopyToAsync(stream);

                news.ImageUrl = "/images/" + fileName; // URL ფაილისთვის
            }

            news.CreatedAt = DateTime.UtcNow;
            _context.News.Add(news);
            await _context.SaveChangesAsync();
            return Ok(news);
        }

        // ✅ PUT news with optional image update
        [Authorize(Roles = "Admin,Premium")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] News updatedNews, IFormFile? image)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();

            news.Title = updatedNews.Title;
            news.Content = updatedNews.Content;
            news.Category = updatedNews.Category;

            if (image != null && image.Length > 0)
            {
                var imagesFolder = Path.Combine(_env.WebRootPath, "images");
                if (!Directory.Exists(imagesFolder))
                    Directory.CreateDirectory(imagesFolder);

                var fileName = $"{Guid.NewGuid()}_{image.FileName}";
                var filePath = Path.Combine(imagesFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await image.CopyToAsync(stream);

                news.ImageUrl = "/images/" + fileName;
            }

            await _context.SaveChangesAsync();
            return Ok(news);
        }

        // ✅ DELETE news
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
