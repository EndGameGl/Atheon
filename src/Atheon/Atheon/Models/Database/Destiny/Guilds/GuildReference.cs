using System.Text.Json.Serialization;

namespace Atheon.Models.Database.Destiny.Guilds;

public class GuildReference
{
    [JsonPropertyName("guildId")]
    public string GuildId { get; set; }

    [JsonPropertyName("guildName")]
    public string GuildName { get; set; }
}
