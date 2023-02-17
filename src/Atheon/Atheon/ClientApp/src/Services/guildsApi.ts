import ApiResponse from "../Models/ApiResponse";
import { GuildReference } from "../Models/GuildReference";
import callApi from "./apiAccess";

export async function getGuildReferencesAsync(): Promise<ApiResponse<GuildReference[]>> {
    return await callApi(
        'api/Guilds/GuildReferences',
        null,
        'GET');
}