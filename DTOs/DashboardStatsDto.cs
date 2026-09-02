using System.Collections.Generic;

namespace Soo_artist.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalCategories { get; set; }
        public int TotalProducts { get; set; }
        public List<ProductDto> LatestProducts { get; set; } = new List<ProductDto>();
    }
}
