import ApiResponse from "../Models/ApiResponse";
import { GuildReference } from "../Models/GuildReference";
import { GuildChannelReferenceModel } from "../Models/Guilds/GuildChannelReferenceModel";
import { GuildSettingsModel } from "../Models/Guilds/GuildSettings";
import callApi from "./apiAccess";

export async function getGuildReferencesAsync(): Promise<ApiResponse<GuildReference[]>> {
    return await callApi(
        '/api/Guilds/GuildReferences',
        null,
        'GET');
}

export async function getGuildSettingsAsync(guildId: string): Promise<ApiResponse<GuildSettingsModel>> {
    return await callApi(
        `/api/Guilds/Settings/${guildId}`,
        null,
        'GET');
}

export async function getGuildTextChannelsAsync(guildId: string): Promise<ApiResponse<GuildChannelReferenceModel[]>> {
    return await callApi(
        `/api/Guilds/${guildId}/TextChannels`,
        null,
        'GET');
}

export async function updateGuildDbModelAsync(settingsModel: GuildSettingsModel): Promise<ApiResponse<GuildSettingsModel>> {
    return await callApi(
        `/api/Guilds/${settingsModel.guildId}/Update`,
        settingsModel,
        'POST');
}