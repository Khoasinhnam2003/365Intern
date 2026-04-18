using Microsoft.EntityFrameworkCore;
using ThuVienBaiBao.Query.Application.Common.Interfaces;
using ThuVienBaiBao.Query.Domain.Entities;

namespace ThuVienBaiBao.Query.Persistence;

public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Menu> Menus => Set<Menu>();

    public DbSet<News> News => Set<News>();

    public DbSet<MenuNews> MenuNews => Set<MenuNews>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.ToTable("Menus");
            entity.HasKey(menu => menu.Id);
            entity.Property(menu => menu.Name).HasMaxLength(200).IsRequired();
            entity.Property(menu => menu.Slug).HasMaxLength(250).IsRequired();
            entity.Property(menu => menu.Description).HasMaxLength(1000);
            entity.HasIndex(menu => menu.Slug).IsUnique();
        });

        modelBuilder.Entity<News>(entity =>
        {
            entity.ToTable("News");
            entity.HasKey(news => news.Id);
            entity.Property(news => news.Title).HasMaxLength(250).IsRequired();
            entity.Property(news => news.Slug).HasMaxLength(300).IsRequired();
            entity.Property(news => news.Content).IsRequired();
            entity.HasIndex(news => news.Slug).IsUnique();
        });

        modelBuilder.Entity<MenuNews>(entity =>
        {
            entity.ToTable("MenuNews");
            entity.HasKey(link => new { link.MenuId, link.NewsId });

            entity.HasOne(link => link.Menu)
                .WithMany(menu => menu.MenuNews)
                .HasForeignKey(link => link.MenuId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(link => link.News)
                .WithMany(news => news.MenuNews)
                .HasForeignKey(link => link.NewsId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

