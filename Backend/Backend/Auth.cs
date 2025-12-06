using Backend.Models;
using Supabase.Gotrue;
using static Supabase.Postgrest.Constants;

namespace Backend;

public static class Auth
{
    /// <summary>
    /// Sign up a new user with email and password (for testing purposes)
    /// </summary>
    public static async Task<IResult> SignUp(SignUpRequest request, Supabase.Client client)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return Results.BadRequest(new { error = "Email y contraseña son requeridos" });
            }

            if (string.IsNullOrEmpty(request.Nombre) || string.IsNullOrEmpty(request.Apellidos))
            {
                return Results.BadRequest(new { error = "Nombre y apellidos son requeridos" });
            }

            // Sign up with Supabase Auth
            var signUpOptions = new SignUpOptions
            {
                Data = new Dictionary<string, object>
                {
                    { "nombre", request.Nombre },
                    { "apellidos", request.Apellidos },
                    { "dni", request.Dni ?? "" },
                    { "telefono", request.Telefono ?? "" }
                }
            };

            var authResponse = await client.Auth.SignUp(request.Email, request.Password, signUpOptions);

            if (authResponse?.User == null)
            {
                return Results.BadRequest(new { error = "Error al crear usuario en Supabase Auth" });
            }

            return Results.Ok(new
            {
                status = "success",
                message = "Usuario registrado correctamente. Revisa tu email para confirmar tu cuenta.",
                user = new
                {
                    id = authResponse.User.Id,
                    email = authResponse.User.Email,
                    emailConfirmedAt = authResponse.User.ConfirmedAt
                },
                session = authResponse.AccessToken != null ? new
                {
                    access_token = authResponse.AccessToken,
                    refresh_token = authResponse.RefreshToken,
                    expires_in = authResponse.ExpiresIn
                } : null
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Error en el registro",
                detail: ex.Message,
                statusCode: 500
            );
        }
    }

    /// <summary>
    /// Sign in with email and password (for testing purposes)
    /// </summary>
    public static async Task<IResult> SignIn(SignInRequest request, Supabase.Client client)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return Results.BadRequest(new { error = "Email y contraseña son requeridos" });
            }

            // Sign in with Supabase Auth
            var authResponse = await client.Auth.SignIn(request.Email, request.Password);

            if (authResponse?.User == null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                status = "success",
                message = "Inicio de sesión exitoso",
                user = new
                {
                    id = authResponse.User.Id,
                    email = authResponse.User.Email,
                    emailConfirmedAt = authResponse.User.ConfirmedAt
                },
                session = new
                {
                    access_token = authResponse.AccessToken,
                    refresh_token = authResponse.RefreshToken,
                    expires_in = authResponse.ExpiresIn,
                    token_type = "Bearer"
                }
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Error en el inicio de sesión",
                detail: ex.Message,
                statusCode: 500
            );
        }
    }

    /// <summary>
    /// Sign out the current user (for testing purposes)
    /// </summary>
    public static async Task<IResult> SignOut(HttpContext httpContext, Supabase.Client client)
    {
        try
        {
            var userAuth = httpContext.Items["user_auth"] as string;
            
            if (string.IsNullOrEmpty(userAuth))
            {
                return Results.BadRequest(new { error = "No hay sesión activa" });
            }

            await client.Auth.SignOut();

            return Results.Ok(new
            {
                status = "success",
                message = "Sesión cerrada correctamente"
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Error al cerrar sesión",
                detail: ex.Message,
                statusCode: 500
            );
        }
    }
    
    public static async Task<IResult> IsUserAdmin(Supabase.Client client, HttpContext context)
    {
        try
        {
            // Auth
            var userId = (string)context.Items["user_id"]!;
            
            var response = await client
                .From<Admin>()
                .Filter("fk_usuario", Operator.Equals, userId)
                .Get();
            
            // Si la lista tiene elementos (> 0) es administrador.
            bool esAdmin = response.Models.Count > 0;

            return Results.Ok(new { isAdmin = esAdmin });
        }
        catch (Exception ex)
        {
            return Results.Problem("Error comprobando rol: " + ex.Message);
        }
    }
}

public record SignUpRequest(
    string Email,
    string Password,
    string Nombre,
    string Apellidos,
    string? Dni,
    string? Telefono
);

public record SignInRequest(
    string Email,
    string Password
);

