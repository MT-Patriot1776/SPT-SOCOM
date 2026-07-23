using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;

namespace SOCOM.Traders;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 4)]
public class SOCOMTraderUnlockSync(ProfileHelper profileHelper, TraderHelper traderHelper) : IOnUpdate
{
    private static readonly MongoId TraderId = "69325b507cd59da087ad2912";
    private const double InitialStanding = 0.2;

    public Task<bool> OnUpdate(long secondsSinceLastRun)
    {
        foreach (var (sessionId, profile) in profileHelper.GetProfiles())
        {
            var pmcData = profile.CharacterData?.PmcData;
            if (pmcData?.Info?.Level is not { } playerLevel)
            {
                continue;
            }

            if (!pmcData.TradersInfo.TryGetValue(TraderId, out var traderInfo))
            {
                traderHelper.GetTrader(TraderId, sessionId);
                pmcData.TradersInfo.TryGetValue(TraderId, out traderInfo);
            }

            if (traderInfo is null)
            {
                continue;
            }

            if (traderInfo.Standing is null or < InitialStanding or > 1)
            {
                traderInfo.Standing = InitialStanding;
            }

            traderHelper.ValidateTraderStandingsAndPlayerLevelForProfile(sessionId);

            var traderBase = traderHelper.GetTrader(TraderId, sessionId);
            var firstLoyaltyLevel = traderBase?.LoyaltyLevels?.FirstOrDefault();
            var requiredLevel = firstLoyaltyLevel?.MinLevel ?? 1;
            var shouldBeUnlocked = playerLevel >= requiredLevel;

            if (traderInfo.Unlocked != shouldBeUnlocked)
            {
                traderHelper.SetTraderUnlockedState(TraderId, shouldBeUnlocked, sessionId);
            }
        }

        return Task.FromResult(true);
    }
}
