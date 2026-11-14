using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace XIVRusUpdater.Hooks;

using ToLowerDelegate = Utf8String.Delegates.ToLower;
using ToUpperDelegate = Utf8String.Delegates.ToUpper;
public unsafe class Utf8StringHook : IDisposable
{
    [StructLayout(LayoutKind.Explicit, Size = 0x10)]
    private struct ExcludeWordEntry
    {
        [FieldOffset(0x0)] public byte* Str;
        [FieldOffset(0x8)] public int Length;
    }

    private Hook<ToUpperDelegate> toUpperHook;
    private Hook<ToLowerDelegate> toLowerHook;

    public Utf8StringHook(IGameInteropProvider provider)
    {
        toUpperHook = provider.HookFromAddress<ToUpperDelegate>(Utf8String.MemberFunctionPointers.ToUpper, ToUpperDetour);
        toLowerHook = provider.HookFromAddress<ToLowerDelegate>(Utf8String.MemberFunctionPointers.ToLower, ToLowerDetour);

        EnableAll();
    }

    public void EnableAll()
    {
        toUpperHook.Enable();
        toLowerHook.Enable();
    }

    public void DisableAll()
    {
        toUpperHook.Disable();
        toLowerHook.Disable();
    }

    public void Dispose()
    {
        DisableAll();
        toUpperHook.Dispose();
        toLowerHook.Dispose();
    }

    private Utf8String* ToUpperDetour(Utf8String* str, bool firstCharOnly, bool everyWord, bool normalizeVowels, byte* excludeWord)
    {
        try
        {
            if (str == null)
                return toUpperHook.Original(str, firstCharOnly, everyWord, normalizeVowels, excludeWord);

            byte* p = str->StringPtr;
            long len = str->BufUsed - 1;
            if (p == null || len <= 0)
                return str;

            if (Ascii.IsValid(new ReadOnlySpan<byte>(p, (int)len)))
                return toUpperHook.Original(str, firstCharOnly, everyWord, normalizeVowels, excludeWord);

            everyWord = false;

            var onlyFirst = firstCharOnly && !everyWord;
            var atWordStart = true;
            var entries = (ExcludeWordEntry*)excludeWord;
            long removed = 0;
            long i = 0;

            while (i < len)
            {
                var status = Rune.DecodeFromUtf8(new ReadOnlySpan<byte>(p + i, (int)(len - i)), out var rune, out var consumed);
                if (consumed < 1) consumed = 1;

                var skip = firstCharOnly && !atWordStart;

                if (status == OperationStatus.Done && !skip)
                {
                    if (normalizeVowels && atWordStart && TryGetBaseVowel(rune, out var ascii))
                    {
                        var extra = consumed - 1;
                        p[i] = ascii;
                        if (extra > 0)
                        {
                            var tail = (int)(len - i - consumed + 1);
                            new Span<byte>(p + i + consumed, tail).CopyTo(new Span<byte>(p + i + 1, tail));
                            len -= extra;
                            removed += extra;
                            consumed = 1;
                        }
                    }
                    else
                    {
                        var upper = Rune.ToUpperInvariant(rune);
                        if (upper != rune && upper.Utf8SequenceLength == consumed && !(i != 0 && excludeWord != null && IsExcluded(p + i, len - i + 1, entries)))
                        {
                            upper.EncodeToUtf8(new Span<byte>(p + i, consumed));
                        }
                    }
                }

                if (!skip)
                    atWordStart = false;
                if (status == OperationStatus.Done && rune.Value == ' ')
                    atWordStart = true;

                i += consumed;
                if (onlyFirst)
                    break;
            }

            if (removed != 0)
            {
                str->BufUsed -= removed;
                str->StringLength -= removed;
            }

            return str;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Utf8String.ToUpper detour failed");
            return toUpperHook.Original(str, firstCharOnly, everyWord, normalizeVowels, excludeWord);
        }
    }

    private Utf8String* ToLowerDetour(Utf8String* str)
    {
        try
        {
            if (str == null)
                return toLowerHook.Original(str);

            byte* p = str->StringPtr;
            long len = str->BufUsed - 1;
            if (p == null || len <= 0)
                return str;

            if (Ascii.IsValid(new ReadOnlySpan<byte>(p, (int)len)))
                return toLowerHook.Original(str);

            long i = 0;
            while (i < len)
            {
                var status = Rune.DecodeFromUtf8(new ReadOnlySpan<byte>(p + i, (int)(len - i)), out var rune, out var consumed);
                if (consumed < 1) consumed = 1;

                if (status == OperationStatus.Done)
                {
                    var lower = Rune.ToLowerInvariant(rune);
                    if (lower != rune && lower.Utf8SequenceLength == consumed)
                        lower.EncodeToUtf8(new Span<byte>(p + i, consumed));
                }

                i += consumed;
            }

            return str;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Utf8String.ToLower detour failed");
            return toLowerHook.Original(str);
        }
    }

    private static bool TryGetBaseVowel(Rune r, out byte ascii)
    {
        var v = r.Value;
        ascii = v switch
        {
            >= 0xE0 and <= 0xE5 => (byte)'A',
            >= 0xE8 and <= 0xEB => (byte)'E',
            >= 0xEC and <= 0xEF => (byte)'I',
            >= 0xF2 and <= 0xF6 => (byte)'O',
            >= 0xF9 and <= 0xFC => (byte)'U',
            _ => 0,
        };
        return ascii != 0;
    }

    private static bool IsExcluded(byte* text, long remaining, ExcludeWordEntry* e)
    {
        for (; e->Length != 0; e++)
        {
            var match = true;
            for (var k = 0; k < e->Length; k++)
            {
                var a = e->Str[k];
                var b = k < remaining ? text[k] : (byte)0;
                if (a != b) { match = false; break; }
                if (a == 0) break;
            }

            if (match)
                return true;
        }

        return false;
    }
}
