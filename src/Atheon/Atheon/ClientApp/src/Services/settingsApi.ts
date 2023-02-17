import ApiResponse from "../Models/ApiResponse";
import callApi from "./apiAccess";

export async function setBungieApiKeyAsync(
    bungieApiKey: string): Promise<ApiResponse<boolean>> {
    return await callApi<boolean>(
        'api/SettingsStorage/SetBungieApiKey',
        bungieApiKey,
        'POST');
}

export async function setDiscordTokenAsync(
    discordToken: string,
    reloadDiscordClient: boolean): Promise<ApiResponse<boolean>> {
    return await callApi<boolean>(
        `api/SettingsStorage/SetDiscordToken/${reloadDiscordClient}`,
        discordToken,
        'POST');
}

export async function setBungieManifestPathAsync(
    path: string,
    reloadManifest: boolean): Promise<ApiResponse<boolean>> {
    return await callApi<boolean>(
        `api/SettingsStorage/SetDestinyManifestPath/${reloadManifest}`,
        path,
        'POST');
}

export async function getBungieManifestPathAsync(): Promise<ApiResponse<string>> {
    return await callApi<string>(
        `api/SettingsStorage/GetDestinyManifestPath`,
        null,
        'GET');
}
