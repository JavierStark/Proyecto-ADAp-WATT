namespace Backend;

// ==================== Auth Response DTOs ====================
public record AuthUserDto(
    string Id,
    string Email,
    DateTimeOffset? EmailConfirmedAt
);

public record AuthSessionDto(
    string AccessToken,
    string RefreshToken,
    long ExpiresIn,
    string TokenType
);

public record SignUpResponseDto(
    string Status,
    string Message,
    AuthUserDto User,
    AuthSessionDto? Session
);

public record SignInResponseDto(
    string Status,
    string Message,
    AuthUserDto User,
    AuthSessionDto Session
);

public record SignOutResponseDto(
    string Status,
    string Message
);

public record IsAdminResponseDto(bool IsAdmin);

public record IsPartnerResponseDto(bool IsSocio);

public record IsCorporateResponseDto(bool IsCorporate);

// ==================== Profile Response DTOs ====================
public record UserProfileDto(
    Guid IdInterno,
    string Email,
    string? Dni,
    string? Nombre,
    string? Apellidos,
    string? Telefono,
    string? Calle,
    string? Numero,
    string? Piso,
    string? Cp,
    string? Ciudad,
    string? Provincia,
    string? Pais,
    bool SuscritoNewsletter
);

public record ProfileUpdateResponseDto(
    string Status,
    string Message,
    ProfileDataDto Data
);

public record ProfileDataDto(
    string? Nombre,
    string? Apellidos,
    string? Dni,
    string? Telefono,
    string? Calle,
    string? Numero,
    string? Piso,
    string? Cp,
    string? Ciudad,
    string? Provincia,
    string? Pais,
    bool Newsletter
);

// ==================== Partner Response DTOs ====================
public record PartnerSubscriptionResponseDto(
    string Mensaje,
    DateTime Vence,
    Guid? PagoRef
);

public record PartnerDataDto(
    string? Plan,
    decimal Cuota,
    DateTime FechaInicio,
    DateTime FechaFin,
    bool IsActivo,
    int DiasRestantes
);

// ==================== Corporate Response DTOs ====================
public record CorporateUpdateResponseDto(
    string Message,
    CorporateDataDetailsDto Datos
);

public record CorporateDataDetailsDto(
    Guid? Id,
    string NombreEmpresa,
    Guid? FkCliente
);

public record CorporateDataDto(
    string NombreEmpresa
);

// ==================== Ticket Response DTOs ====================
public record TicketTypeDto(
    Guid TicketEventId,
    string Nombre,
    decimal Precio,
    int Stock
);

public record TicketTypesResponseDto(
    string Status,
    IEnumerable<TicketTypeDto> Data
);

public record PurchaseTicketsResponseDto(
    string Status,
    string Message,
    decimal TotalPagado
);

public record ValidateTicketQrResponseDto(
    string Status,
    string Message,
    TicketValidationDataDto Ticket
);

public record TicketValidationDataDto(
    Guid? Id,
    string? EventoNombre,
    decimal Precio,
    DateTime FechaCompra,
    string? Estado
);

// ==================== Donation Response DTOs ====================
public record DonationCreatedResponseDto(
    string Status,
    string Message,
    Guid? IdDonacion
);

// ==================== Payment Response DTOs ====================
public record PaymentMethodDto(
    int Id,
    string Nombre,
    string Codigo
);

public record PaymentMethodsResponseDto(
    string Status,
    string Message,
    IEnumerable<PaymentMethodDto> Metodos
);

// ==================== Discount Response DTOs ====================
public record DiscountDetailsDto(
    string? Codigo,
    decimal Descuento,
    decimal Porcentaje,
    int? UsosRestantes,
    string TipoDescuento,
    DateTime? FechaExpiracion,
    bool Valido
);

public record DiscountValidationResponseDto(
    string Status,
    string Message,
    DiscountDetailsDto Discount
);

// ==================== Admin Response DTOs ====================
public record TicketTypeCreatedDto(
    string Tipo,
    decimal Precio,
    int Stock
);

public record AdminEventCreateResponseDto(
    string Status,
    string Message,
    AdminEndpoints.EventoAdminDto Evento,
    IEnumerable<TicketTypeCreatedDto> TicketsCreados
);

public record AdminEventUpdateResponseDto(
    string Status,
    string Message,
    AdminEndpoints.EventoAdminDto Evento
);

public record AdminEventDeleteResponseDto(
    string Status,
    string Message
);

