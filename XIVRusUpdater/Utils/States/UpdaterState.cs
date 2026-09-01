using System;
using System.Collections.Generic;
using XIVRusUpdater.Core.Components;
using XIVRusUpdater.Models;
using XIVRusUpdater.Services;

namespace XIVRusUpdater.Utils.States;

public sealed class UpdaterState
{
    public TranslationManifest? LastRemoteStatus { get; set; }

    public bool PenumbraEnabled { get; set; }

    public ManifestState Translation { get; set; } = new ManifestState();

    public ManifestState Penumbra { get; set; } = new ManifestState();

    public bool UpdateAvailable => Translation.UpdateAvailable || Penumbra.UpdateAvailable;
    public bool ShowChangelog => Translation.ShowChangelog || Penumbra.ShowChangelog;
    public string LastChangelog => $"Translation Changelog: {Translation.LastChangelog}\n\nPenumbra Changelog: {Penumbra.LastChangelog}";
}
