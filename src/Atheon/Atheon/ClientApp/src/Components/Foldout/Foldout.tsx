import React, { useState } from "react";
import './Foldout.css';

type FoldoutProps = {
    children: any;
    headerText: string;
    foldedByDefault: boolean;
}

function Foldout(props: FoldoutProps) {
    const [isHidden, setIsHidden] = useState<boolean>(props.foldedByDefault);

    const handleHeaderClick = () => {
        setIsHidden(!isHidden);
    };

    const getClassNameForHeader = () => {
        return `${isHidden ? '' : "foldout-header-arrow-displayed"} foldout-header-arrow`;
    }

    return (
        <div className="foldout-container">
            <div className="foldout-header">
                <img className={getClassNameForHeader()}/>
                <label onClick={handleHeaderClick}>{props.headerText}</label>
            </div>

            <div className={isHidden ? "foldout-content-hidden" : "foldout-content-displayed"}>
                {props.children}
            </div>
        </div >
    )
}

export default Foldout;