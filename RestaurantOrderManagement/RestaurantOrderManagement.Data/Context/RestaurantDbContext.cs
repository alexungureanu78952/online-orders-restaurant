using Microsoft.EntityFrameworkCore;

namespace RestaurantOrderManagement.Data.Context
{
    public class RestaurantDbContext : DbContext
    {
        public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options) : base(options)
        {
        }

        // DbSets added during implementation
        // public DbSet<Product> Products { get; set; }
    }
}
