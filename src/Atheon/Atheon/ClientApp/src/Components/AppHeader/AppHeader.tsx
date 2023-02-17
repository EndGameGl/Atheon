import React from "react";
import './AppHeader.css'
import { useAppSelector } from "../../hooks";

function AppHeader() {
    const definitionsLoadingState = useAppSelector((state) => state.destinyDefinitions.IsLoading);

    return (
        <div className="app-header-container">
            <div className="app-header-label">
                Atheon
            </div>
            <div>
                {definitionsLoadingState ? 'Definitions are updating...' : ''}
            </div>
        </div>
    )
}

export default AppHeader;