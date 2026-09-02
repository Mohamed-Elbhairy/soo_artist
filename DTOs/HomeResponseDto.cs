using System.Collections.Generic;

namespace Soo_artist.DTOs
{
    public class HomeResponseDto
    {
        public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
        public List<ProductDto> LatestProducts { get; set; } = new List<ProductDto>();
    }
}
