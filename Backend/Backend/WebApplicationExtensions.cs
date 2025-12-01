namespace Backend;

public static class WebApplicationExtensions
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/auth");
        auth.MapPost("/register", Auth.RegisterUser);
        auth.MapPost("/login", Auth.LoginUser);
        auth.MapPost("/logout", Auth.LogoutUser);
        auth.MapPost("/refresh", Auth.RefreshToken);
        
        return app;
    }

    public static WebApplication MapUserEndpoints(this WebApplication app)
    {
        var users = app.MapGroup("/users/me");
        users.MapGet("", Profile.GetMyProfile);
        users.MapPut("", Profile.UpdateProfile);
        users.MapGet("/tickets", Tickets.GetMyTickets);
        users.MapGet("/tickets/{ticketId:int}", Tickets.GetMyTickets);
        users.MapGet("/donations", Donations.GetMyDonations);
        users.MapGet("/donations/summary", Donations.GetMyDonationSummary);
        
        return app;
    }

    public static WebApplication MapEventEndpoints(this WebApplication app)
    {
        var events = app.MapGroup("/events");
        events.MapGet("", Events.ListEvents);
        events.MapGet("/{eventId:int}", Events.GetEvent);
        
        return app;
    }

    public static WebApplication MapTicketEndpoints(this WebApplication app)
    {
        var tickets = app.MapGroup("/tickets");
        tickets.MapPost("/purchase/start", Events.StartPurchase);
        tickets.MapPost("/purchase/confirm", Events.ConfirmPurchase);
        
        return app;
    }

    public static WebApplication MapDonationEndpoints(this WebApplication app)
    {
        var donations = app.MapGroup("/donations");
        donations.MapPost("", Donations.CreateDonation);
        donations.MapGet("/certificate/annual", Donations.GetDonationCertificate);
        
        return app;
    }

    public static WebApplication MapPaymentEndpoints(this WebApplication app)
    {
        var payments = app.MapGroup("/payments");
        payments.MapGet("/methods", Events.GetPaymentMethods);
        
        return app;
    }

    public static WebApplication MapDiscountEndpoints(this WebApplication app)
    {
        var discounts = app.MapGroup("/discounts");
        discounts.MapPost("/validate", Events.ValidateDiscount);
        
        return app;
    }

    public static WebApplication MapAdminEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/admin/events");
        admin.MapGet("", Admin.AdminListEvents);
        admin.MapPost("", Admin.AdminCreateEvent);
        admin.MapPut("/{eventId:int}", Admin.AdminUpdateEvent);
        admin.MapDelete("/{eventId:int}", Admin.AdminDeleteEvent);
        
        return app;
    }
}