using Backend.Filters;

namespace Backend;

public static class WebApplicationExtensions
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/auth");
        auth.MapPost("/signup", Auth.SignUp).WithTags("Authentication (Testing)");
        auth.MapPost("/signin", Auth.SignIn).WithTags("Authentication (Testing)");
        
        var authProtected = app.MapGroup("/auth").AddEndpointFilter<SupabaseAuthFilter>();
        authProtected.MapPost("/signout", Auth.SignOut).WithTags("Authentication (Testing)");

        return app;
    }

    public static WebApplication MapUserEndpoints(this WebApplication app)
    {
        var users = app.MapGroup("/users/me").AddEndpointFilter<SupabaseAuthFilter>();
        users.MapGet("", Profile.GetMyProfile);
        users.MapPut("", Profile.UpdateProfile);
        
        users.MapGet("/is-admin", Auth.IsUserAdmin);
        users.MapGet("/is-partner", Auth.IsPartner);
        users.MapGet("/is-corporate", Auth.IsCorporate);
        
        users.MapGet("/tickets", Tickets.GetMyTickets);
        users.MapGet("/tickets/{ticketId}", Tickets.GetMyTickets);
        users.MapGet("/donations", Donations.GetMyDonations);
        users.MapGet("/donations/summary", Donations.GetMyDonationSummary);

        return app;
    }
    
    public static WebApplication MapPartnerEndpoints(this WebApplication app)
    {
        var partners = app.MapGroup("/partners").AddEndpointFilter<SupabaseAuthFilter>();
        partners.MapPost("/subscribe", Partner.BecomePartner);
        partners.MapGet("/data", Partner.GetPartnerData);

        return app;
    }
    
    public static WebApplication MapCorporateEndpoints(this WebApplication app)
    {
        var company = app.MapGroup("/company").AddEndpointFilter<SupabaseAuthFilter>();
        company.MapPost("", Corporate.UpdateCorporate);
        company.MapGet("", Corporate.GetCorporateData);

        return app;
    }

    public static WebApplication MapEventEndpoints(this WebApplication app)
    {
        var events = app.MapGroup("/events");
        events.MapGet("", Events.ListEvents);
        events.MapGet("/{eventId}", Events.GetEvent);

        return app;
    }

    public static WebApplication MapTicketEndpoints(this WebApplication app)
    {
        var tickets = app.MapGroup("/tickets");
        tickets.MapGet("/get", Tickets.GetEventTicketTypes);
        tickets.MapPost("/purchase", Tickets.PurchaseTickets).AddEndpointFilter<SupabaseAuthFilter>();
        tickets.MapGet("/validate", Tickets.ValidateTicketQr);

        return app;
    }

    public static WebApplication MapDonationEndpoints(this WebApplication app)
    {
        var donations = app.MapGroup("/donations").AddEndpointFilter<SupabaseAuthFilter>();
        donations.MapPost("", Donations.CreateDonation);
        donations.MapPost("/certificate/annual", Donations.GetDonationCertificate);

        return app;
    }

    public static WebApplication MapPaymentEndpoints(this WebApplication app)
    {
        var payments = app.MapGroup("/payments");
        payments.MapGet("/methods", Tickets.GetPaymentMethods);

        return app;
    }

    public static WebApplication MapDiscountEndpoints(this WebApplication app)
    {
        var discounts = app.MapGroup("/discounts");
        discounts.MapPost("/validate", Tickets.ValidateDiscount);

        return app;
    }

    public static WebApplication MapAdminEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/admin/events")
            .AddEndpointFilter<SupabaseAuthFilter>()
            .AddEndpointFilter<AdminAuthFilter>();
        admin.MapGet("", AdminEndpoints.AdminListEvents);
        admin.MapPost("", AdminEndpoints.AdminCreateEvent).DisableAntiforgery();
        admin.MapPut("/{eventId}", AdminEndpoints.AdminUpdateEvent).DisableAntiforgery();
        admin.MapDelete("/{eventId}", AdminEndpoints.AdminDeleteEvent);

        return app;
    }
    
    public static WebApplication MapDevEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment()) // Solo en desarrollo por seguridad
        {
            app.MapGet("/dev/dtos", DevTools.GetAllDtoStructures);
        }
        return app;
    }
}