using System;
using System.Linq;

namespace XIVRusUpdater.Windows.Debug;

public interface IDebugWindowWidget
{
    string[]? CommandShortcuts { get; init; }
    string DisplayName { get; init; }
    bool Ready { get; protected set; }

    void Load();
    void Draw();
    bool IsWidgetCommand(string command) => this.CommandShortcuts?.Any(shortcut => string.Equals(shortcut, command, StringComparison.InvariantCultureIgnoreCase)) ?? false;
}
