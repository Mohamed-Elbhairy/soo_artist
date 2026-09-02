using System;
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
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;

        public ProductsController(AppDbContext context, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 12)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 12;

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .AsQueryable();

            int totalCount = await query.CountAsync();

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var productDtos = products.Select(p => new ProductDto
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

            var result = new PagedResult<ProductDto>(productDtos, totalCount, page, pageSize);

            return Ok(result);
        }

        // GET: api/products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound($"Product with ID {id} not found.");
            }

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? string.Empty,
                CreatedAt = product.CreatedAt,
                Images = product.Images.Select(img => new ProductImageDto
                {
                    Id = img.Id,
                    Url = img.Url
                }).ToList()
            };

            return Ok(productDto);
        }

        // POST: api/products
        [HttpPost]
        public async Task<ActionResult<ProductDto>> CreateProduct([FromForm] CreateProductDto createDto)
        {
            // Verify Category exists
            var category = await _context.Categories.FindAsync(createDto.CategoryId);
            if (category == null)
            {
                return BadRequest($"Invalid CategoryId: {createDto.CategoryId} does not exist.");
            }

            if (createDto.Images == null || createDto.Images.Count == 0)
            {
                return BadRequest("At least one product image is required.");
            }

            var product = new Product
            {
                Name = createDto.Name,
                Description = createDto.Description,
                Price = createDto.Price,
                CategoryId = createDto.CategoryId,
                CreatedAt = DateTime.UtcNow
            };

            // Upload images to Cloudinary
            foreach (var file in createDto.Images)
            {
                if (file.Length > 0)
                {
                    try
                    {
                        var (url, publicId) = await _cloudinaryService.UploadImageAsync(file);
                        product.Images.Add(new ProductImage
                        {
                            Url = url,
                            PublicId = publicId
                        });
                    }
                    catch (Exception ex)
                    {
                        // If one fails, we should clean up previously uploaded images (best practice)
                        foreach (var uploadedImage in product.Images)
                        {
                            await _cloudinaryService.DeleteImageAsync(uploadedImage.PublicId);
                        }
                        return BadRequest($"Failed to upload one of the images: {ex.Message}");
                    }
                }
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                CategoryId = product.CategoryId,
                CategoryName = category.Name,
                CreatedAt = product.CreatedAt,
                Images = product.Images.Select(img => new ProductImageDto
                {
                    Id = img.Id,
                    Url = img.Url
                }).ToList()
            };

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, productDto);
        }

        // PUT: api/products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] UpdateProductDto updateDto)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound($"Product with ID {id} not found.");
            }

            // Verify Category exists
            var category = await _context.Categories.FindAsync(updateDto.CategoryId);
            if (category == null)
            {
                return BadRequest($"Invalid CategoryId: {updateDto.CategoryId} does not exist.");
            }

            product.Name = updateDto.Name;
            product.Description = updateDto.Description;
            product.Price = updateDto.Price;
            product.CategoryId = updateDto.CategoryId;

            // Handle image deletions
            if (updateDto.DeleteImageIds != null && updateDto.DeleteImageIds.Count > 0)
            {
                var imagesToDelete = product.Images
                    .Where(img => updateDto.DeleteImageIds.Contains(img.Id))
                    .ToList();

                foreach (var img in imagesToDelete)
                {
                    // Delete from Cloudinary
                    try
                    {
                        await _cloudinaryService.DeleteImageAsync(img.PublicId);
                    }
                    catch
                    {
                        // Ignore or log error, proceed with DB deletion
                    }

                    // Remove from EF core tracking (cascade delete takes care of DB)
                    product.Images.Remove(img);
                }
            }

            // Handle uploading new images
            if (updateDto.NewImages != null && updateDto.NewImages.Count > 0)
            {
                foreach (var file in updateDto.NewImages)
                {
                    if (file.Length > 0)
                    {
                        try
                        {
                            var (url, publicId) = await _cloudinaryService.UploadImageAsync(file);
                            product.Images.Add(new ProductImage
                            {
                                Url = url,
                                PublicId = publicId
                            });
                        }
                        catch (Exception ex)
                        {
                            return BadRequest($"Failed to upload a new image: {ex.Message}");
                        }
                    }
                }
            }

            // A product must have at least one image remaining
            if (product.Images.Count == 0)
            {
                return BadRequest("A product must keep at least one image.");
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound($"Product with ID {id} not found.");
            }

            // Delete all images from Cloudinary before removing product from DB
            foreach (var img in product.Images)
            {
                if (!string.IsNullOrEmpty(img.PublicId))
                {
                    try
                    {
                        await _cloudinaryService.DeleteImageAsync(img.PublicId);
                    }
                    catch
                    {
                        // Log or ignore
                    }
                }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
