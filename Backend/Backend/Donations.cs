namespace Backend;

static class Donations
{
    public static IResult GetMyDonations() => Results.Ok();
    public static IResult GetMyDonationSummary() => Results.Ok();
    public static IResult CreateDonation(DonationDto dto) => Results.Ok();
    public static IResult GetDonationCertificate(int donationId) => Results.File("dummy.pdf");
    
    public record DonationDto(decimal Amount);
}