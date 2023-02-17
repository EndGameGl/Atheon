import React, { useEffect, useState } from "react";
import './SettingsMenu.css';
import {
    setDiscordTokenAsync,
    setBungieApiKeyAsync,
    setBungieManifestPathAsync,
    getBungieManifestPathAsync
} from "../../Services/settingsApi";
import { useQuery } from "react-query";

function SettingsMenu() {
    const [discordToken, setDiscordToken] = useState<string | null>(null);
    const [reloadDiscordClient, setReloadDiscordClient] = useState<boolean>(false);

    const [bungieApiKey, setBungieApiKey] = useState<string | null>(null);

    const [bungieManifestPath, setBungieManifestPath] = useState<string | null>(null);
    const [reloadBungieManifestPath, setReloadBungieManifestPath] = useState<boolean>(false);

    const bungieManifestPathQuery = useQuery('bungieManifestPath', async () => {
        const manifestPath = await getBungieManifestPathAsync();
        return manifestPath.Data;
    }, { onSuccess: setBungieManifestPath });


    const updateDiscordToken = () => {
        setDiscordTokenAsync(discordToken, reloadDiscordClient)
            .catch((e) => console.log(e));
    }

    const updateBungieApiKey = () => {
        setBungieApiKeyAsync(bungieApiKey)
            .catch((e) => console.log(e));
    }

    const updateDestinyManifestPath = () => {
        setBungieManifestPathAsync(bungieManifestPath, reloadBungieManifestPath)
            .catch((e) => console.log(e));
    }

    const discordInput = (): JSX.Element => {
        return (
            <div className="input-wrapper">
                <label>Discord token: </label>
                <input type="text" value={discordToken} onChange={(e) => { setDiscordToken(e.target.value) }} />
                <input type="checkbox" checked={reloadDiscordClient} onChange={(e) => {
                    setReloadDiscordClient(!reloadDiscordClient)
                }} />
                <label>Reload client</label>
                <button onClick={updateDiscordToken}>Update</button>
            </div>
        );
    }

    const bungieApiKeyInput = (): JSX.Element => {
        return (
            <div className="input-wrapper">
                <label>Bungie API key: </label>
                <input type="text" value={bungieApiKey} onChange={(e) => { setBungieApiKey(e.target.value) }} />
                <button onClick={updateBungieApiKey}>Update</button>
            </div>
        );
    }

    const bungieManifestPathInput = (): JSX.Element => {
        return (
            <div className="input-wrapper">
                <label>Destiny 2 Manifest path: </label>
                <input type="text" value={bungieManifestPathQuery.isLoading ? 'Loading...' : bungieManifestPath} onChange={(e) => { setBungieManifestPath(e.target.value) }} />
                <input type="checkbox" checked={reloadBungieManifestPath} onChange={(e) => {
                    setReloadBungieManifestPath(!reloadBungieManifestPath)
                }} />
                <label>Reload client</label>
                <button onClick={updateDestinyManifestPath}>Update</button>
            </div>
        );
    }

    return (
        <div>
            {discordInput()}
            {bungieApiKeyInput()}
            {bungieManifestPathInput()}
        </div>
    );
}

export default SettingsMenu;