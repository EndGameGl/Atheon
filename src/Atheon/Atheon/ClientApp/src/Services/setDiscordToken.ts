async function setDiscordTokenAsync(
    discordToken: string,
    reloadDiscordClient: boolean): Promise<ApiResponse<boolean>> {
    const response = await fetch(
        `api/SettingsStorage/SetDiscordToken/${reloadDiscordClient}`,
        {
            method: 'POST', 
            body: discordToken
        });
    const result =  await response.json() as ApiResponse<boolean>;
    return result;
}

export default setDiscordTokenAsync;