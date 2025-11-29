// Sirve para obtener información de la cabecera
using Microsoft.AspNetCore.Mvc;

// Sirve para declarar las tablas de supabase
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

// He tenido que cambiar la seguridad para pasar el token de sesion al metodo GetMyProfile
// sin que este quede reflejado en la URL

// Configuración del Candado (Security Definition)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Cudeca API", Version = "v1" });

    // Definimos el esquema de seguridad "Bearer"
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Autenticación JWT usando el esquema Bearer.\r\n\r\nEscribe 'Bearer' [espacio] y tu token.\r\n\r\nEjemplo: \"Bearer eyJhbGc...\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Aplicamos el esquema a todos los endpoints
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
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
});

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

// Registro, inicio, cerrar y refrescar sesión
//=====================================================
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
        // Devolvemos un 400/401
        return Results.BadRequest(new { error = "Credenciales inválidas (Usuario o contraseña incorrectos)." });
    }
}

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

async Task<IResult> RefreshToken(RefreshTokenDto dto, Supabase.Client client)
{
    // Comprobamos que ninguno de los tokens sea nulo
    if (string.IsNullOrEmpty(dto.AccessToken) || string.IsNullOrEmpty(dto.RefreshToken))
    {
        return Results.BadRequest(new { error = "Se requieren el AccessToken y el RefreshToken antiguos." });
    }

    try
    {
        // Cargamos los tokens antiguos en el cliente
        await client.Auth.SetSession(dto.AccessToken, dto.RefreshToken);
        
        // Pedimos a Supabase que nos renueve la sesión
        // Supabase verifica si el RefreshToken es válido y no ha caducado.
        var session = await client.Auth.RefreshSession();

        if (session == null || session.AccessToken == null)
        {
            return Results.Unauthorized();
        }

        // Devolvemos los nuevos tokens
        return Results.Ok(new 
        { 
            status = "success",
            message = "Token renovado correctamente",
            token = session.AccessToken,
            refreshToken = session.RefreshToken
        });
    }
    catch (Exception ex)
    {
        // Si el refresh token ya caducó o fue revocado (logout)
        return Results.Unauthorized();
    }
}

// Extraer y actualizar el perfil
//=====================================================
//IResult GetMyProfile() => Results.Ok();
async Task<IResult> GetMyProfile([FromHeader(Name = "Authorization")] string? authHeader, Supabase.Client client)
{
    // Obtener Token y validar
    if (string.IsNullOrEmpty(authHeader)) return Results.Unauthorized();

    // Limpiar el token (Quitar "Bearer " y comillas si las hubiera)
    string token = authHeader.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
        .Replace("\"", "")
        .Trim();

    if (string.IsNullOrEmpty(token)) return Results.Unauthorized();

    try 
    {
        // Identificar al usuario logueado (UUID)
        await client.Auth.SetSession(token, "token_falso");
        var userAuth = client.Auth.CurrentUser;
        if (userAuth == null) return Results.Unauthorized();

        // CONSULTA 1: Datos Generales (Tabla Usuario)
        // Buscamos por el UUID de Supabase
        var usuarioDb = await client
            .From<Usuario>()
            .Filter("id_auth_supabase", Supabase.Postgrest.Constants.Operator.Equals, userAuth.Id)
            .Single(); // Si falla aquí es que el usuario no existe en tu tabla

        // CONSULTA 2: Datos Específicos (Tabla Cliente)
        // Usamos el ID numérico que acabamos de obtener
        var clienteDb = await client
            .From<Cliente>()
            .Filter("id_cliente", Supabase.Postgrest.Constants.Operator.Equals, usuarioDb.IdUsuario.ToString())
            .Single(); 

        // COMBINAR DATOS
        // Creamos un objeto para el frontend
        var perfilCompleto = new 
        {
            // Datos de identificación
            id_interno = usuarioDb.IdUsuario,
            email = usuarioDb.Email,
            
            // Datos personales (Tabla Usuario)
            dni = usuarioDb.Dni,
            nombre = usuarioDb.Nombre,
            apellidos = usuarioDb.Apellidos,
            telefono = usuarioDb.Telefono,

            // Datos de cliente (Tabla Cliente)
            direccion = clienteDb.Direccion,
            suscrito_newsletter = clienteDb.SuscritoNewsletter,
        };

        return Results.Ok(perfilCompleto);
    }
    catch (Exception ex)
    {
        return Results.Problem("Error obteniendo el perfil completo: " + ex.Message);
    }
}

