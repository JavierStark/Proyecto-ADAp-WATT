using Backend;
using System.Text.Json;
using Backend.Filters;
using Backend.Services;
using Swashbuckle.AspNetCore.SwaggerGen;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON options for DateTime handling
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    SwaggerAuthSetup(c);
    c.SchemaFilter<SwaggerEmptyStringDefaultFilter>();
});
builder.Services.AddCors();
builder.Services.AddHttpClient(); // For JWKS fetching in SupabaseAuthFilter

var supabaseSettings = builder.Configuration.GetSection("Supabase").Get<SupabaseSettings>();

if (supabaseSettings == null || string.IsNullOrEmpty(supabaseSettings.Url) ||
    string.IsNullOrEmpty(supabaseSettings.Key))
{
    throw new InvalidOperationException(
        "Supabase configuration is missing. Please configure Supabase:Url and Supabase:Key in user secrets:\n" +
        "  dotnet user-secrets set \"Supabase:Url\" \"https://your-project.supabase.co\"\n" +
        "  dotnet user-secrets set \"Supabase:Key\" \"your-anon-key\"");
}

builder.Services.AddSingleton<Supabase.Client>(_ =>
{
    var options = new Supabase.SupabaseOptions
    {
        AutoRefreshToken = true,
        AutoConnectRealtime = true
    };

    var client = new Supabase.Client(supabaseSettings.Url, supabaseSettings.Key, options);
    client.InitializeAsync().Wait();
    return client;
});

// Payment logic
// if (builder.Environment.IsDevelopment())
// {
//     builder.Services.AddScoped<IPaymentService, SimulatedPaymentService>();
// }
// else
// {
//     builder.Services.AddScoped<IPaymentService, StripePaymentService>();
// }

builder.Services.AddScoped<IPaymentService, SimulatedPaymentService>();

builder.Services.AddScoped<IEmailService, MailGunService>();

var app = builder.Build();

app.UseCors(policy =>
    policy.WithOrigins(
            "http://localhost:4200",
            "https://cudeca-watt.es",
            "https://www.cudeca-watt.es"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()
);

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapGet("/", () => "CUDECA API");
app.MapGet("/test/supabase", TestSupabase);

app.MapAuthEndpoints()
    .MapUserEndpoints()
    .MapPartnerEndpoints()
    .MapEventEndpoints()
    .MapTicketEndpoints()
    .MapDonationEndpoints()
    .MapPaymentEndpoints()
    .MapDiscountEndpoints()
    .MapAdminEndpoints();

app.Run();
return;

IResult TestSupabase(Supabase.Client supabase)
{
    try
    {
        var maskedUrl = supabaseSettings.Url.Length > 20
            ? supabaseSettings.Url[..20] + "..."
            : supabaseSettings.Url;

        return Results.Ok(new
        {
            status = "success",
            message = "Supabase client initialized successfully",
            connection = new
            {
                url = maskedUrl,
                initialized = true,
                timestamp = DateTime.UtcNow
            }
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Supabase Connection Error",
            detail: ex.Message,
            statusCode: 500
        );
    }
}

void SwaggerAuthSetup(SwaggerGenOptions swaggerGenOptions)
{
    swaggerGenOptions.SwaggerDoc("v1",
        new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Cudeca API", Version = "v1" });

    // Definimos el esquema de seguridad "Bearer"
    swaggerGenOptions.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description =
            "Autenticación JWT usando el esquema Bearer.\r\n\r\nEscribe 'Bearer' [espacio] y tu token.\r\n\r\nEjemplo: \"Bearer eyJhbGc...\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    swaggerGenOptions.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            },
            new List<string>()
        }
    });
}

record SupabaseSettings(string Url, string Key);