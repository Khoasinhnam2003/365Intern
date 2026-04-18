namespace ThuVienBaiBao.Query.Persistence;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    public string DatabaseName { get; set; } = "ThuVienBaiBaoReadModel";

    public string MenusCollectionName { get; set; } = "menus";

    public string NewsCollectionName { get; set; } = "news";
}