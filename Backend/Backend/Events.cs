namespace Backend;

static class Events
{
    public static IResult ListEvents(string? query) => Results.Ok();
    public static IResult GetEvent(int eventId) => Results.Ok();
    public static IResult StartPurchase(PurchaseStartDto dto) => Results.Ok();
    public static IResult ConfirmPurchase(PurchaseConfirmDto dto) => Results.Ok();
    
    public static IResult GetPaymentMethods() => Results.Ok();
    public static IResult ValidateDiscount(DiscountCheckDto dto) => Results.Ok();
    
    public record PurchaseStartDto(
        int EventId,
        int Quantity,
        bool IsCompany,
        string BillingAddress,
        string? DiscountCode);

    
    public record PurchaseConfirmDto(
        string PaymentMethod,
        string PaymentToken);

    public record DiscountCheckDto(string Code);

}