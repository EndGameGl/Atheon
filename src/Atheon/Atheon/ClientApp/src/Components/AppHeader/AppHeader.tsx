import React from "react";
import './AppHeader.css'
import { useAppSelector } from "../../hooks";

function AppHeader() {
    const headerMessage = useAppSelector((state) => state.headerText.text);

    return (
        <div className="app-header-container">
            <div className="app-header-label">
                Atheon
            </div>
            <div>
                {headerMessage}
            </div>
        </div>
    )
}

export default AppHeader;