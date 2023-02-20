import { DestinyManifest } from "quria";
import React, { useEffect, useState } from "react";
import { useAppDispatch, useAppSelector, useInterval } from "../../hooks";
import { DefinitionDictionary } from "../../Models/Destiny/DefinitionDictionary";
import quriaService from "../../Services/quriaService";
import { manifestDb } from "../../Stores/IndexedDb/ManifestDb";
import { DestinyDefinitionsState, setDefinitions, setIsLoading } from "../../Stores/Slices/destinyDefinitionsSlice";
import { setMessage, clearMessage } from "../../Stores/Slices/headerStatusMessageSlice";

const DefinitionsToStore = [
    "DestinyInventoryItemDefinition",
    "DestinySeasonDefinition",
    "DestinyChecklistDefinition",
    "DestinyArtifactDefinition"
];


function Destiny2ManifestUpdater() {
    const definitionsState = useAppSelector((state) => state.destinyDefinitions);
    const dispatch = useAppDispatch();

    const updateManifest = () => {
        dispatch(setMessage('Checking manifest...'));
        if (definitionsState.IsLoading)
            return;

        dispatch(setIsLoading(true));
        quriaService
            .destiny2
            .GetDestinyManifest()
            .then(async manifestResponse => {
                let manifest = manifestResponse.Response;
                let currentManifest = await manifestDb.manifestVersion.get(0);

                let currentLoadedDefinitions = await manifestDb.definitions
                    .where('DefinitionType')
                    .startsWith('D')
                    .primaryKeys();

                let manifestIsMissing = !currentManifest;
                let manifestIsOutdated = manifestIsMissing && currentManifest?.Manifest.version !== manifest.version;
                let definitionTypesToAdd: string[] = []
                let definitionTypesToRemove: string[] = [];
                currentLoadedDefinitions.forEach(loadedDefinition => {
                    if (!DefinitionsToStore.includes(loadedDefinition)) {
                        definitionTypesToRemove.push(loadedDefinition);
                    }
                });
                DefinitionsToStore.forEach(loadedDefinition => {
                    if (!currentLoadedDefinitions.includes(loadedDefinition)) {
                        definitionTypesToAdd.push(loadedDefinition);
                    }
                });

                if (manifestIsMissing || manifestIsOutdated || definitionTypesToAdd.length > 0 || definitionTypesToRemove.length > 0) {
                    dispatch(setMessage('Manifest is updating...'));
                    console.log(`Manifest data requires update due to: \n- Manifest missing: ${manifestIsMissing} \n- Manifest outdated: ${manifestIsOutdated} ${definitionTypesToAdd.length > 0 ? `\n-Need to add defs: ${definitionTypesToAdd}` : ''}`);

                    let manifestTable = await getManifestTables(manifest);
                    await validateStoredTables(manifestTable, definitionTypesToAdd, definitionTypesToRemove);
                    await updateManifestDataInDb(manifest);
                    const loadedDefs: DestinyDefinitionsState = {
                        IsLoaded: true,
                        IsLoading: false,
                        ManifestVersion: manifest.version,
                        InventoryItems: LoadTypesInMemory(manifestTable, definitionsState.InventoryItems.Type),
                        Seasons: LoadTypesInMemory(manifestTable, definitionsState.Seasons.Type),
                        Checklists: LoadTypesInMemory(manifestTable, definitionsState.Checklists.Type),
                        Artifacts: LoadTypesInMemory(manifestTable, definitionsState.Artifacts.Type)
                    };
                    dispatch(setDefinitions(loadedDefs));
                }
                else {
                    if (!definitionsState.IsLoaded) {
                        dispatch(setMessage('Loading definitions...'));
                        let manifestTable = await getManifestTables(manifest);
                        const loadedDefs: DestinyDefinitionsState = {
                            IsLoaded: true,
                            IsLoading: false,
                            ManifestVersion: manifest.version,
                            InventoryItems: LoadTypesInMemory(manifestTable, definitionsState.InventoryItems.Type),
                            Seasons: LoadTypesInMemory(manifestTable, definitionsState.Seasons.Type),
                            Checklists: LoadTypesInMemory(manifestTable, definitionsState.Checklists.Type),
                            Artifacts: LoadTypesInMemory(manifestTable, definitionsState.Artifacts.Type)
                        };
                        dispatch(setDefinitions(loadedDefs));
                    }
                }
            })
            .finally(() => {
                dispatch(setIsLoading(false));
                dispatch(clearMessage());
            });
    }

    useInterval(updateManifest, 1000 * 60);

    return (<></>);
}

async function getManifestTables(manifest: DestinyManifest): Promise<any> {
    let manifestPath = manifest.jsonWorldContentPaths["en"];
    let bungieResourcePath = 'https://www.bungie.net' + manifestPath;
    let manifestJsonResponse = await fetch(bungieResourcePath);
    return await manifestJsonResponse.json();
}

async function validateStoredTables(manifestTable: any, definitionTypesToAdd: string[], definitionTypesToRemove: string[]) {
    await Promise.all(Object.keys(manifestTable).map(async definitionType => {
        let table = manifestTable[definitionType];
        if (DefinitionsToStore.includes(definitionType)) {
            await manifestDb.definitions.put({
                DefinitionType: definitionType,
                DefinitionTable: table
            }, definitionType);
        }
    }));

    await Promise.all(definitionTypesToRemove.map(async defToRemove => {
        await manifestDb.definitions.delete(defToRemove);
    }));
}

async function updateManifestDataInDb(manifest: DestinyManifest) {
    await manifestDb.manifestVersion.put({
        Manifest: manifest,
        Id: 0
    }, 0);
}

function LoadTypesInMemory<T>(manifest: any, type: string): DefinitionDictionary<T> {
    console.log(type);
    const definitions = manifest[type];
    const dict: DefinitionDictionary<T> = {
        Type: type
    }
    Object.keys(definitions).map(x => {
        let hash = parseInt(x);
        let definition = definitions[x];
        dict[hash] = definition as T;
    });
    return dict;
}

export default Destiny2ManifestUpdater;