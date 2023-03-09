using Atheon.Models.Database.Destiny.Tracking;
using Atheon.Services.Interfaces;
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
            {
                DefinitionHashes.Collectibles.Classified_2629609052,
                CuratedCollectible.New(
                    DefinitionHashes.Collectibles.Classified_2629609052, 
                    "Vexcalibur",
                    "https://cdn.discordapp.com/attachments/296008136785920001/1083368881411866654/image.png")
            }
        };

        foreach (var curatedCollectible in curatedCollectibles)
        {
            await _destinyDb.UpsertCuratedCollectibleDefinitionAsync(curatedCollectible.Value);
        }

        CuratedCollectibles = new ReadOnlyDictionary<uint, CuratedCollectible>(curatedCollectibles);
    }
}
