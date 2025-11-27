var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var supabaseSettings = builder.Configuration.GetSection("Supabase").Get<SupabaseSettings>();
if (supabaseSettings == null || string.IsNullOrEmpty(supabaseSettings.Url) || string.IsNullOrEmpty(supabaseSettings.Key))
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

var app = builder.Build();

// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapGet("/", () => "CUDECA API");

app.MapGet("/test/supabase", TestSupabase);

var auth = app.MapGroup("/auth");
auth.MapPost("/register", RegisterUser);
auth.MapPost("/login", LoginUser);
auth.MapPost("/logout", LogoutUser);
auth.MapPost("/refresh", RefreshToken);


var users = app.MapGroup("/users/me");
users.MapGet("", GetMyProfile);
users.MapPut("", UpdateMyProfile);
users.MapPatch("", PartialUpdateProfile);

users.MapGet("/tickets", GetMyTickets);
users.MapGet("/tickets/{ticketId}", GetMyTicketById);

users.MapGet("/donations", GetMyDonations);
users.MapGet("/donations/summary", GetMyDonationSummary);


var events = app.MapGroup("/events");
events.MapGet("", ListEvents);
events.MapGet("/{eventId}", GetEvent);


var tickets = app.MapGroup("/tickets");

tickets.MapPost("/purchase/start", StartPurchase);
tickets.MapPost("/purchase/confirm", ConfirmPurchase);


var donations = app.MapGroup("/donations");

donations.MapPost("", CreateDonation);
donations.MapGet("/{donationId}/certificate", GetDonationCertificate);


var payments = app.MapGroup("/payments");

payments.MapGet("/methods", GetPaymentMethods);


var discounts = app.MapGroup("/discounts");

discounts.MapPost("/validate", ValidateDiscount);


var admin = app.MapGroup("/admin/events");

admin.MapGet("", AdminListEvents);
admin.MapPost("", AdminCreateEvent);
admin.MapPut("/{eventId}", AdminUpdateEvent);
admin.MapDelete("/{eventId}", AdminDeleteEvent);

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

async Task<IResult> RegisterUser(RegisterDto dto, Supabase.Client client)
{
    // Comprobamos que los datos no sean nulos
    if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
    {
        return Results.BadRequest(new { error = "El email y la contraseña son obligatorios." });
    }

    try
    {
        // Llamamos a Supabase para crear el usuario
        var session = await client.Auth.SignUp(dto.Email, dto.Password);

        // Verificamos si Supabase respondió con un usuario
        // Nota: A veces devuelve sesión nula si requiere confirmación de email, pero User no debería ser nulo.
        if (session?.User == null)
        {
            return Results.BadRequest(new { error = "No se pudo registrar el usuario. Inténtalo de nuevo." });
        }

        // Devolvemos un 200 OK
        return Results.Ok(new 
        { 
            status = "success", 
            message = "Usuario creado correctamente. ¡Revisa tu correo para confirmar la cuenta!",
            userId = session.User.Id
        });
    }
    catch (Exception ex)
    {
        // Si algo falla devolvemos el error
        return Results.BadRequest(new { error = ex.Message });
    }
}

async Task<IResult> LoginUser(LoginDto dto, Supabase.Client client)
{
    // Comprobamos que los datos no sean nulos
    if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
    {
        return Results.BadRequest(new { error = "El email y la contraseña son obligatorios." });
    }

    try
    {
        // Llamamos a Supabase para preguntar a Supabase si los datos son correctos
        var session = await client.Auth.SignIn(dto.Email, dto.Password);
        
        // Supabase devuelve una "Session" que contiene el Token de acceso.
        if (session == null || session.AccessToken == null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new 
        { 
            status = "success",
            message = "Login correcto",
            
            // Llave para futuras peticiones
            token = session.AccessToken, 
            refreshToken = session.RefreshToken,
            user = new 
            { 
                id = session.User.Id,       // Esta comprobado que no sea nula
                email = session.User.Email 
            }
        });
    }
    catch (Exception ex)
    {
        // Devolvemos un 400
        return Results.BadRequest(new { error = "Credenciales inválidas (Usuario o contraseña incorrectos)." });
    }
}

//IResult LogoutUser() => Results.Ok();
async Task<IResult> LogoutUser(Supabase.Client client)
{
    try 
    {
        // Avisamos a Supabase que esta sesión ya no es válida.
        // Esto invalida el "Refresh Token"
        await client.Auth.SignOut();

        return Results.Ok(new { message = "Has cerrado sesión correctamente." });
    }
    catch (Exception ex)
    {
        // Aunque falle el usuario ya ha cerrado sesión
        return Results.Ok(new { message = "Sesión cerrada." });
    }
}

IResult RefreshToken() => Results.Ok();

IResult GetMyProfile() => Results.Ok();
IResult UpdateMyProfile(UserUpdateDto dto) => Results.Ok();
IResult PartialUpdateProfile(UserUpdatePartialDto dto) => Results.Ok();

IResult GetMyTickets() => Results.Ok();
IResult GetMyTicketById(int ticketId) => Results.Ok();


IResult GetMyDonations() => Results.Ok();
IResult GetMyDonationSummary() => Results.Ok();

IResult ListEvents(string? query) => Results.Ok();
IResult GetEvent(int eventId) => Results.Ok();

IResult StartPurchase(PurchaseStartDto dto) => Results.Ok();
IResult ConfirmPurchase(PurchaseConfirmDto dto) => Results.Ok();

IResult CreateDonation(DonationDto dto) => Results.Ok();
IResult GetDonationCertificate(int donationId) => Results.File("dummy.pdf");

IResult GetPaymentMethods() => Results.Ok();

IResult ValidateDiscount(DiscountCheckDto dto) => Results.Ok();

IResult AdminListEvents() => Results.Ok();
IResult AdminCreateEvent(EventAdminCreateDto dto) => Results.Ok();
IResult AdminUpdateEvent(int eventId, EventAdminUpdateDto dto) => Results.Ok();
IResult AdminDeleteEvent(int eventId) => Results.Ok();


record RegisterDto(string Email, string Password);
record LoginDto(string Email, string Password);
record UserUpdateDto(string Name, string Email, string Phone);
record UserUpdatePartialDto(string? Name, string? Phone);

record PurchaseStartDto(
    int EventId,
    int Quantity,
    bool IsCompany,
    string BillingAddress,
    string? DiscountCode);

record PurchaseConfirmDto(
    string PaymentMethod,
    string PaymentToken);

record DonationDto(decimal Amount);
record DiscountCheckDto(string Code);

record EventAdminCreateDto(string Title, string Description, DateTime Date);
record EventAdminUpdateDto(string? Title, string? Description, DateTime? Date);

record SupabaseSettings(string Url, string Key);

