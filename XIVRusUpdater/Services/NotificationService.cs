using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using System;

namespace XIVRusUpdater.Services;

public sealed class NotificationService
{
    private const int DurationMs = 5000;
    private readonly INotificationManager _notificationManager;

    public NotificationService(INotificationManager notificationManager)
    {
        _notificationManager = notificationManager;
    }

    public IActiveNotification Error(string message, params (string Label, Action Callback)[] buttons) => Show(message, NotificationType.Error, buttons);
    public IActiveNotification Warning(string message, params (string Label, Action Callback)[] buttons) => Show(message, NotificationType.Warning, buttons);
    public IActiveNotification Information(string message, params (string Label, Action Callback)[] buttons) => Show(message, NotificationType.Info, buttons);

    private IActiveNotification Show(string message, NotificationType type, (string Label, Action Callback)[] buttons)
    {
        var notification = _notificationManager.AddNotification(new Notification
        {
            Title = type switch
            {
                NotificationType.Error => "Error",
                NotificationType.Warning => "Warning",
                _ => "Information"
            },
            Content = message,
            Type = type,
            InitialDuration = TimeSpan.FromMilliseconds(DurationMs),
            ExtensionDurationSinceLastInterest = TimeSpan.FromMilliseconds(DurationMs)
        });

        if (buttons.Length > 0)
        {
            notification.DrawActions += args =>
            {
                ImGui.SetCursorScreenPos(args.MinCoord);

                for (var i = 0; i < buttons.Length; i++)
                {
                    if (i > 0)
                        ImGui.SameLine();

                    if (ImGui.Button($"{buttons[i].Label}##{i}"))
                        buttons[i].Callback();
                }
            };
        }

        return notification;
    }
}
