import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import { RootState } from "../store";

export interface HeaderMessage {
    text: string;
}

const initialState: HeaderMessage = {
    text: ''
}

export const headerStatusMessageSlice = createSlice({
    name: 'headerStatusMessage',
    initialState: initialState,
    reducers: {
        setMessage: (state, newMessage: PayloadAction<string>) => {
            state.text = newMessage.payload;
        },
        clearMessage: (state) => {
            state.text = '';
        }
    }
});

export const { setMessage, clearMessage } = headerStatusMessageSlice.actions;

export const selectHeaderText = (state: RootState) => state.headerText.text;

export default headerStatusMessageSlice.reducer;