namespace OpenMarketplace.Domain.Advertising;

public sealed class AdCampaign : OpenMarketplace.Domain.Common.Entity
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "Active";
    public DateTimeOffset StartDate { get; set; } = DateTimeOffset.UtcNow.AddDays(-1);
    public DateTimeOffset EndDate { get; set; } = DateTimeOffset.UtcNow.AddDays(30);
    public int Priority { get; set; } = 100;
    public decimal? Budget { get; set; }
}

public sealed class AdCreative : OpenMarketplace.Domain.Common.Entity
{
    public Guid CampaignId { get; set; }
    public string Placement { get; set; } = "HomeHero";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string DesktopImageUrl { get; set; } = "";
    public string MobileImageUrl { get; set; } = "";
    public string TargetUrl { get; set; } = "";
    public bool OpenInNewTab { get; set; } = true;
    public int SortOrder { get; set; }
    public string Status { get; set; } = "Active";
    public int MaxImpressions { get; set; }
    public int CurrentImpressions { get; set; }
    public int MaxClicks { get; set; }
    public int CurrentClicks { get; set; }
}

public sealed class AdStatistic : OpenMarketplace.Domain.Common.Entity
{
    public Guid CreativeId { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public int Impressions { get; set; }
    public int Clicks { get; set; }
}
