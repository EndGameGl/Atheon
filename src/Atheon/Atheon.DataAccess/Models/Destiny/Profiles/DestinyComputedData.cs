namespace Atheon.DataAccess.Models.Destiny.Profiles;

public class DestinyComputedData
{
    public Dictionary<uint, int>? Drystreaks { get; set; }
    public Dictionary<uint, int>? Titles { get; set; }
    public int? PowerLevel { get; set; }
    public int? ArtifactPowerLevel { get; set; }
    public int? LifetimeScore { get; set; }
    public int? LegacyScore { get; set; }
    public int? ActiveScore { get; set; }
    public int? TotalTitlesEarned { get; set; }
}
