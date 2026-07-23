using System.Reflection;
using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Loaders;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Services;
using Range = SemanticVersioning.Range;

namespace SOCOM;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.mtmilitia.socom";
    public override string Name { get; init; } = "SOCOM Armory";
    public override string Author { get; init; } = "MT_Militia";
    public override List<string>? Contributors { get; init; } = ["Srispt,EpicRangeTime"];
    public override SemanticVersioning.Version Version { get; init; } = new(typeof(ModMetadata).Assembly.GetName().Version?.ToString(3));
    public override Range SptVersion { get; init; } = new("~4.0.13");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, Range>? ModDependencies { get; init; } = new()
    {
        { "com.wtt.commonlib", new Range("~2.0.20") },
        { "com.epicrangetime.aio", new Range("~4.0.8") }
    };
    public override string? Url { get; init; } = "https://github.com/MT-Patriot1776/SPT-SOCOM";
    public override bool? IsBundleMod { get; init; } = false;
    public override string License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 3)]
public class SOCOM(
    WTTServerCommonLib.WTTServerCommonLib wttCommon,
    BundleLoader bundleLoader,
    BundleHashCacheService bundleHashCacheService,
    ModHelper modHelper) : IOnLoad
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task OnLoad()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        await RegisterMissingBundles(assembly);
        await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly);
        await wttCommon.CustomQuestZoneService.CreateCustomQuestZones(assembly);
        await wttCommon.CustomQuestService.CreateCustomQuests(assembly);
        await wttCommon.CustomLocaleService.CreateCustomLocales(assembly);
    }

    private async Task RegisterMissingBundles(Assembly assembly)
    {
        var absoluteModPath = modHelper.GetAbsolutePathToModFolder(assembly);
        var manifestPath = Path.Combine(absoluteModPath, "bundles.json");

        if (!File.Exists(manifestPath))
        {
            return;
        }

        var manifest = JsonSerializer.Deserialize<BundleManifest>(await File.ReadAllTextAsync(manifestPath), JsonOptions);
        if (manifest?.Manifest is null)
        {
            return;
        }

        var relativeModPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), absoluteModPath).Replace('\\', '/');
        foreach (var bundleManifest in manifest.Manifest)
        {
            if (string.IsNullOrWhiteSpace(bundleManifest.Key) || bundleLoader.GetBundle(bundleManifest.Key) is not null)
            {
                continue;
            }

            var bundlePath = Path.Combine(absoluteModPath, "bundles", bundleManifest.Key.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(bundlePath))
            {
                continue;
            }

            var crc = await bundleHashCacheService.CalculateMatchAndStoreHash(bundlePath);
            bundleLoader.AddBundle(bundleManifest.Key, new BundleInfo(relativeModPath, bundleManifest, crc));
        }
    }
}
