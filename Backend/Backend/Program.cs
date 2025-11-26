using Backend;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/", () => "CUDECA API");


// =========================
// AUTH ENDPOINTS
// =========================
var auth = app.MapGroup("/auth");
auth.MapPost("/register", RegisterUser);
auth.MapPost("/login", LoginUser);
auth.MapPost("/logout", LogoutUser);
auth.MapPost("/refresh", RefreshToken);


// =========================
// USERS ENDPOINTS
// =========================
var users = app.MapGroup("/users/me");
users.MapGet("", GetMyProfile);
users.MapPut("", UpdateMyProfile);
users.MapPatch("", PartialUpdateProfile);

users.MapGet("/tickets", GetMyTickets);
users.MapGet("/tickets/{ticketId}", GetMyTicketById);

users.MapGet("/donations", GetMyDonations);
users.MapGet("/donations/summary", GetMyDonationSummary);


// =========================
// EVENTS ENDPOINTS
// =========================
var events = app.MapGroup("/events");
events.MapGet("", ListEvents);
events.MapGet("/{eventId}", GetEvent);


// =========================
// TICKETS / PURCHASE
// =========================
var tickets = app.MapGroup("/tickets");

tickets.MapPost("/purchase/start", StartPurchase);
tickets.MapPost("/purchase/confirm", ConfirmPurchase);


// =========================
// DONATIONS
// =========================
var donations = app.MapGroup("/donations");

donations.MapPost("", CreateDonation);
donations.MapGet("/{donationId}/certificate", GetDonationCertificate);


// =========================
// PAYMENTS
// =========================
var payments = app.MapGroup("/payments");

payments.MapGet("/methods", GetPaymentMethods);


// =========================
// DISCOUNTS
// =========================
var discounts = app.MapGroup("/discounts");

discounts.MapPost("/validate", ValidateDiscount);


// =========================
// ADMIN ENDPOINTS
// =========================
var admin = app.MapGroup("/admin/events");

admin.MapGet("", AdminListEvents);
admin.MapPost("", AdminCreateEvent);
admin.MapPut("/{eventId}", AdminUpdateEvent);
admin.MapDelete("/{eventId}", AdminDeleteEvent);

app.Run();


// ===================================
// HANDLERS (boilerplate empty methods)
// ===================================

// -------- AUTH ----------
IResult RegisterUser(RegisterDto dto) => Results.Ok();
IResult LoginUser(LoginDto dto) => Results.Ok();
IResult LogoutUser() => Results.Ok();
IResult RefreshToken() => Results.Ok();

// -------- USERS ----------
IResult GetMyProfile() => Results.Ok();
IResult UpdateMyProfile(UserUpdateDto dto) => Results.Ok();
IResult PartialUpdateProfile(UserUpdatePartialDto dto) => Results.Ok();

IResult GetMyTickets() => Results.Ok();
IResult GetMyTicketById(int ticketId) => Results.Ok();


IResult GetMyDonations() => Results.Ok();
IResult GetMyDonationSummary() => Results.Ok();

// -------- EVENTS ----------
IResult ListEvents(string? query) => Results.Ok();
IResult GetEvent(int eventId) => Results.Ok();

// -------- PURCHASE ----------
IResult StartPurchase(PurchaseStartDto dto) => Results.Ok();
IResult ConfirmPurchase(PurchaseConfirmDto dto) => Results.Ok();

// -------- DONATIONS ----------
IResult CreateDonation(DonationDto dto) => Results.Ok();
IResult GetDonationCertificate(int donationId) => Results.File("dummy.pdf");

// -------- PAYMENTS ----------
IResult GetPaymentMethods() => Results.Ok();

// -------- DISCOUNTS ----------
IResult ValidateDiscount(DiscountCheckDto dto) => Results.Ok();

// -------- ADMIN ----------
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