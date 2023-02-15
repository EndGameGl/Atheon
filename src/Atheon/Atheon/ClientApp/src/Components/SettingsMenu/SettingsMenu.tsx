import React, { useState } from "react";
import setDiscordTokenAsync from "../../Services/setDiscordToken";
import './SettingsMenu.css';

function SettingsMenu() {
    const [discordToken, setDiscordToken] = useState<string | null>(null);
    const [reloadDiscordClient, setReloadDiscordClient] = useState<boolean>(false);

    const updateDiscordToken = () => {
        setDiscordTokenAsync(discordToken, reloadDiscordClient)
            .catch((e) => console.log(e))
    }

    return (
        <div>
            <div className="input-wrapper">
                <label>Discord token: </label>
                <input type="text" value={discordToken} onChange={(e) => { setDiscordToken(e.target.value) }} />
                <input type="checkbox" checked={reloadDiscordClient} onChange={(e) => {
                    console.log(e.target.value);
                    setReloadDiscordClient(e.target.value === 'on')
                }} />
                <label>Reload client</label>
                <button onClick={updateDiscordToken}>Update</button>
            </div>
        </div>
    );
}

export default SettingsMenu;