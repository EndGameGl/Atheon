import { DefinitionTrackSettingsModel } from "./DefinitionTrackSettings";

export interface GuildSettingsModel {
    guildId: string;
    guildName: string;
    defaultReportChannel: string | null;
    trackedMetrics: DefinitionTrackSettingsModel;
    trackedRecords: DefinitionTrackSettingsModel;
    trackedCollectibles: DefinitionTrackSettingsModel;
    trackedProgressions: DefinitionTrackSettingsModel;
    systemReportsEnabled: boolean;
    systemReportsOverrideChannel: string | null;
    clans: string[];
    reportClanChanges: boolean;
}