using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

    public TranslationComponentAttribute(string id, string displayName, string description)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
    }
}

public sealed record TranslationComponent(string Id, string DisplayName, string Description, IReadOnlyDictionary<string, uint[]> Sheets);

[TranslationComponent("enemy_npc", "Enemy NPCs", "Disable if translated enemy names make it difficult to follow guides or complete quests.")]
public sealed class EnemyNpcComponent
{
    public static readonly string[] Sheets = ["BNpcName"];
}

[TranslationComponent("place_names", "Place Names", "Disable if translated location names make it difficult to use wikis, maps, or guides.")]
public sealed class PlaceNamesComponent
{
    public static readonly string[] Sheets = ["PlaceName", "Town"];
}

[TranslationComponent("content_finder", "Duty Names and Descriptions", "Disable if you prefer to see original duty names in Duty Finder. The original name will still be shown in the description when translations are enabled.")]
public sealed class ContentFinderComponent
{
    public static readonly string[] Sheets = ["ContentFinderCondition", "ContentFinderConditionTransient"];
}

[TranslationComponent("npc_names", "Friendly NPC Names", "Disable if translated NPC names make it difficult to find vendors, quest NPCs, or other service NPCs.")]
public sealed class FriendlyNpcComponent
{
    public static readonly string[] Sheets = ["ENpcResident"];
}

[TranslationComponent("actions", "Actions, Traits and Statuses", "Disable if you prefer action, trait, and status descriptions in the original language.")]
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

[TranslationComponent("achievements", "Achievements", "Disable if you prefer achievement names in the original language. Achievement descriptions will also remain untranslated.")]
public sealed class AchievementsComponent
{
    public static readonly string[] Sheets = ["Achievement"];
}

[TranslationComponent("titles", "Titles", "Disable if you prefer player titles in the original language.")]
public sealed class TitlesComponent
{
    public static readonly string[] Sheets = ["Title"];
}

[TranslationComponent("collectibles", "Collectibles", "Includes mounts, minions, Adventurer Plate customization, chocobo barding, Triple Triad cards, Bozjan notes, Variant Dungeon records, Occult Records, and leves.")]
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

[TranslationComponent("emotes", "Emotes", "Disable if you prefer emote names in the original language. Emote commands are never translated.")]
public sealed class EmotesComponent
{
    public static readonly string[] Sheets = ["Emote"];
}
