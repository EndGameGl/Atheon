import { configureStore } from '@reduxjs/toolkit'
import destinyDefinitionsReducer from '../Stores/Slices/destinyDefinitionsSlice';
import headerStatusMessageReducer from '../Stores/Slices/headerStatusMessageSlice'

export const store = configureStore({
    reducer: {
        destinyDefinitions: destinyDefinitionsReducer,
        headerText: headerStatusMessageReducer
    },
});

export type RootState = ReturnType<typeof store.getState>;

export type AppDispatch = typeof store.dispatch;