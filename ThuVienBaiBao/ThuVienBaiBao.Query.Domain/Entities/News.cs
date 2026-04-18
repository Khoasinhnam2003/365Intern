namespace ThuVienBaiBao.Query.Domain.Entities;

public sealed class News
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<MenuNews> MenuNews { get; set; } = new List<MenuNews>();
}

