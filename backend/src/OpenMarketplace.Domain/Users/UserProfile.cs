namespace OpenMarketplace.Domain.Users;
public sealed class UserProfile : OpenMarketplace.Domain.Common.Entity
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Location { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public string Role { get; set; } = "Customer";
    public string Source { get; set; } = "WebCustomer";
    public string PasswordHash { get; set; } = "";
    public bool EmailVerified { get; set; }
    public bool PhoneVerified { get; set; }
    public bool IdVerified { get; set; }
    public bool BusinessVerified { get; set; }
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public int TrustScore { get; set; } = 50;
    public string Status { get; set; } = "Active";
}
