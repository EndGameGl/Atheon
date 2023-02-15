import React from "react";
import { Link } from "react-router-dom";
import './SideBarMenu.css';

function SideBarMenu() {

    return (
        <div className="menu-options-container">
            <a><Link to={'/guilds'}>Guilds</Link></a>
            <a><Link to={'/settings'}>Settings</Link></a>
        </div>
    );
}

export default SideBarMenu;