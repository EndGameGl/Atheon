using Atheon.DataAccess;
using Atheon.DataAccess.Models.Destiny.Tracking;
using DotNetBungieAPI.HashReferences;
using System.Collections.ObjectModel;

namespace Atheon.Services;

public class CuratedDefinitionInitialiser
{
    private readonly IDestinyDb _destinyDb;

    public ReadOnlyDictionary<uint, CuratedCollectible> CuratedCollectibles { get; private set; }

    public CuratedDefinitionInitialiser(
        IDestinyDb destinyDb)
    {
        _destinyDb = destinyDb;
    }

    public async Task Initialise()
    {
        await _destinyDb.ClearAllCuratedTables();
        await InitialiseRecords();
        await InitialiseCollectibles();

    }

    private async Task InitialiseRecords()
    {
        await _destinyDb.UpsertCuratedRecordDefinitionAsync(CuratedRecord.New(DefinitionHashes.Records.GraspofAvariceSoloFlawless));
    }

    private async Task InitialiseCollectibles()
    {

        var curatedCollectibles = new Dictionary<uint, CuratedCollectible>()
        {
        };

        foreach (var curatedCollectible in curatedCollectibles)
        {
            await _destinyDb.UpsertCuratedCollectibleDefinitionAsync(curatedCollectible.Value);
        }

        CuratedCollectibles = new ReadOnlyDictionary<uint, CuratedCollectible>(curatedCollectibles);
    }
}
