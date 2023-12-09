using Atheon.DataAccess.Models.Destiny;
using Atheon.DataAccess.Models.Destiny.Profiles;
using Atheon.Services.Interfaces;
using DotNetBungieAPI.Models.Destiny.Responses;
using DotNetBungieAPI.Service.Abstractions;

namespace Atheon.Services.Scanners.ProfileUpdaters;

public class MetricUpdater : IProfileUpdater
{
    public MetricUpdater()
    {
    }

    public bool ReliesOnSecondaryComponents => true;
    public int Priority => 0;

    public Task Update(
        IBungieClient bungieClient,
        DestinyProfileDbModel dbProfile,
        DestinyProfileResponse profileResponse,
        List<DiscordGuildSettingsDbModel> guildSettings)
    {
        UpdateMetrics(dbProfile, profileResponse);
        return Task.CompletedTask;
    }

    public Task UpdateSilent(IBungieClient bungieClient, DestinyProfileDbModel dbProfile, DestinyProfileResponse profileResponse)
    {
        UpdateMetrics(dbProfile, profileResponse);
        return Task.CompletedTask;
    }

    private void UpdateMetrics(
        DestinyProfileDbModel dbProfile,
        DestinyProfileResponse profileResponse)
    {
        dbProfile.Metrics ??= [];

        foreach (var (metricHash, metricComponent) in profileResponse.Metrics.Data.Metrics)
        {
            if (metricComponent.ObjectiveProgress is null)
                continue;

            if (dbProfile.Metrics.TryGetValue(metricHash, out var dbModel))
            {
                dbModel.Progress.Progress = metricComponent.ObjectiveProgress.Progress;
                dbModel.Progress.ObjectiveHash = metricComponent.ObjectiveProgress.Objective.Hash.GetValueOrDefault();
                dbModel.Progress.CompletionValue = metricComponent.ObjectiveProgress.CompletionValue;
                dbModel.Progress.IsComplete = metricComponent.ObjectiveProgress.IsComplete;
            }
            else
            {
                dbProfile.Metrics.Add(metricHash, DestinyMetricDbModel.FromMetricComponent(metricComponent));
            }
        }
    }
}
