using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Soo_artist.DTOs
{
    public class CreateCategoryDto
    {
        [Required(ErrorMessage = "Category name is required.")]
        [MaxLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category image is required.")]
        public IFormFile Image { get; set; } = null!;
    }
}
