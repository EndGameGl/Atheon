import React from "react";
import { useAppSelector } from "../../../../hooks";
import { DefinitionDictionary } from "../../../../Models/Destiny/DefinitionDictionary";
import { DefinitionTrackSettingsModel } from "../../../../Models/Guilds/DefinitionTrackSettings";

type DefinitionTrackSettingsProps<T> = {
  settings: DefinitionTrackSettingsModel;
  definitionsStore: DefinitionDictionary<T>;
}

function DefinitionTrackSettings<T>(props: DefinitionTrackSettingsProps<T>) {
  const definitions = useAppSelector(state => state.destinyDefinitions);

  return (
    <>
      <div>
        Is tracked: {`${props.settings.isTracked}`}
      </div>
      <div>
        Is reported: {props.settings.isReported}
      </div>
      <div>
        Override report channel: {props.settings.overrideReportChannel}
      </div>
      <div>       
        Tracked definitions: {JSON.stringify(props.settings.trackedHashes)}
      </div>
    </>
  )
}

export default DefinitionTrackSettings;