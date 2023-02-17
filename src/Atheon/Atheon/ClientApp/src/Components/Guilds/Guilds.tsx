import React, { useState } from "react";
import { useQuery } from "react-query";
import { GuildReference } from "../../Models/GuildReference";
import { getGuildReferencesAsync } from "../../Services/guildsApi";

function Guilds() {

    const [guilds, setGuilds] = useState<GuildReference[]>([])

    const getGuildsQuery = useQuery('guildReferences', async () => {
        const response = await getGuildReferencesAsync();
        return response.Data;
    },
        { onSuccess: setGuilds });

    return (
        <>
            {getGuildsQuery.isLoading ?
                <>Guilds are loading...</> :
                <ul>
                    {guilds.map(x =>
                        <li>
                            {x.guildId} - {x.guildName}
                        </li>)}
                </ul>
            }
        </>
    );
}

export default Guilds;