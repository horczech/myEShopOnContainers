//ToDo Next: setup Program.cs, Startup.cs, and create docker with runnin SQL DB

using Catalog.API;
using Catalog.API.Infrastructure;
using Catalog.API.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.eShopOnContainers.Services.Catalog.API.IntegrationEvents;

var builder = WebApplication.CreateBuilder(args);


// Add services to the dependency container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<ICatalogIntegrationEventService, CatalogIntegrationEventService>();
builder.Services.Configure<CatalogSettings>(builder.Configuration.GetSection("CatalogSettings"));

//ToDo M: figure out wtf is it
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder
            .SetIsOriginAllowed((host) => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

//database setup
var connectionString = builder.Configuration.GetConnectionString(nameof(CatalogContext));
builder.Services.AddDbContextFactory<CatalogContext>(optionsBuilder =>
{
    optionsBuilder.UseSqlServer(connectionString);
    
}, ServiceLifetime.Scoped);

// builder.Services.AddGrpc();

var app = builder.Build();


//Middleware
//ToDo: uncomment when adding grpc
// // Configure the HTTP request pipeline.
// app.MapGrpcService<GreeterService>();
// app.MapGet("/",
//     () =>
//         "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("CorsPolicy");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();