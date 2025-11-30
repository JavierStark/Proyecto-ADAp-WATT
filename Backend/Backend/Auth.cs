namespace Backend;

static class Auth
{
    public static async Task<IResult> RegisterUser(RegisterDto dto, Supabase.Client client)
    {
        if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
        {
            return Results.BadRequest(new { error = "El email y la contraseña son obligatorios." });
        }

        try
        {
            var session = await client.Auth.SignUp(dto.Email, dto.Password);

            if (session?.User == null)
            {
                return Results.BadRequest(new { error = "No se pudo registrar el usuario. Inténtalo de nuevo." });
            }

            return Results.Ok(new
            {
                status = "success",
                message = "Usuario creado correctamente. ¡Revisa tu correo para confirmar la cuenta!",
                userId = session.User.Id
            });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    public static async Task<IResult> LoginUser(LoginDto dto, Supabase.Client client)
    {
        if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
        {
            return Results.BadRequest(new { error = "El email y la contraseña son obligatorios." });
        }

        try
        {
            var session = await client.Auth.SignIn(dto.Email, dto.Password);

            if (session?.AccessToken == null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                status = "success",
                message = "Login correcto",

                token = session.AccessToken,
                refreshToken = session.RefreshToken,
                user = new
                {
                    id = session.User?.Id,
                    email = session.User?.Email
                }
            });
        }
        catch (Exception)
        {
            return Results.BadRequest(new { error = "Credenciales inválidas (Usuario o contraseña incorrectos)." });
        }
    }

    public static async Task<IResult> LogoutUser(Supabase.Client client)
    {
        try
        {
            await client.Auth.SignOut();

            return Results.Ok(new { message = "Has cerrado sesión correctamente." });
        }
        catch (Exception)
        {
            return Results.Ok(new { message = "Sesión cerrada." });
        }
    }

    public static async Task<IResult> RefreshToken(RefreshTokenDto dto, Supabase.Client client)
    {
        if (string.IsNullOrEmpty(dto.AccessToken) || string.IsNullOrEmpty(dto.RefreshToken))
        {
            return Results.BadRequest(new { error = "Se requieren el AccessToken y el RefreshToken antiguos." });
        }

        try
        {
            await client.Auth.SetSession(dto.AccessToken, dto.RefreshToken);

            // Pedimos a Supabase que nos renueve la sesión
            // Supabase verifica si el RefreshToken es válido y no ha caducado.
            var session = await client.Auth.RefreshSession();

            if (session?.AccessToken == null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                status = "success",
                message = "Token renovado correctamente",
                token = session.AccessToken,
                refreshToken = session.RefreshToken
            });
        }
        catch (Exception)
        {
            // Si el refresh token ya caducó o fue revocado (logout)
            return Results.Unauthorized();
        }
    }
    
    public record RegisterDto(string Email, string Password);

    public record LoginDto(string Email, string Password);

    public record RefreshTokenDto(string AccessToken, string RefreshToken);
}