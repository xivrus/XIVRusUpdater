using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.System.String;
using Lumina.Excel;
using System;
using System.Collections.Generic;
using XIVRusUpdater.Core;

namespace XIVRusUpdater.Hooks;

public unsafe class QuestHook : IDisposable
{
    private const string SayToDo = "SAYTODO";
    private const string IsAcceptSayEventSig = "48 89 5C 24 ?? 4C 89 4C 24 ?? 4C 89 44 24 ?? 48 89 54 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 83 EC ?? 4C 8B 35";
    private const int KeyColumn = 0;
    private const int ValueColumn = 1;

    private readonly IDataManager dataManager;
    private readonly TranslationParser parser;
    private delegate bool IsAcceptSayEventDelegate(QuestEventHandler* @this, Utf8String* text, GameObject** target, uint* value);
    private readonly Hook<IsAcceptSayEventDelegate> hook;

    public QuestHook(IGameInteropProvider provider, IDataManager dataManager, TranslationParser parser)
    {
        this.dataManager = dataManager;
        this.parser = parser;
        hook = provider.HookFromSignature<IsAcceptSayEventDelegate>(IsAcceptSayEventSig, IsAcceptSayHandler);
    }

    public void EnableAll() => hook.Enable();

    public void DisableAll() => hook.Disable();

    public void Dispose()
    {
        DisableAll();
        hook.Dispose();
    }

    private bool IsAcceptSayHandler(QuestEventHandler* @this, Utf8String* text, GameObject** target, uint* value)
    {
        Plugin.Log.Information($"Called IsAcceptSay with arg:\nText: {text->ToString()}\nQuestId: {@this->QuestId}, ScriptId: {@this->ScriptId}, scriptPath: {@this->ScriptPath}");
        var sheetPath = $"quest/{@this->ScriptPath}";
        if (!parser.IsSheetLoaded(sheetPath))
            return hook.Original(@this, text, target, value);

        if (hook.Original(@this, text, target, value))
            return true;

        var sheet = dataManager.GetExcelSheet<RawRow>(name: sheetPath);
        var saved = new Dictionary<string, byte[]>();
        try
        {
            foreach (ref var pair in @this->LuaEventHandler.LuaTexts)
            {
                var key = pair.Item2.Key.ToString();
                if (!key.Contains(SayToDo))
                    continue;

                foreach (var row in sheet)
                {
                    if (row.ReadStringColumn(KeyColumn).ExtractText() != key)
                        continue;

                    ref var luaText = ref pair.Item2.Value;
                    saved[key] = new ReadOnlySpan<byte>(luaText.StringPtr, (int)luaText.BufUsed - 1).ToArray();
                    luaText.SetString(row.ReadStringColumn(ValueColumn).Data.Span);
                    break;
                }
            }

            return hook.Original(@this, text, target, value);
        }
        finally
        {
            foreach (ref var pair in @this->LuaTexts)
            {
                if (saved.TryGetValue(pair.Item2.Key.ToString(), out var old))
                    pair.Item2.Value.SetString(old);
            }
        }
    }
}
