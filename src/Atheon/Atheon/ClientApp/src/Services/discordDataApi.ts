import ApiResponse from "../Models/ApiResponse";
import callApi from "./apiAccess";

export async function getDiscordClientIdAsync(): Promise<ApiResponse<string>> {
    return await callApi(
        'api/DiscordData/GetDiscordClientId',
        null,
        'GET');
}