async Task<IResult> UpdateMyProfile([FromHeader(Name = "Authorization")] string? authHeader, UserUpdateDto dto, Supabase.Client client)
{
    // Obtener Token y validar
    if (string.IsNullOrEmpty(authHeader)) return Results.Unauthorized();

    // Limpiar el token
    string token = authHeader.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
        .Replace("\"", "")
        .Trim();

    if (string.IsNullOrEmpty(token)) return Results.Unauthorized();

    try
    {
        // Identificar al usuario logueado (UUID)
        await client.Auth.SetSession(token, "token_falso");
        var userAuth = client.Auth.CurrentUser;
        if (userAuth == null) return Results.Unauthorized();

        // Obtener el usuario actual de la BD
        var usuarioDb = await client
            .From<Usuario>()
            .Filter("id_auth_supabase", Supabase.Postgrest.Constants.Operator.Equals, userAuth.Id)
            .Single();

        // Preparar un objeto Usuario con los campos a actualizar
        var usuarioToUpdate = new Usuario
        {
            IdUsuario = usuarioDb.IdUsuario,
            IdAuthSupabase = usuarioDb.IdAuthSupabase,
            Email = usuarioDb.Email,
            Nombre = string.IsNullOrEmpty(dto.Nombre) ? usuarioDb.Nombre : dto.Nombre,
            Apellidos = string.IsNullOrEmpty(dto.Apellidos) ? usuarioDb.Apellidos : dto.Apellidos,
            Telefono = string.IsNullOrEmpty(dto.Telefono) ? usuarioDb.Telefono : dto.Telefono,
            Dni = string.IsNullOrEmpty(dto.Dni) ? usuarioDb.Dni : dto.Dni
        };

        // Actualizar la tabla usuario usando el modelo (evita sobrecarga ambigua)
        await client
            .From<Usuario>()
            .Where(x => x.IdUsuario == usuarioDb.IdUsuario)
            .Update(usuarioToUpdate);

        // Nota: cambiar el email en Supabase Auth no se realiza aquí.
        // Si el cliente solicitó cambiar el email, devolver una instrucción
        if (!string.IsNullOrEmpty(dto.Email) && dto.Email != usuarioDb.Email)
        {
            return Results.BadRequest(new { error = "Para cambiar el email debe usar el flujo de verificación de correo (no soportado en este endpoint)." });
        }

        // Obtener los datos actualizados
        var usuarioActualizado = await client
            .From<Usuario>()
            .Filter("id_auth_supabase", Supabase.Postgrest.Constants.Operator.Equals, userAuth.Id)
            .Single();

        var clienteDb = await client
            .From<Cliente>()
            .Filter("id_cliente", Supabase.Postgrest.Constants.Operator.Equals, usuarioActualizado.IdUsuario.ToString())
            .Single();

        var perfilActualizado = new
        {
            id_interno = usuarioActualizado.IdUsuario,
            email = usuarioActualizado.Email,
            dni = usuarioActualizado.Dni,
            nombre = usuarioActualizado.Nombre,
            apellidos = usuarioActualizado.Apellidos,
            telefono = usuarioActualizado.Telefono,
            direccion = clienteDb.Direccion,
            suscrito_newsletter = clienteDb.SuscritoNewsletter,
        };

        return Results.Ok(new { status = "success", message = "Perfil actualizado correctamente", data = perfilActualizado });
    }
    catch (Exception ex)
    {
        return Results.Problem("Error actualizando el perfil: " + ex.Message);
    }
}

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
record RefreshTokenDto(string AccessToken, string RefreshToken);
record UserUpdateDto(string? Nombre, string? Apellidos, string? Email, string? Telefono, string? Dni);
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

// Tablas de la BD
[Table("usuario")]
public class Usuario : BaseModel
{
    // Clave primaria numérica (1, 2, 3...)
    [PrimaryKey("id_usuario")]
    public long IdUsuario { get; set; }

    // El puente con el login (UUID)
    [Column("id_auth_supabase")]
    public string? IdAuthSupabase { get; set; }

    [Column("Email")]
    public string? Email { get; set; }

    [Column("dni")]
    public string? Dni { get; set; }

    [Column("nombre")]
    public string? Nombre { get; set; }

    [Column("apellidos")]
    public string? Apellidos { get; set; }

    [Column("telefono")]
    public string? Telefono { get; set; }
}

[Table("cliente")]
public class Cliente : BaseModel
{
    // Coincide con el ID_usuario
    [PrimaryKey("id_cliente")]
    public long IdCliente { get; set; }

    [Column("direccion")]
    public string? Direccion { get; set; }

    [Column("suscritonewsletter")]
    public bool SuscritoNewsletter { get; set; } // bool normal (true/false)

    [Column("Tipo")]
    public string? Tipo { get; set; } // "Socio" o "Corporativo"
}
