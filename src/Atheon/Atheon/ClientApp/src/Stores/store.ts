import { configureStore } from '@reduxjs/toolkit'
import destinyDefinitionsReducer from '../Stores/Slices/destinyDefinitionsSlice';

export const store = configureStore({
    reducer: {
        destinyDefinitions: destinyDefinitionsReducer
    },
});

export type RootState = ReturnType<typeof store.getState>;

export type AppDispatch = typeof store.dispatch;