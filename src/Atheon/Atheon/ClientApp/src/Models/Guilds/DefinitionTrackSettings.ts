export interface DefinitionTrackSettingsModel {
    trackedHashes: number[];
    isTracked: boolean;
    isReported: boolean;
    overrideReportChannel: string | null;
}