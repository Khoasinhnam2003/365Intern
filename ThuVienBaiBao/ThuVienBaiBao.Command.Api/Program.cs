using ThuVienBaiBao.Command.Application;
using ThuVienBaiBao.Command.Persistence;
using ThuVienBaiBao.Command.Presentation;
using Microsoft.EntityFrameworkCore;
using ThuVienBaiBao.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddGrpcClient<ReadModel.ReadModelClient>(options =>
{
    var address = builder.Configuration["Grpc:QueryApiAddress"] ?? "https://localhost:7245";
    options.Address = new Uri(address);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});
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
    dbContext.Database.Migrate();
    DbSeeder.Seed(dbContext);
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
app.Run();

