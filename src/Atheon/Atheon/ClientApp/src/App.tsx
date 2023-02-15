import React from 'react';
import AppHeader from './Components/AppHeader/AppHeader';
import SideBarMenu from './Components/SideBarMenu/SideBarMenu';
import './App.css'
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import SettingsMenu from './Components/SettingsMenu/SettingsMenu';
import Guilds from './Components/Guilds/Guilds';

function App() {
    return (
        <React.StrictMode>
            <BrowserRouter>
                <div className='application'>
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
        </React.StrictMode >
    );
}

export default App;
