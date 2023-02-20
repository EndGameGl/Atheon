import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import {
    DestinyArtifactDefinition,
    DestinyChecklistDefinition,
    DestinyInventoryItemDefinition,
    DestinySeasonDefinition
} from "quria";
import { DefinitionDictionary } from "../../Models/Destiny/DefinitionDictionary";
import { RootState } from "../store";

export interface DestinyDefinitionsState {
    IsLoaded: boolean;
    IsLoading: boolean;
    ManifestVersion: string | null;
    InventoryItems: DefinitionDictionary<DestinyInventoryItemDefinition> | null;
    Seasons: DefinitionDictionary<DestinySeasonDefinition> | null;
    Checklists: DefinitionDictionary<DestinyChecklistDefinition> | null;
    Artifacts: DefinitionDictionary<DestinyArtifactDefinition> | null;
}

const initialState: DestinyDefinitionsState = {
    IsLoaded: false,
    IsLoading: false,
    ManifestVersion: null,
    InventoryItems: { Type: 'DestinyInventoryItemDefinition' },
    Seasons: { Type: 'DestinySeasonDefinition' },
    Checklists: { Type: 'DestinyChecklistDefinition' },
    Artifacts: { Type: 'DestinyArtifactDefinition' }
}

export const definitionsSlice = createSlice({
    name: 'destinyDefinitions',
    initialState: initialState,
    reducers: {
        setDefinitions: (state, newState: PayloadAction<DestinyDefinitionsState>) => {
            state = newState.payload;
        },
        setIsLoading: (state, payload: PayloadAction<boolean>) => {
            state.IsLoading = payload.payload;
        }
    }
});

export const { setDefinitions, setIsLoading } = definitionsSlice.actions;

export const selectDefinitions = (state: RootState) => state.destinyDefinitions;

export default definitionsSlice.reducer;