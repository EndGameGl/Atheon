import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import {
    DestinyArtifactDefinition,
    DestinyChecklistDefinition,
    DestinyInventoryItemDefinition,
    DestinySeasonDefinition
} from "quria";
import { DefinitionDictionary } from "../../Models/Destiny/DefinitionDictionary";
import { RootState } from "../store";

interface DestinyDefinitionsState {
    InventoryItems: DefinitionDictionary<DestinyInventoryItemDefinition>;
    Seasons: DefinitionDictionary<DestinySeasonDefinition>;
    Checklists: DefinitionDictionary<DestinyChecklistDefinition>;
    Artifacts: DefinitionDictionary<DestinyArtifactDefinition>;
}

const initialState: DestinyDefinitionsState = {
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
        }
    }
});

export const { setDefinitions } = definitionsSlice.actions;

export const selectDefinitions = (state: RootState) => state.destinyDefinitions;

export default definitionsSlice.reducer;