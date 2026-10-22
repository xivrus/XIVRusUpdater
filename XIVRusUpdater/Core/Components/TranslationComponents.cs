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

    public TranslationComponentAttribute(string id, string displayNameKey, string descriptionKey)
    {
        Id = id;
        DisplayName = displayNameKey;
        Description = descriptionKey;
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

// LazyLoot
[TranslationComponent("thirdparty", nameof(Localization.ThirdParty), nameof(Localization.ThirdPartyDescription))]
public sealed class ThirtPartyComponent
{
    public static readonly Dictionary<string, uint[]> Sheets = new()
    {
        ["LogMessage"] = [4234u, 5194u]
    };
}
