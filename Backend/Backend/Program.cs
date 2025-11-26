using Backend;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/", () => "Hello World!")
    .WithName("GetHelloWorld")
    .WithOpenApi();



app.MapGet("/eventos/{numeroEventosString}", Test.GetEvents)
    .WithName("GetEvents")
    .WithOpenApi();

app.Run();