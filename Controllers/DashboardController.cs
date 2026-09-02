using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soo_artist.Entities;
using Soo_artist.DTOs;

namespace Soo_artist.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/dashboard/stats
        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetStats()
        {
            var totalCategories = await _context.Categories.CountAsync();
            var totalProducts = await _context.Products.CountAsync();

            var latestProductsEntities = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            var latestProducts = latestProductsEntities.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty,
                CreatedAt = p.CreatedAt,
                Images = p.Images.Select(img => new ProductImageDto
                {
                    Id = img.Id,
                    Url = img.Url
                }).ToList()
            }).ToList();

            var stats = new DashboardStatsDto
            {
                TotalCategories = totalCategories,
                TotalProducts = totalProducts,
                LatestProducts = latestProducts
            };

            return Ok(stats);
        }
    }
}
