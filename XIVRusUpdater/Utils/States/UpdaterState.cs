using System;
using System.Collections.Generic;
using System.Text;
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
    public bool IsChecking { get; set; }
    public bool UpdateAvailable => Translation.UpdateAvailable || Penumbra.UpdateAvailable;
    public bool ShowChangelog => Translation.ShowChangelog || Penumbra.ShowChangelog;
    public string LastChangelog
    {
        get
        {
            var builder = new StringBuilder();

            if(!string.IsNullOrEmpty(Translation.LastChangelog))
                builder.Append($"Translation Changelog:\n{Translation.LastChangelog}");

            if (!string.IsNullOrEmpty(Penumbra.LastChangelog))
                builder.Append($"Penumbra Changelog:\n{Penumbra.LastChangelog}");

            return builder.ToString();
        }
    }
}
