# XIVRus (Dalamud layer)

<p align="center">
  <img src="https://raw.githubusercontent.com/xivrus/XIVRusUpdater/master/Data/icon.png" alt="XIVRus" width="256"/>
</p>

Final Fantasy XIV Translation Layer for XIVRus

The XIVRus translation engine for Final Fantasy XIV, which handles interactions with the game and automatically distributes the latest translation updates to all users. It uses [Penumbra](https://github.com/xivdev/Penumbra) to replace the in-game font (with one that supports Russian) and to replace in-game textures.

## Main Features

### Update
* Integrates with Penumbra: it automatically downloads and installs the translation mod (a font with Russian language support and textures) and keeps it up to date.
* Automatically checks for updates to the translation and the Penumbra mod.
* Auto-update can be disabled: checks will still run and notify you of new versions, but files will not be downloaded automatically.
* The update check interval is customizable.

### Translation
* The translation works on a component-based system. By default, everything that can be translated is translated.
* Individual components can be enabled or disabled, in which case the corresponding parts of the strings remain in the original language.
* There is an option to enable or disable all components at once.
* The translation engine can be selected in the settings.


* Access to test builds is enabled in the settings after you confirm that you understand the potential issues and accept responsibility for them.

## Support

For help resolving issues, you can open an issue directly on GitHub or message us directly on [Discord](https://discord.gg/e6z2VY8fNj)

## How To Install

- Import the below repository into Dalamud Settings by going into **Experimental** > **Custom Plugin Repositories**.
XIVRus (Dalamud Layer):
```
https://raw.githubusercontent.com/xivrus/XIVRusUpdater/master/repo.json
```

Penumbra:
```
https://raw.githubusercontent.com/xivdev/Penumbra/master/repo.json
```

- Look for **XIVRus** and **Penumbra** in the Dalamud Plugin Installer.
- Install the plugins and give them a little while to load.
- Please wait until all files have finished installing, and then restart the game (either manually or using the `/xlrestart` command)
- Enjoy! The game has now been translated.

### Prerequisites

XIVRus (Dalamud layer) assumes all the following prerequisites are met:
* XIVLauncher, FINAL FANTASY XIV, and Dalamud have all been installed and the game has been run with Dalamud at least once.
* XIVLauncher is installed to its default directories and configurations.
  * If a custom path is required for Dalamud's dev directory, it must be set with the `DALAMUD_HOME` environment variable.
* A .NET Core 8 SDK has been installed and configured, or is otherwise available.

### Building

1. Open up `XIVRusAutoUpdate.slnx` in your C# editor of choice (likely [Visual Studio](https://visualstudio.microsoft.com) or [JetBrains Rider](https://www.jetbrains.com/rider/)).
2. Build the solution. By default, this will build a `Debug` build, but you can switch to `Release` in your IDE.
3. The resulting plugin can be found at `XIVRusAutoUpdate/bin/x64/Debug/XIVRusAutoUpdate.dll` (or `Release` if appropriate.)

## Adding Your Translation

If, for any reason, you need to add your own translation engine to the plugin, the plugin is open to this type of contribution.
To do this, modify the file `XIVRusUpdater/Core/Components/TranslationEngine.cs` in your pull request and declare your engine as follows:
```cs
[TranslationEngineDefinition(“SampleEngineID”, “Penumbra Mod Name”, “Engine Display Name”, “Base API URL”, ResourceFormat.Xrt)]
public sealed class SampleEngine;
```

In this case:
- SampleEngineID - the engine’s internal identifier. Must not contain spaces.
- Penumbra Mod Name - the `InternalName` used for your mod in Penumbra
- Engine Display Name - the display name of your translation engine
- Base API URL - a link to the web API responsible for distributing the version manifest.
- ResourceFormat.Xrt - the format of the translation files. Currently, only XRT is supported, but feel free to add new ones

### Manifest Format

To function correctly, the plugin expects a specific type of API with accessible branches. Below are the API paths and an example of their manifest format:

$env{APIBASE}/branches/release.json

$env{APIBASE}/branches/test.json

$env{APIBASE}/branches/xrt.json
```json
{
  “game_version”: “Game version the translation is designed for”,
  “urls”: [
    “Direct download link 1”,
    “Direct download link 2”,
    “Direct download link 3”
  ],
  “changelog”: “Latest translation changelog”,
  “version”: “1.19.1”
}
```

## Acknowledgments
TODO: Add everyone who contributed in any way to the release of this plugin. 

## AI disclose

AI disclosure: [Assist](https://dalamud.dev/plugin-publishing/ai-policy). Public sources and human judgment were used in the design process, but the AI assisted in selecting specific architectural variations for certain components.

The generator is set to “Auto” due to a lack of high-quality documentation. 

## Third-party code attribution

The [SamplePlugin](https://github.com/goatcorp/SamplePlugin) template was used as the basis for building the project.

The design principle of the debug menu with widgets in XIVRusUpdater was taken from the `/xldata` layer for [Dalamud](https://github.com/goatcorp/Dalamud/tree/master/Dalamud/Interface/Internal/Windows/Data)

The Excel row rewriting layer in XIVRusUpdater is based on data mapped in [FFXIVClientStructs](https://github.com/aers/FFXIVClientStructs).

The cross-platform download feature with progress tracking in XIVRusUpdater is based on [HttpClientProgressExtensions](https://gist.github.com/dalexsoto/9fd3c5bdbe9f61a717d47c5843384d11)

Portions of the original source code have been adapted and modified for XIVRusUpdater.

The original copyright attribution and applicable licensing terms are retained.

##### Final Fantasy XIV © 2010-2026 SQUARE ENIX CO., LTD. All Rights Reserved. We are not affiliated with SQUARE ENIX CO., LTD. in any way.