using DotNetBungieAPI.Models.Destiny.Components;
using DotNetBungieAPI.Models.Destiny.Definitions.Progressions;
using DotNetBungieAPI.Models.Destiny.Progressions;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny.Responses;
using DotNetBungieAPI.Service.Abstractions;
using DotNetBungieAPI.Models.GroupsV2;
using Atheon.Extensions;

namespace Atheon.Destiny2.Metadata;

public static class DestinyExtensions
{
	public static bool HasPublicRecords(this DestinyProfileResponse profileResponse)
	{
		return profileResponse.ProfileRecords.Data is not null;
	}

	public static DestinyProgression GetMostCompletedProgressionAcrossCharacters(
		this IDictionary<long, DestinyCharacterProgressionComponent> characterProgressions,
		uint progressionHash,
		IBungieClient bungieClient
	)
	{
		var progressionComponents = new List<DestinyProgression>(characterProgressions.Count);

		foreach (var (_, progressions) in characterProgressions)
		{
			if (progressions.Progressions.TryGetValue(progressionHash, out var destinyProgression))
			{
				progressionComponents.Add(destinyProgression);
			}
		}

		if (
			bungieClient.Repository.TryGetDestinyDefinition<DestinyProgressionDefinition>(
				progressionHash,
				out var progressionDefinition
			)
		)
		{
			if (progressionComponents.Any(x => x.CurrentResetCount is not null))
			{
				var totalProgressPoints = progressionDefinition.Steps.Sum(x => x.ProgressTotal);
				return progressionComponents.MaxBy(
					x =>
						x.CurrentResetCount.GetValueOrDefault() * totalProgressPoints
						+ x.CurrentProgress
				)!;
			}
			else
			{
				return progressionComponents.MaxBy(x => x.CurrentProgress)!;
			}
		}
		else
		{
			return progressionComponents.MaxBy(x => x.CurrentProgress)!;
		}
	}

	public static DestinyRecordComponent GetOptimalRecordAcrossCharacters(
		this IDictionary<long, DestinyCharacterRecordsComponent> characterRecords,
		uint recordHash
	)
	{
		if (characterRecords.Count is 1)
		{
			return characterRecords.First().Value.Records[recordHash];
		}

		var recordComponents = new List<DestinyRecordComponent>(characterRecords.Count);

		foreach (var (_, records) in characterRecords)
		{
			if (records.Records.TryGetValue(recordHash, out var destinyRecord))
			{
				recordComponents.Add(destinyRecord);
			}
		}

		var optimalRecord = recordComponents
			.OrderByDescending(x =>
			{
				return !x.State.HasFlag(
					DotNetBungieAPI.Models.Destiny.DestinyRecordState.ObjectiveNotCompleted
				);
			})
			.ThenByDescending(x =>
			{
				if (x.Objectives.Count > 0)
				{
					return x.Objectives.Sum(x =>
					{
						var progress = x.Progress.GetValueOrDefault();
						if (progress is 0)
							return 0;

						var completionRatio = x.CompletionValue / (decimal)progress;
						return completionRatio;
					});
				}
				if (x.IntervalObjectives.Count > 0)
				{
					return x.IntervalObjectives.Max(x => x.Progress).GetValueOrDefault();
				}
				return default;
			})
			.ThenByDescending(x =>
			{
				return x.CompletedCount.GetValueOrDefault();
			})
			.First();

		return optimalRecord;
	}

	public static bool ShouldScanClanMember(this GroupMember clanMember)
	{
		if (clanMember.IsOnline)
			return true;

		return (
				DateTime.UtcNow - clanMember.LastOnlineStatusChange.UnixTimeStampToDateTime()
			).TotalMinutes <= 15;
	}

	public static string ToFriendlyType(this BungieMembershipType bungieMembershipType)
	{
		return bungieMembershipType switch
		{
			BungieMembershipType.TigerXbox => "Xbox",
			BungieMembershipType.TigerPsn => "Playstation",
			BungieMembershipType.TigerEgs => "Epic Games",
			BungieMembershipType.TigerStadia => "Stadia",
			BungieMembershipType.TigerSteam => "Steam",
			_ => $"Unknown {(int)bungieMembershipType}"
		};
	}

	public static long? GetLastPlayedCharacterId(this DestinyProfileResponse destinyProfileResponse)
	{
		if (destinyProfileResponse.Characters is null)
			return null;

		if (destinyProfileResponse.Characters.Data.Count == 0)
			return null;

		var dateLastPlayed = destinyProfileResponse.Characters.Data.MaxBy(
			x => x.Value.DateLastPlayed
		);
		return dateLastPlayed.Key;
	}
}
