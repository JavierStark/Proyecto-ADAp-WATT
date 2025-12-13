using Backend.Filters;

namespace Backend;

public static class WebApplicationExtensions
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/auth").WithTags("Authentication");
        
        auth.MapPost("/signup", Auth.SignUp)
            .WithSummary("Sign up a new user")
            .WithDescription("Register a new user account with email, password, and personal information. For testing purposes.")
            .Produces<SignUpResponseDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(500);
            
        auth.MapPost("/signin", Auth.SignIn)
            .WithSummary("Sign in with credentials")
            .WithDescription("Authenticate a user and receive access tokens. For testing purposes.")
            .Produces<SignInResponseDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(500);
        
        var authProtected = app.MapGroup("/auth")
            .WithTags("Authentication")
            .AddEndpointFilter<SupabaseAuthFilter>();
            
        authProtected.MapPost("/signout", Auth.SignOut)
            .WithSummary("Sign out current user")
            .WithDescription("End the current user session. Requires authentication.")
            .Produces<SignOutResponseDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(500)
            .WithOpenApi(op =>
            {
                op.Security = new List<Microsoft.OpenApi.Models.OpenApiSecurityRequirement>
                {
                    new() { [new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }] = [] }
                };
                return op;
            });

        return app;
    }

    public static WebApplication MapUserEndpoints(this WebApplication app)
    {
        var users = app.MapGroup("/users/me")
            .AddEndpointFilter<SupabaseAuthFilter>()
            .WithOpenApi(op =>
            {
                op.Security = new List<Microsoft.OpenApi.Models.OpenApiSecurityRequirement>
                {
                    new() { [new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }] = [] }
                };
                return op;
            });
            
        users.MapGet("", Profile.GetMyProfile)
            .WithTags("User Profile")
            .WithSummary("Get current user profile")
            .WithDescription("Retrieve complete profile information including personal data and address details.")
            .Produces<UserProfileDto>(200)
            .ProducesProblem(401)
            .ProducesProblem(500);
            
        users.MapPut("", Profile.UpdateProfile)
            .WithTags("User Profile")
            .WithSummary("Update user profile")
            .WithDescription("Update personal information and address details. Only provided fields will be updated.")
            .Produces<ProfileUpdateResponseDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .ProducesProblem(500);
        
        users.MapGet("/is-admin", Auth.IsUserAdmin)
            .WithTags("User Status")
            .WithSummary("Check if user is admin")
            .WithDescription("Verify if the authenticated user has administrator privileges.")
            .Produces<IsAdminResponseDto>(200)
            .ProducesProblem(401)
            .ProducesProblem(500);
            
        users.MapGet("/is-partner", Auth.IsPartner)
            .WithTags("User Status")
            .WithSummary("Check if user is partner")
            .WithDescription("Verify if the authenticated user has an active partner membership.")
            .Produces<IsPartnerResponseDto>(200)
            .ProducesProblem(401)
            .ProducesProblem(500);
            
        users.MapGet("/is-corporate", Auth.IsCorporate)
            .WithTags("User Status")
            .WithSummary("Check if user is corporate")
            .WithDescription("Verify if the authenticated user has a corporate profile.")
            .Produces<IsCorporateResponseDto>(200)
            .ProducesProblem(401)
            .ProducesProblem(500);
        
        users.MapGet("/tickets", Tickets.GetMyTickets)
            .WithTags("User Resources")
            .WithSummary("Get user's tickets")
            .WithDescription("Retrieve all event tickets purchased by the authenticated user.")
            .Produces<IEnumerable<Tickets.TicketDto>>(200)
            .ProducesProblem(401)
            .ProducesProblem(500);
            
        users.MapGet("/tickets/{ticketId}", Tickets.GetMyTickets)
            .WithTags("User Resources")
            .WithSummary("Get specific ticket")
            .WithDescription("Retrieve details of a specific ticket by ID.")
            .Produces<IEnumerable<Tickets.TicketDto>>(200)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .ProducesProblem(500);
            
        users.MapGet("/donations", Donations.GetMyDonations)
            .WithTags("User Resources")
            .WithSummary("Get user's donation history")
            .WithDescription("Retrieve complete donation history for the authenticated user.")
            .Produces<IEnumerable<Donations.DonationHistoryDto>>(200)
            .ProducesProblem(401)
            .ProducesProblem(500);
            
        users.MapGet("/donations/summary", Donations.GetMyDonationSummary)
            .WithTags("User Resources")
            .WithSummary("Get donation summary")
            .WithDescription("Get total amount donated by the authenticated user.")
            .Produces<Donations.DonationSummaryDto>(200)
            .ProducesProblem(401)
            .ProducesProblem(500);

        return app;
    }
    
    public static WebApplication MapPartnerEndpoints(this WebApplication app)
    {
        var partners = app.MapGroup("/partners")
            .WithTags("Partners")
            .AddEndpointFilter<SupabaseAuthFilter>()
            .WithOpenApi(op =>
            {
                op.Security = new List<Microsoft.OpenApi.Models.OpenApiSecurityRequirement>
                {
                    new() { [new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }] = [] }
                };
                return op;
            });
            
        partners.MapPost("/subscribe", Partner.BecomePartner)
            .WithSummary("Subscribe as partner")
            .WithDescription("Create or renew a partner membership. Supports monthly, quarterly, and annual plans. Processes payment and creates/extends membership period.")
            .Produces<PartnerSubscriptionResponseDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(500);
            
        partners.MapGet("/data", Partner.GetPartnerData)
            .WithSummary("Get partner membership data")
            .WithDescription("Retrieve current partner subscription details including plan type, fees, dates, and active status.")
            .Produces<PartnerDataDto>(200)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .ProducesProblem(500);

        return app;
    }
    
    public static WebApplication MapCorporateEndpoints(this WebApplication app)
    {
        var company = app.MapGroup("/company")
            .WithTags("Corporate")
            .AddEndpointFilter<SupabaseAuthFilter>()
            .WithOpenApi(op =>
            {
                op.Security = new List<Microsoft.OpenApi.Models.OpenApiSecurityRequirement>
                {
                    new() { [new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }] = [] }
                };
                return op;
            });
            
        company.MapPost("", Corporate.UpdateCorporate)
            .WithSummary("Create or update corporate profile")
            .WithDescription("Create a new corporate profile or update existing company information for the authenticated user.")
            .Produces<CorporateUpdateResponseDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(500);
            
        company.MapGet("", Corporate.GetCorporateData)
            .WithSummary("Get corporate profile")
            .WithDescription("Retrieve corporate profile information including company name.")
            .Produces<CorporateDataDto>(200)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .ProducesProblem(500);

        return app;
    }

    public static WebApplication MapEventEndpoints(this WebApplication app)
    {
        var events = app.MapGroup("/events").WithTags("Events");
        
        events.MapGet("", Events.ListEvents)
            .WithSummary("List all visible events")
            .WithDescription("Retrieve a list of all public events. Supports optional search by event name using 'query' parameter. Events are ordered by date ascending.")
            .Produces<IEnumerable<Events.EventoListDto>>(200)
            .ProducesProblem(500);
            
        events.MapGet("/{eventId}", Events.GetEvent)
            .WithSummary("Get event details")
            .WithDescription("Retrieve detailed information about a specific event including name, description, date, location, capacity, and tickets sold.")
            .Produces<Events.EventoListDto>(200)
            .ProducesProblem(404)
            .ProducesProblem(500);

        return app;
    }

    public static WebApplication MapTicketEndpoints(this WebApplication app)
    {
        var tickets = app.MapGroup("/tickets").WithTags("Tickets");
        
        tickets.MapGet("/type/event/{eventId}", Tickets.GetEventTicketTypes)
            .WithSummary("Get ticket types for event")
            .WithDescription("Retrieve all available ticket types (General, VIP, etc.) for a specific event including prices and stock availability.")
            .Produces<TicketTypesResponseDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(500);
            
        tickets.MapPost("/purchase", Tickets.PurchaseTickets)
            .WithSummary("Purchase event tickets (authenticated or anonymous)")
            .WithDescription("Process ticket purchase for one or more ticket types. Supports both authenticated users and anonymous purchases. For authenticated users, updates user profile data. For anonymous purchases, requires email and full fiscal data in the request. Validates stock, applies discount codes, processes payment, generates QR codes, and sends tickets via email.")
            .AddEndpointFilter<OptionalAuthFilter>()
            .Produces<PurchaseTicketsResponseDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .ProducesProblem(500);
            
        tickets.MapGet("/validate", Tickets.ValidateTicketQr)
            .WithSummary("Validate ticket QR code")
            .WithDescription("Verify if a QR code corresponds to a valid ticket and retrieve ticket details.")
            .Produces<ValidateTicketQrResponseDto>(200)
            .ProducesProblem(404)
            .ProducesProblem(500);

        return app;
    }

    public static WebApplication MapDonationEndpoints(this WebApplication app)
    {
        var donations = app.MapGroup("/donations")
            .WithTags("Donations")
            .AddEndpointFilter<SupabaseAuthFilter>()
            .WithOpenApi(op =>
            {
                op.Security = new List<Microsoft.OpenApi.Models.OpenApiSecurityRequirement>
                {
                    new() { [new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }] = [] }
                };
                return op;
            });
            
        donations.MapPost("", Donations.CreateDonation)
            .WithSummary("Make a donation")
            .WithDescription("Create a new donation with specified amount and payment method. Processes payment immediately.")
            .Produces<DonationCreatedResponseDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(500);
            
        donations.MapPost("/certificate/annual", Donations.GetDonationCertificate)
            .WithSummary("Generate annual donation certificate")
            .WithDescription("Generate a PDF certificate for tax purposes with all donations made in a specific fiscal year. Validates and updates user's fiscal data if needed.")
            .Produces<byte[]>(200, contentType: "application/pdf")
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .ProducesProblem(500);

        return app;
    }

    public static WebApplication MapPaymentEndpoints(this WebApplication app)
    {
        var payments = app.MapGroup("/payments").WithTags("Payments");
        
        payments.MapGet("/methods", Tickets.GetPaymentMethods)
            .WithSummary("Get available payment methods")
            .WithDescription("Retrieve list of all available payment methods including cards, PayPal, and bank transfers.")
            .Produces<PaymentMethodsResponseDto>(200);

        return app;
    }

    public static WebApplication MapDiscountEndpoints(this WebApplication app)
    {
        var discounts = app.MapGroup("/discounts").WithTags("Discounts");
        
        discounts.MapPost("/validate", Tickets.ValidateDiscount)
            .WithSummary("Validate discount code")
            .WithDescription("Check if a discount code is valid, not expired, and has available uses. Returns discount percentage and details.")
            .Produces<DiscountValidationResponseDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(404)
            .ProducesProblem(500);

        return app;
    }

    public static WebApplication MapAdminEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/admin/events")
            .WithTags("Admin - Events")
            .AddEndpointFilter<SupabaseAuthFilter>()
            .AddEndpointFilter<AdminAuthFilter>()
            .WithOpenApi(op =>
            {
                op.Security = new List<Microsoft.OpenApi.Models.OpenApiSecurityRequirement>
                {
                    new() { [new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }] = [] }
                };
                return op;
            });
            
        admin.MapGet("", AdminEndpoints.AdminListEvents)
            .WithSummary("List all events (Admin)")
            .WithDescription("Retrieve complete list of all events including hidden ones with full details and ticket information. Requires admin privileges.")
            .Produces<IEnumerable<AdminEndpoints.EventoAdminDto>>(200)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(500);
            
        admin.MapPost("", AdminEndpoints.AdminCreateEvent)
            .WithSummary("Create new event (Admin)")
            .WithDescription("Create a new event with ticket types. Supports image upload. Requires admin privileges. Use multipart/form-data for file upload.")
            .DisableAntiforgery()
            .Accepts<AdminEndpoints.EventoCreateDto>("multipart/form-data")
            .Produces<AdminEventCreateResponseDto>(201)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(500);
            
        admin.MapPut("/{eventId}", AdminEndpoints.AdminUpdateEvent)
            .WithSummary("Update event (Admin)")
            .WithDescription("Update an existing event including details, tickets, and image. Only provided fields will be updated. Requires admin privileges.")
            .DisableAntiforgery()
            .Accepts<AdminEndpoints.EventoModifyDto>("multipart/form-data")
            .Produces<AdminEventUpdateResponseDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500);
            
        admin.MapDelete("/{eventId}", AdminEndpoints.AdminDeleteEvent)
            .WithSummary("Delete event (Admin)")
            .WithDescription("Permanently delete an event and its associated image from storage. Requires admin privileges.")
            .Produces<AdminEventDeleteResponseDto>(200)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500);

        return app;
    }
    
    public static WebApplication MapDevEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment()) // Solo en desarrollo por seguridad
        {
            app.MapGet("/dev/dtos", DevTools.GetAllDtoStructures)
                .WithTags("Development")
                .WithSummary("Get all DTO structures (Dev only)")
                .WithDescription("Retrieve a complete list of all DTOs used in the API with their properties and types. Available only in development environment.")
                .Produces<Dictionary<string, object>>(200);
        }
        return app;
    }
}