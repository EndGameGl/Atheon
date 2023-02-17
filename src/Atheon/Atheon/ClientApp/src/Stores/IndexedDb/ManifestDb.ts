import Dexie from "dexie";
import { DestinyManifest } from "quria";

class ManifestDb extends Dexie {
    definitions!: Dexie.Table<DefinitionTableEntry, string>;
    manifestVersion!: Dexie.Table<ManifestEntry, number>;

    constructor() {
        super("ManifestDatabase");
        this.version(1).stores({
            definitions: '++DefinitionType, DefinitionTable',
            manifestVersion: '++Id, Manifest'
        });
    }
}

interface DefinitionTableEntry {
    DefinitionType: string;
    DefinitionTable: any;
}

interface ManifestEntry {
    Id: number;
    Manifest: DestinyManifest;
}

export const manifestDb = new ManifestDb();