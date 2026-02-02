using Microsoft.EntityFrameworkCore;
using PontelloApp.Models;
using System.Numerics;

namespace PontelloApp.Data
{
    public class PontelloAppContext : DbContext
    {
        public PontelloAppContext(DbContextOptions<PontelloAppContext> options) 
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Variant> Variants { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Prevent Cascade Delete from Category to Product
            modelBuilder.Entity<Category>()
                .HasMany<Product>(d => d.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Category>()
                .HasMany(c => c.SubCategories)
                .WithOne(c => c.ParentCategory)
                .HasForeignKey(c => c.ParentCategoryID)
                .OnDelete(DeleteBehavior.Restrict); 

        }
    }
}
