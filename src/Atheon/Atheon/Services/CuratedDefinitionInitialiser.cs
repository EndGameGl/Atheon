using Atheon.Models.Database.Destiny.Tracking;
using Atheon.Services.Interfaces;
using DotNetBungieAPI.HashReferences;

namespace Atheon.Services;

public class CuratedDefinitionInitialiser
{
    private readonly IDestinyDb _destinyDb;

    public CuratedDefinitionInitialiser(
        IDestinyDb destinyDb)
    {
        _destinyDb = destinyDb;
    }

    public async Task Initialise()
    {
        await InitialiseRecords();
        await InitialiseCollectibles();

    }

    private async Task InitialiseRecords()
    {
        await _destinyDb.UpsertCuratedRecordDefinitionAsync(CuratedRecord.New(DefinitionHashes.Records.GraspofAvariceSoloFlawless));
    }

    private async Task InitialiseCollectibles()
    {
        await _destinyDb.UpsertCuratedCollectibleDefinitionAsync(CuratedCollectible.New(DefinitionHashes.Collectibles.Parasite));
    }
}
