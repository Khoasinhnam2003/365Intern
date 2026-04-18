using ThuVienBaiBao.Command.Domain.Entities;

namespace ThuVienBaiBao.Command.Persistence;

public static class DbSeeder
{
    public static void Seed(ApplicationDbContext context)
    {
        if (context.Menus.Any() || context.News.Any())
        {
            return;
        }

        var menus = new[]
        {
            new Menu { Name = "Tin tá»©c", Slug = "tin-tuc", Description = "CÃ¡c bÃ i viáº¿t cáº­p nháº­t" },
            new Menu { Name = "Sá»± kiá»‡n", Slug = "su-kien", Description = "BÃ i viáº¿t vá» sá»± kiá»‡n" },
            new Menu { Name = "ThÃ´ng bÃ¡o", Slug = "thong-bao", Description = "ThÃ´ng bÃ¡o tá»« há»‡ thá»‘ng" }
        };

        var news = new[]
        {
            new News
            {
                Title = "Khai giáº£ng tuáº§n thá»±c táº­p",
                Slug = "khai-giang-tuan-thuc-tap",
                Content = "Ná»™i dung bÃ i viáº¿t máº«u cho project demo.",
                IsPublished = true,
                PublishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new News
            {
                Title = "HÆ°á»›ng dáº«n lÃ m bÃ¡o cÃ¡o",
                Slug = "huong-dan-lam-bao-cao",
                Content = "BÃ i viáº¿t máº«u mÃ´ táº£ quy trÃ¬nh lÃ m bÃ¡o cÃ¡o.",
                IsPublished = true,
                PublishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new News
            {
                Title = "Lá»‹ch review tuáº§n 1",
                Slug = "lich-review-tuan-1",
                Content = "ThÃ´ng bÃ¡o lá»‹ch review cá»§a tuáº§n Ä‘áº§u tiÃªn.",
                IsPublished = false,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Menus.AddRange(menus);
        context.News.AddRange(news);
        context.SaveChanges();

        context.MenuNews.AddRange(
            new MenuNews { MenuId = menus[0].Id, NewsId = news[0].Id },
            new MenuNews { MenuId = menus[0].Id, NewsId = news[1].Id },
            new MenuNews { MenuId = menus[1].Id, NewsId = news[0].Id },
            new MenuNews { MenuId = menus[1].Id, NewsId = news[2].Id },
            new MenuNews { MenuId = menus[2].Id, NewsId = news[2].Id });

        context.SaveChanges();
    }
}
