using System;
using System.Collections.Generic;
using XIVRusUpdater.Resource;

namespace XIVRusUpdater.Core.Components;

public static partial class TranslationComponents
{
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class TranslationComponentAttribute : Attribute
{
    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public int Priority { get;  }

    public TranslationComponentAttribute(string id, string displayNameKey, string descriptionKey, int priority = 0)
    {
        Id = id;
        DisplayName = displayNameKey;
        Description = descriptionKey;
        Priority = priority;
    }
}

public sealed record TranslationComponent(string Id, string DisplayName, string Description, IReadOnlyDictionary<string, uint[]> Sheets);

[TranslationComponent("enemy_npc", nameof(Localization.EnemyNpcNames), nameof(Localization.EnemyNpcNamesDescription))]
public sealed class EnemyNpcComponent
{
    public static readonly string[] Sheets = ["BNpcName"];
}

[TranslationComponent("place_names", nameof(Localization.PlaceNames), nameof(Localization.PlaceNamesDescription))]
public sealed class PlaceNamesComponent
{
    public static readonly string[] Sheets = ["PlaceName", "Town"];
}

[TranslationComponent("content_finder", nameof(Localization.DutyNames), nameof(Localization.DutyNamesDescription))]
public sealed class ContentFinderComponent
{
    public static readonly string[] Sheets = ["ContentFinderCondition", "ContentFinderConditionTransient"];
}

[TranslationComponent("npc_names", nameof(Localization.FriendlyNpcNames), nameof(Localization.FriendlyNpcNamesDescription))]
public sealed class FriendlyNpcComponent
{
    public static readonly string[] Sheets = ["ENpcResident"];
}

[TranslationComponent("actions", nameof(Localization.ActionsTraitsStatuses),nameof(Localization.ActionsTraitsStatusesDescription))]
public sealed class ActionsComponent
{
    public static readonly string[] Sheets = [
        "Action",
        "ActionCategory",
        "ActionComboRouteTransient",
        "ActionTransient",
        "TraitTransient",
        "Status"
    ];
}

[TranslationComponent("achievements", nameof(Localization.Achievements), nameof(Localization.AchievementsDescription))]
public sealed class AchievementsComponent
{
    public static readonly string[] Sheets = ["Achievement"];
}

[TranslationComponent("titles", nameof(Localization.PlayerTitles), nameof(Localization.PlayerTitlesDescription))]
public sealed class TitlesComponent
{
    public static readonly string[] Sheets = ["Title"];
}

[TranslationComponent("collectibles", nameof(Localization.Collectibles), nameof(Localization.CollectiblesDescription))]
public sealed class CollectiblesComponent
{
    public static readonly string[] Sheets = [
        "Mount",
        "Companion",
        "CharaCard*",
        "BannerBg",
        "BannerDecoration",
        "BannerDesignPreset",
        "BannerFrame",
        "BuddyEquip",
        "TripleTriadCard",
        "MYCWarResultNotebook",
        "VVDNotebookContents",
        "MKDLore",
        "Leve"
    ];
}

[TranslationComponent("emotes", nameof(Localization.Emotes), nameof(Localization.EmotesDescription))]
public sealed class EmotesComponent
{
    public static readonly string[] Sheets = ["Emote"];
}

[TranslationComponent("color", nameof(Localization.Colors), nameof(Localization.ColorsDescription))]
public sealed class PaintComponent
{
    public static readonly string[] Sheets = ["Stain"];
}

[TranslationComponent("param", nameof(Localization.Attributes), nameof(Localization.AttributesDescription))]
public sealed class ParamComponent
{
    public static readonly string[] Sheets = ["BaseParam"];
}

[TranslationComponent("LazyLootPlugin", nameof(Localization.LazyLootPlugin), nameof(Localization.ThirdPartyDescription), 1)]
public sealed class LazyLootComponent
{
    public static readonly Dictionary<string, uint[]> Sheets = new()
    {
        ["LogMessage"] = [4234u, 5194u]
    };
}

// CustomDefaultQuantity, FixTarget, QuickSellItems, ColoredDutyRoulette
[TranslationComponent("SimpleTweakPlugin", nameof(Localization.SimpleTweaksPlugin), nameof(Localization.ThirdPartyDescription), 1)]
public sealed class SimpleTweaksComponent
{
    public static readonly Dictionary<string, uint[]> Sheets = new()
    {
        ["Addon"] = [93u, 914u, 915u, 3621u, 5480u],
        ["ContentRoulette"] = Array.Empty<uint>()
    };
}


[TranslationComponent("TriadBuddyPlugin", nameof(Localization.TriadBuddyPlugin), nameof(Localization.ThirdPartyDescription), 1)]
public sealed class TriadBuddyComponent
{
    public static readonly Dictionary<string, uint[]> Sheets = new()
    {
        ["TripleTriadRule"] = Array.Empty<uint>()
    };
}

[TranslationComponent("LifestreamPlugin", nameof(Localization.LifestreamPlugin), nameof(Localization.ThirdPartyDescription), 1)]
public sealed class LifeStreamComponent
{
    public static readonly Dictionary<string, uint[]> Sheets = new()
    {
        ["Town"] = Array.Empty<uint>(),
        ["Addon"] = [115u, 2090u, 6349u, 6355u, 6782u, 6784u, 12519u, 12624u, 17531u],
        ["Lobby"] = [11u, 25u, 95u, 96u, 629u, 1117u, 1150u, 1218u],
        ["LogMessage"] = [430u, 9419u, 9426u, 9427u],
        ["PlaceName"] = [1250u, 1251u, 1252u, 1300u],
        ["EObjName"] = [2002737u, 2002739u, 2004353u],
        ["Warp"] = [131148u],
        ["transport/Aetheryte"] = [1u, 2u, 3u, 16u],
        ["transport/AetheryteIshgard"] = [0u],
        ["custom/003/HouFixMansionEntrance_00359"] = [0u, 2u],
        ["custom/007/CtsMjiEntrance_00798"] = [4u, 7u],
        ["custom/001/CmnDefHousingPersonalRoomEntrance_00178"] = [11u]
    };
}
