using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Soo_artist.DTOs
{
    public class UpdateCategoryDto
    {
        [Required(ErrorMessage = "Category name is required.")]
        [MaxLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        public IFormFile? Image { get; set; }
    }
}
