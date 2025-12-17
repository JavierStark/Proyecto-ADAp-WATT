﻿using Backend.Models;

namespace Backend;

static class Profile
{
    public static async Task<IResult> GetMyProfile(HttpContext httpContext, Supabase.Client client)
    {
        try
        {
            var userId = (string)httpContext.Items["user_id"]!;
            var parsed = Guid.Parse(userId);

            var usuario = await client
                .From<Usuario>()
                .Where(u => u.Id == parsed)
                .Single();

            var cliente = await client
                .From<Cliente>()
                .Where(c => c.Id == parsed)
                .Single();

            var perfilCompleto = new UserProfileDto(
                usuario.Id,
                usuario.Email!,
                usuario.Dni,
                usuario.Nombre,
                usuario.Apellidos,
                usuario.Telefono,
                cliente.Calle,
                cliente.Numero,
                cliente.PisoPuerta,
                cliente.CodigoPostal,
                cliente.Ciudad,
                cliente.Provincia,
                cliente.Pais,
                cliente.SuscritoNewsletter
            );

            return Results.Ok(perfilCompleto);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error obteniendo el perfil completo: " + ex.Message);
        }
    }

    public static async Task<IResult> UpdateProfile(ProfileUpdateDto dto, HttpContext httpContext,
        Supabase.Client client)
    {
        try
        {
            var userId = (string)httpContext.Items["user_id"]!;
            var parsed = Guid.Parse(userId);
            
            var usuarioTask = client.From<Usuario>().Where(u => u.Id == parsed).Single();
            var clienteTask = client.From<Cliente>().Where(c => c.Id == parsed).Single();

            await Task.WhenAll(usuarioTask, clienteTask);

            var usuario = usuarioTask.Result;
            var cliente = clienteTask.Result;

            if (usuario == null || cliente == null)
                return Results.NotFound("Usuario o Cliente no encontrados.");

            // Si el DTO es nulo o vacío, no tocamos la propiedad, conservando el valor original
            // Actualizar tabla Usuario
            if (!string.IsNullOrEmpty(dto.Nombre)) usuario.Nombre = dto.Nombre;
            if (!string.IsNullOrEmpty(dto.Apellidos)) usuario.Apellidos = dto.Apellidos;
            if (!string.IsNullOrEmpty(dto.Dni)) usuario.Dni = dto.Dni;
            if (!string.IsNullOrEmpty(dto.Telefono)) usuario.Telefono = dto.Telefono;

            var usuarioResponse = await client.From<Usuario>().Update(usuario);
            var usuarioNuevo = usuarioResponse.Models.First();

            // Actualizar tabla Cliente
            if (!string.IsNullOrEmpty(dto.Calle)) cliente.Calle = dto.Calle;
            if (!string.IsNullOrEmpty(dto.Numero)) cliente.Numero = dto.Numero;
            if (!string.IsNullOrEmpty(dto.PisoPuerta)) cliente.PisoPuerta = dto.PisoPuerta;
            if (!string.IsNullOrEmpty(dto.CodigoPostal)) cliente.CodigoPostal = dto.CodigoPostal;
            if (!string.IsNullOrEmpty(dto.Ciudad)) cliente.Ciudad = dto.Ciudad;
            if (!string.IsNullOrEmpty(dto.Provincia)) cliente.Provincia = dto.Provincia;
            if (!string.IsNullOrEmpty(dto.Pais)) cliente.Pais = dto.Pais;

            // Para booleanos (nullable), verificamos si tiene valor (no es null)
            if (dto.SuscritoNewsletter.HasValue)
                cliente.SuscritoNewsletter = dto.SuscritoNewsletter.Value;

            var clienteResponse = await client.From<Cliente>().Update(cliente);
            var clienteNuevo = clienteResponse.Models.First();

            var resultado = new ProfileUpdateResponseDto(
                "success",
                "Perfil actualizado correctamente",
                new ProfileDataDto(
                    usuarioNuevo.Nombre,
                    usuarioNuevo.Apellidos,
                    usuarioNuevo.Dni,
                    usuarioNuevo.Telefono,
                    clienteNuevo.Calle,
                    clienteNuevo.Numero,
                    clienteNuevo.PisoPuerta,
                    clienteNuevo.CodigoPostal,
                    clienteNuevo.Ciudad,
                    clienteNuevo.Provincia,
                    clienteNuevo.Pais,
                    clienteNuevo.SuscritoNewsletter
                )
            );

            return Results.Ok(resultado);
        }
        catch (Exception ex)
        {
            return Results.Problem("Error actualizando perfil: " + ex.Message);
        }
    }

    public record ProfileUpdateDto(
        string? Nombre,
        string? Apellidos,
        string? Dni,
        string? Telefono,
        string? Calle,
        string? Numero,
        string? PisoPuerta,
        string? CodigoPostal,
        string? Ciudad,
        string? Provincia,
        string? Pais,
        bool? SuscritoNewsletter
    );
}