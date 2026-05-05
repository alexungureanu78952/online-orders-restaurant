using Microsoft.EntityFrameworkCore;
using RestaurantOrderManagement.Data.Models;

namespace RestaurantOrderManagement.Data.Context
{
    public class RestaurantDbContext : DbContext
    {
        public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Allergen> Allergens { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductAllergen> ProductAllergens { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<MenuProduct> MenuProducts { get; set; }
        public DbSet<MenuImage> MenuImages { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Configuration> Configurations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Category
            modelBuilder.Entity<Category>()
                .HasKey(c => c.CategoryId);
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            // Configure Allergen
            modelBuilder.Entity<Allergen>()
                .HasKey(a => a.AllergenId);
            modelBuilder.Entity<Allergen>()
                .HasIndex(a => a.Name)
                .IsUnique();

            // Configure Product
            modelBuilder.Entity<Product>()
                .HasKey(p => p.ProductId);
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Product>()
                .HasIndex(p => new { p.Name, p.CategoryId })
                .IsUnique();

            // Configure ProductAllergen
            modelBuilder.Entity<ProductAllergen>()
                .HasKey(pa => pa.ProductAllergenId);
            modelBuilder.Entity<ProductAllergen>()
                .HasOne(pa => pa.Product)
                .WithMany(p => p.ProductAllergens)
                .HasForeignKey(pa => pa.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ProductAllergen>()
                .HasOne(pa => pa.Allergen)
                .WithMany(a => a.ProductAllergens)
                .HasForeignKey(pa => pa.AllergenId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProductAllergen>()
                .HasIndex(pa => new { pa.ProductId, pa.AllergenId })
                .IsUnique();

            // Configure ProductImage
            modelBuilder.Entity<ProductImage>()
                .HasKey(pi => pi.ProductImageId);
            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.ProductImages)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Menu
            modelBuilder.Entity<Menu>()
                .HasKey(m => m.MenuId);
            modelBuilder.Entity<Menu>()
                .HasOne(m => m.Category)
                .WithMany(c => c.Menus)
                .HasForeignKey(m => m.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure MenuProduct
            modelBuilder.Entity<MenuProduct>()
                .HasKey(mp => mp.MenuProductId);
            modelBuilder.Entity<MenuProduct>()
                .HasOne(mp => mp.Menu)
                .WithMany(m => m.MenuProducts)
                .HasForeignKey(mp => mp.MenuId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MenuProduct>()
                .HasOne(mp => mp.Product)
                .WithMany(p => p.MenuProducts)
                .HasForeignKey(mp => mp.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MenuProduct>()
                .HasIndex(mp => new { mp.MenuId, mp.ProductId })
                .IsUnique();

            // Configure MenuImage
            modelBuilder.Entity<MenuImage>()
                .HasKey(mi => mi.MenuImageId);
            modelBuilder.Entity<MenuImage>()
                .HasOne(mi => mi.Menu)
                .WithMany(m => m.MenuImages)
                .HasForeignKey(mi => mi.MenuId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure User
            modelBuilder.Entity<User>()
                .HasKey(u => u.UserId);
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Configure Order
            modelBuilder.Entity<Order>()
                .HasKey(o => o.OrderId);
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderCode)
                .IsUnique();

            // Configure OrderItem
            modelBuilder.Entity<OrderItem>()
                .HasKey(oi => oi.OrderItemId);
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Configuration
            modelBuilder.Entity<Configuration>()
                .HasKey(c => c.ConfigId);
        }
    }
}
