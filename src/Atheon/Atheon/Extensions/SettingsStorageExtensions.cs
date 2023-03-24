using Atheon.DataAccess;

namespace Atheon.Extensions;

public static class SettingsStorageExtensions
{
    public static async Task<string?> GetDiscordToken(this ISettingsStorage settingsStorage)
    {
        return await settingsStorage.GetOption<string>(SettingKeys.DiscordToken);
    }

    public static async Task SetDiscordToken(this ISettingsStorage settingsStorage, string token)
    {
        await settingsStorage.SetOption(SettingKeys.DiscordToken, token);
    }

    public static async Task<string?> GetBungieApiKey(this ISettingsStorage settingsStorage)
    {
        return await settingsStorage.GetOption<string>(SettingKeys.BungieApiKey);
    }

    public static async Task SetBungieApiKey(this ISettingsStorage settingsStorage, string apiKey)
    {
        await settingsStorage.SetOption(SettingKeys.BungieApiKey, apiKey);
    }

    public static async Task<string?> GetManifestPath(this ISettingsStorage settingsStorage)
    {
        return await settingsStorage.GetOption<string>(SettingKeys.BungieManifestStoragePath);
    }

    public static async Task SetManifestPath(this ISettingsStorage settingsStorage, string manifestPath)
    {
        await settingsStorage.SetOption(SettingKeys.BungieManifestStoragePath, manifestPath);
    }
}
