using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace SOCOM.Traders;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 6)]
public class SOCOMSilentHousekeepingQuestSync(
    DatabaseService databaseService,
    ISptLogger<SOCOMSilentHousekeepingQuestSync> logger) : IOnLoad
{
    private static readonly MongoId QuestId = "693900000000000000000300";
    private static readonly MongoId KillConditionId = "693900000000000000000302";

    public Task OnLoad()
    {
        var tables = databaseService.GetTables();
        if (!tables.Templates.Quests.TryGetValue(QuestId, out var quest))
        {
            logger.Warning($"Unable to update SOCOM suppressor quest, quest {QuestId} was not found");
            return Task.CompletedTask;
        }

        var killCondition = quest.Conditions?.AvailableForFinish?
            .SelectMany(condition => condition.Counter?.Conditions ?? [])
            .FirstOrDefault(condition => condition.Id == KillConditionId);

        if (killCondition is null)
        {
            logger.Warning($"Unable to update SOCOM suppressor quest, kill condition {KillConditionId} was not found");
            return Task.CompletedTask;
        }

        var suppressorIds = tables.Templates.Items
            .Where(template => template.Value.Parent == BaseClasses.SILENCER)
            .Select(template => template.Key.ToString())
            .Distinct()
            .ToList();

        if (suppressorIds.Count == 0)
        {
            logger.Warning("Unable to update SOCOM suppressor quest, no suppressor templates were found");
            return Task.CompletedTask;
        }

        killCondition.WeaponModsInclusive = suppressorIds
            .Select(suppressorId => new List<string> { suppressorId })
            .ToList();
        return Task.CompletedTask;
    }
}
