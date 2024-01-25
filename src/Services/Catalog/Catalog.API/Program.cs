//ToDo Next: setup Program.cs, Startup.cs, and create docker with runnin SQL DB
var builder = WebApplication.CreateBuilder(args);


// Add services to the dependency container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();