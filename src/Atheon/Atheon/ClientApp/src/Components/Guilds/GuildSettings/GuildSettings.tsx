import React, { useState } from "react";
import { useQuery } from "react-query";
import { useParams } from "react-router-dom";
import { getGuildSettingsAsync, getGuildTextChannelsAsync } from "../../../Services/guildsApi";
import { GuildSettingsModel } from "../../../Models/Guilds/GuildSettings";
import './GuildSettings.css';
import Foldout from "../../Foldout/Foldout";
import { useAppSelector } from "../../../hooks";
import DefinitionTrackSettings from "./DefinitionTrackSettings/DefinitionTrackSettings";
import { DestinyCollectibleDefinition, DestinyMetricDefinition, DestinyProgressionDefinition, DestinyRecordDefinition } from "quria";
import { GuildChannelReferenceModel } from "../../../Models/Guilds/GuildChannelReferenceModel";

type GuildSettingsRouteParams = {
    guildId: string;
}

function GuildSettings() {
    const params = useParams<GuildSettingsRouteParams>();
    const [guildSettings, setGuildSettings] = useState<GuildSettingsModel | null>(null);
    const definitions = useAppSelector(state => state.destinyDefinitions);
    const [guildTextChannels, setGuildTextChannels] = useState<GuildChannelReferenceModel[]>([]);

    const getGuildSettingsQuery = useQuery(`guildSettings:${params.guildId}`,
        async () => {
            const resp = await getGuildSettingsAsync(params.guildId);
            return resp;
        }, {
        onSuccess: (data) => {
            if (data.Code === 200) {
                setGuildSettings(data.Data);
            }
        }
    });

    const getGuildTextChannelsQuery = useQuery(
        `guildTextChannels:${params.guildId}`,
        async () => await getGuildTextChannelsAsync(params.guildId),
        {
            onSuccess: (data) => {
                setGuildTextChannels(data.Data);
            }
        })

    if (!getGuildSettingsQuery.isSuccess || !getGuildTextChannelsQuery.isSuccess) {
        return (
            <>
                Loading data...
            </>);
    }

    return (
        <div>
            <div id="guild-settings-menu-element grid-menu-element">
                <label>Discord Guild ID:</label>
                <label>{guildSettings?.guildId}</label>
            </div>
            <div id="guild-settings-menu-element grid-menu-element">
                <label>Discord Guild Name:</label>
                <label>{guildSettings?.guildName}</label>
            </div>
            <div id="guild-settings-menu-element grid-menu-element">
                <label>Discord Default Report Channel ID:</label>
                <select onChange={(e) => {
                    const newChannelValue = e.target.value;
                    setGuildSettings((old) => {
                        const newData = { ...old };
                        newData.defaultReportChannel = newChannelValue;
                        return newData;
                    });
                }}>
                    <option value={""}>
                        No channel selected
                    </option>
                    {guildTextChannels.map(x => (
                        <option key={x.channelId} value={x.channelId} selected={guildSettings.defaultReportChannel === x.channelId}>
                            {x.channelName}
                        </option>
                    ))}
                </select>
            </div>
            <div id="guild-settings-menu-element grid-menu-element">
                <label>System Reports Enabled:</label>
                <input
                    type={'checkbox'}
                    onClick={() => setGuildSettings((old) => {
                        const newData = { ...old };
                        newData.systemReportsEnabled = !old.systemReportsEnabled;
                        return newData;
                    })}
                    checked={guildSettings?.systemReportsEnabled} />
            </div>
            <div id="guild-settings-menu-element grid-menu-element">
                <label>System Reports Override Channel:</label>
                <select onChange={(e) => {
                    const newChannelValue = e.target.value;
                    setGuildSettings((old) => {
                        const newData = { ...old };
                        newData.systemReportsOverrideChannel = newChannelValue;
                        return newData;
                    });
                }}>
                    <option value={""}>
                        No channel selected
                    </option>
                    {guildTextChannels.map(x => (
                        <option key={x.channelId} value={x.channelId} selected={guildSettings.systemReportsOverrideChannel === x.channelId}>
                            {x.channelName}
                        </option>
                    ))}
                </select>
            </div>
            <div id="guild-settings-menu-element grid-menu-element">
                <label>Report Clan Changes:</label>
                <input
                    type={'checkbox'}
                    onClick={() => setGuildSettings((old) => {
                        const newData = { ...old };
                        newData.reportClanChanges = !old.reportClanChanges;
                        return newData;
                    })}
                    checked={guildSettings?.reportClanChanges} />
            </div>
            <div id="guild-settings-menu-element grid-menu-element">
                <label>Destiny 2 Clans:</label>
                <div>
                    {guildSettings?.clans.length > 0 ?
                        <ul>
                            {guildSettings.clans.map(x => (
                                <li>
                                    {x}
                                </li>
                            ))}
                        </ul> :
                        <label>No clans added currently</label>}
                    <div className="clan-input-wrapper">
                        <input type="text"></input>
                        <button>Add new clan</button>
                    </div>
                </div>
            </div>
            <div id="guild-settings-menu-element">
                <Foldout headerText="Metric settings" foldedByDefault={true}>
                    <DefinitionTrackSettings<DestinyMetricDefinition>
                        settings={guildSettings.trackedMetrics}
                        definitionsStore={definitions.Metrics}
                    />
                </Foldout>
            </div>
            <div id="guild-settings-menu-element">
                <Foldout headerText="Record settings" foldedByDefault={true}>
                    <DefinitionTrackSettings<DestinyRecordDefinition>
                        settings={guildSettings.trackedRecords}
                        definitionsStore={definitions.Records}
                    />
                </Foldout>
            </div>
            <div id="guild-settings-menu-element">
                <Foldout headerText="Collectible settings" foldedByDefault={true}>
                    <DefinitionTrackSettings<DestinyCollectibleDefinition>
                        settings={guildSettings.trackedCollectibles}
                        definitionsStore={definitions.Collectibles}
                    />
                </Foldout>
            </div>
            <div id="guild-settings-menu-element">
                <Foldout headerText="Progression settings" foldedByDefault={true}>
                    <DefinitionTrackSettings<DestinyProgressionDefinition>
                        settings={guildSettings.trackedProgressions}
                        definitionsStore={definitions.Progressions}
                    />
                </Foldout>
            </div>
            <div id="guild-settings-menu-element grid-menu-element">
                <button>Save settings</button>
            </div>
        </div>
    );
}

export default GuildSettings;