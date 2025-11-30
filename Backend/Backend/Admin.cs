namespace Backend;

static class Admin
{
    public static IResult AdminListEvents() => Results.Ok();
    public static IResult AdminCreateEvent(EventAdminCreateDto dto) => Results.Ok();
    public static IResult AdminUpdateEvent(int eventId, EventAdminUpdateDto dto) => Results.Ok();
    public static IResult AdminDeleteEvent(int eventId) => Results.Ok();
    
    public record EventAdminUpdateDto(string? Title, string? Description, DateTime? Date);
    
    public record EventAdminCreateDto(string Title, string Description, DateTime Date);
}