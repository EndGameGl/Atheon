import React, { useState } from "react";
import { useQuery } from "react-query";
import { Link } from "react-router-dom";
import { GuildReference } from "../../Models/GuildReference";
import { getDiscordClientIdAsync } from "../../Services/discordDataApi";
import { getGuildReferencesAsync } from "../../Services/guildsApi";

function Guilds() {

    const [guilds, setGuilds] = useState<GuildReference[]>([])

    const getGuildsQuery = useQuery('guildReferences', async () => {
        const response = await getGuildReferencesAsync();
        return response.Data;
    }, { onSuccess: setGuilds });

    const redirectToBotInvite = async () => {
        const { Data } = await getDiscordClientIdAsync();
        const inviteUrl = `https://discord.com/api/oauth2/authorize?client_id=${Data}&permissions=0&scope=bot%20applications.commands`
        window.open(inviteUrl);
    };

    return (
        <>
            {getGuildsQuery.isLoading ?
                <>Guilds are loading...</> :
                <ul>
                    {guilds.map(x =>
                        <li>
                            <Link to={`/guilds/settings/${x.guildId}`}>{x.guildName}</Link>
                        </li>
                    )}
                </ul>
            }
            <button onClick={redirectToBotInvite}>
                Invite bot to more guilds
            </button>
        </>
    );
}

export default Guilds;