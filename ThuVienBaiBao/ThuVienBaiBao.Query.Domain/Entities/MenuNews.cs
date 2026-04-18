namespace ThuVienBaiBao.Query.Domain.Entities;

public sealed class MenuNews
{
    public int MenuId { get; set; }

    public Menu Menu { get; set; } = null!;

    public int NewsId { get; set; }

    public News News { get; set; } = null!;
}

