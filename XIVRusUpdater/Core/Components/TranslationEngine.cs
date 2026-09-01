using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using XIVRusUpdater.Core.Resource;

namespace XIVRusUpdater.Core.Components;

public static partial class TranslationEngines;

[AttributeUsage(AttributeTargets.Class)]
public sealed class TranslationEngineDefinition : Attribute
{
    public string Id { get; }
    public string ModName { get; }
    public string DisplayName { get; }
    public string ApiUrl { get; }
    public ResourceFormat Format { get; }

    public TranslationEngineDefinition(string id, string modName, string dispayName, string api, ResourceFormat format)
    {
        Id = id;
        ModName = modName;
        DisplayName = dispayName;
        ApiUrl = api;
        Format = format;
    }
}

[TranslationEngineDefinition("XIVRusJapanese", "XIV Rus", "XIVRus — Japanese", "https://update.xivrus.ru/api/jp/", ResourceFormat.Xrt)]
public sealed class XIVRusJapaneseEngine;

[TranslationEngineDefinition("XIVRusEnglish", "XIV Rus", "XIVRus — English", "https://update.xivrus.ru/api", ResourceFormat.Xrt)]
public sealed class XIVRusEnglishEngine;
