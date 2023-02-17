import React from 'react';
import AppHeader from './Components/AppHeader/AppHeader';
import SideBarMenu from './Components/SideBarMenu/SideBarMenu';
import './App.css'
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import SettingsMenu from './Components/SettingsMenu/SettingsMenu';
import Guilds from './Components/Guilds/Guilds';
import { QueryClient, QueryClientProvider } from 'react-query';
import { Provider } from 'react-redux';
import { store } from './Stores/store';
import Destiny2ManifestUpdater from './Components/ServiceComponents/Destiny2ManifestUpdater';

const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            refetchOnWindowFocus: false
        }
    }
})

function App() {
    return (
        <React.StrictMode>
            <Provider store={store}>
                <QueryClientProvider client={queryClient}>
                    <BrowserRouter>
                        <div className='application'>
                            <Destiny2ManifestUpdater />
                            <AppHeader />
                            <div className='application-body-container'>
                                <SideBarMenu />
                                <div className='application-content-container'>
                                    <Routes>
                                        <Route path='/' element={<App />} />
                                        <Route path='/settings' element={<SettingsMenu />} />
                                        <Route path='/guilds' element={<Guilds />} />
                                    </Routes>
                                </div>
                            </div>
                        </div>
                    </BrowserRouter>
                </QueryClientProvider>
            </Provider>
        </React.StrictMode >
    );
}

export default App;
