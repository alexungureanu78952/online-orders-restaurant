using Microsoft.EntityFrameworkCore;
using RestaurantOrderManagement.Data.Models;

namespace RestaurantOrderManagement.Data.Context
{
    public class RestaurantDbContext : DbContext
    {
        public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Allergen> Allergens => Set<Allergen>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductAllergen> ProductAllergens => Set<ProductAllergen>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<Menu> Menus => Set<Menu>();
        public DbSet<MenuProduct> MenuProducts => Set<MenuProduct>();
        public DbSet<MenuImage> MenuImages => Set<MenuImage>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Configuration> Configurations => Set<Configuration>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            
            modelBuilder.Entity<Category>()
                .ToTable("Category")
                .HasKey(c => c.CategoryId);
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            
            modelBuilder.Entity<Allergen>()
                .ToTable("Allergen")
                .HasKey(a => a.AllergenId);
            modelBuilder.Entity<Allergen>()
                .HasIndex(a => a.Name)
                .IsUnique();

            
            modelBuilder.Entity<Product>()
                .ToTable("Product")
                .HasKey(p => p.ProductId);
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Product>()
                .HasIndex(p => new { p.Name, p.CategoryId })
                .IsUnique();

            
            modelBuilder.Entity<ProductAllergen>()
                .ToTable("ProductAllergen")
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

            
            modelBuilder.Entity<ProductImage>()
                .ToTable("ProductImage")
                .HasKey(pi => pi.ProductImageId);
            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.ProductImages)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            
            modelBuilder.Entity<Menu>()
                .ToTable("Menu")
                .HasKey(m => m.MenuId);
            modelBuilder.Entity<Menu>()
                .HasOne(m => m.Category)
                .WithMany(c => c.Menus)
                .HasForeignKey(m => m.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            
            modelBuilder.Entity<MenuProduct>()
                .ToTable("MenuProduct")
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

            
            modelBuilder.Entity<MenuImage>()
                .ToTable("MenuImage")
                .HasKey(mi => mi.MenuImageId);
            modelBuilder.Entity<MenuImage>()
                .HasOne(mi => mi.Menu)
                .WithMany(m => m.MenuImages)
                .HasForeignKey(mi => mi.MenuId)
                .OnDelete(DeleteBehavior.Cascade);

            
            modelBuilder.Entity<User>()
                .ToTable("User")
                .HasKey(u => u.UserId);
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            
            modelBuilder.Entity<Order>()
                .ToTable("Order")
                .HasKey(o => o.OrderId);
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderCode)
                .IsUnique();

           
            modelBuilder.Entity<OrderItem>()
                .ToTable("OrderItem")
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
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Menu)
                .WithMany(m => m.OrderItems)
                .HasForeignKey(oi => oi.MenuId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            
            modelBuilder.Entity<Configuration>()
                .ToTable("Configuration")
                .HasKey(c => c.ConfigId);
        }
    }
}
