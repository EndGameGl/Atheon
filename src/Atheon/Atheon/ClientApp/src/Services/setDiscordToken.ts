import callApi from "./apiAccess";

async function setDiscordTokenAsync(
    discordToken: string,
    reloadDiscordClient: boolean): Promise<ApiResponse<boolean>> {
    return await callApi(
        `api/SettingsStorage/SetDiscordToken/${reloadDiscordClient}`,
        discordToken,
        'POST');
}

export default setDiscordTokenAsync;