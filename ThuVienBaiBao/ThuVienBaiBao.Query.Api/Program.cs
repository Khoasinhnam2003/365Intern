using ThuVienBaiBao.Query.Application;
using ThuVienBaiBao.Query.Persistence;
using ThuVienBaiBao.Query.Presentation;
using Microsoft.EntityFrameworkCore;
using ThuVienBaiBao.Query.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddGrpc();
builder.Services.AddHostedService<RabbitMqReadModelConsumerHostedService>();
builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(AssemblyReference).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("OpenCors", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var readModelStore = scope.ServiceProvider.GetRequiredService<MongoReadModelStore>();
    dbContext.Database.Migrate();
    DbSeeder.Seed(dbContext);
    await readModelStore.SeedFromSqlAsync(dbContext);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("OpenCors");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapGrpcService<ReadModelGrpcService>();
app.Run();

