using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soo_artist.Entities;
using Soo_artist.DTOs;
using Soo_artist.Services;

namespace Soo_artist.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;

        public CategoriesController(AppDbContext context, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        // GET: api/categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    ImageUrl = c.ImageUrl
                })
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound($"Category with ID {id} not found.");
            }

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                ImageUrl = category.ImageUrl
            };

            return Ok(categoryDto);
        }

        // POST: api/categories
        [HttpPost]
        public async Task<ActionResult<CategoryDto>> CreateCategory([FromForm] CreateCategoryDto createDto)
        {
            // Upload image to Cloudinary
            string imageUrl;
            string imagePublicId;
            try
            {
                (imageUrl, imagePublicId) = await _cloudinaryService.UploadImageAsync(createDto.Image);
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Image upload failed: {ex.Message}");
            }

            var category = new Category
            {
                Name = createDto.Name,
                ImageUrl = imageUrl,
                ImagePublicId = imagePublicId
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                ImageUrl = category.ImageUrl
            };

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, categoryDto);
        }

        // PUT: api/categories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromForm] UpdateCategoryDto updateDto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound($"Category with ID {id} not found.");
            }

            category.Name = updateDto.Name;

            // If a new image is provided, upload it and delete the old one
            if (updateDto.Image != null && updateDto.Image.Length > 0)
            {
                string oldPublicId = category.ImagePublicId;

                try
                {
                    // Upload new image
                    var (imageUrl, imagePublicId) = await _cloudinaryService.UploadImageAsync(updateDto.Image);
                    category.ImageUrl = imageUrl;
                    category.ImagePublicId = imagePublicId;
                }
                catch (System.Exception ex)
                {
                    return BadRequest($"New image upload failed: {ex.Message}");
                }

                // Delete old image from Cloudinary in background (or await it, let's await it to ensure consistency)
                if (!string.IsNullOrEmpty(oldPublicId))
                {
                    try
                    {
                        await _cloudinaryService.DeleteImageAsync(oldPublicId);
                    }
                    catch
                    {
                        // Log or ignore failure so update doesn't completely fail, but let's allow it to complete
                    }
                }
            }

            _context.Entry(category).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/categories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound($"Category with ID {id} not found.");
            }

            // Delete image from Cloudinary
            if (!string.IsNullOrEmpty(category.ImagePublicId))
            {
                try
                {
                    await _cloudinaryService.DeleteImageAsync(category.ImagePublicId);
                }
                catch (System.Exception ex)
                {
                    // If Cloudinary deletion fails, we can log it but let's still allow database deletion
                    // Alternatively, we could fail if required, but letting it delete is standard unless it's critical.
                }
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}
