namespace ThuVienBaiBao.Query.Domain.Entities;

public sealed class Menu
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<MenuNews> MenuNews { get; set; } = new List<MenuNews>();
}

