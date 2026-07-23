using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace SOCOM.Traders;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 5)]
public class SOCOMInitialProcurementAssortLock(
    DatabaseService databaseService,
    ISptLogger<SOCOMInitialProcurementAssortLock> logger) : IOnLoad
{
    private static readonly MongoId TraderId = "69325b507cd59da087ad2912";
    private static readonly MongoId InitialProcurementQuestId = "693900000000000000000100";

    public Task OnLoad()
    {
        if (!databaseService.GetTables().Traders.TryGetValue(TraderId, out var trader))
        {
            logger.Warning($"Unable to apply SOCOM quest assort locks, trader {TraderId} was not found");
            return Task.CompletedTask;
        }

        var barterScheme = trader.Assort?.BarterScheme;
        if (barterScheme is null)
        {
            logger.Warning($"Unable to apply SOCOM quest assort locks, trader {TraderId} has no barter scheme");
            return Task.CompletedTask;
        }

        if (trader.QuestAssort is null)
        {
            logger.Warning($"Unable to apply SOCOM quest assort locks, trader {TraderId} has no quest assort data");
            return Task.CompletedTask;
        }

        if (!trader.QuestAssort.TryGetValue("success", out var successAssorts))
        {
            successAssorts = new();
            trader.QuestAssort["success"] = successAssorts;
        }

        var alreadyQuestLockedAssorts = trader.QuestAssort.Values
            .SelectMany(questAssortsByStatus => questAssortsByStatus.Keys)
            .ToHashSet();

        foreach (var assortId in barterScheme.Keys)
        {
            if (alreadyQuestLockedAssorts.Contains(assortId))
            {
                continue;
            }

            successAssorts[assortId] = InitialProcurementQuestId;
        }

        return Task.CompletedTask;
    }
}
