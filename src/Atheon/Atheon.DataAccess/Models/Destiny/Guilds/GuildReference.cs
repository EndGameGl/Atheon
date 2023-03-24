using System.Text.Json.Serialization;

namespace Atheon.DataAccess.Models.Destiny.Guilds;

public class GuildReference
{
    [JsonPropertyName("guildId")]
    public string GuildId { get; set; }

    [JsonPropertyName("guildName")]
    public string GuildName { get; set; }
}
