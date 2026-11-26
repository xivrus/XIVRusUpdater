using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using System;

namespace XIVRusUpdater.Services;

public sealed class NotificationService
{
    private const int DurationMs = 10000;
    private readonly INotificationManager _notificationManager;

    public NotificationService(INotificationManager notificationManager)
    {
        _notificationManager = notificationManager;
    }

    public IActiveNotification? Error(string message, params (string Label, Action Callback)[] buttons) => Show(message, NotificationType.Error, buttons);
    public IActiveNotification? Warning(string message, params (string Label, Action Callback)[] buttons) => Show(message, NotificationType.Warning, buttons);
    public IActiveNotification? Information(string message, params (string Label, Action Callback)[] buttons) => Show(message, NotificationType.Info, buttons);

    private IActiveNotification? Show(string message, NotificationType type, (string Label, Action Callback)[] buttons)
    {
        if (!Plugin.Instance.Configuration.ShowNotifications)
            return null;

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
                var buttonSize = new System.Numerics.Vector2((ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X * (buttons.Length - 1))
                    / buttons.Length, 0);

                for (var i = 0; i < buttons.Length; i++)
                {
                    if (i > 0)
                        ImGui.SameLine();

                    if (ImGui.Button($"{buttons[i].Label}##NotificationButton{i}", buttonSize))
                    {
                        buttons[i].Callback();
                    }
                }
            };
        }

        return notification;
    }
}
