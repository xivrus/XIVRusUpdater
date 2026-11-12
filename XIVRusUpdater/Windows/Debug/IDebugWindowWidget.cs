using System;
using System.Linq;

namespace XIVRusUpdater.Windows.Debug;

public interface IDebugWindowWidget
{
    string DisplayName { get; init; }
    bool Ready { get; protected set; }

    void Load();
    void Draw();
}
