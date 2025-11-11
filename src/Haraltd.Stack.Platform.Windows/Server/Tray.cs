using System.Drawing;
using System.Reflection;
using H.NotifyIcon.Core;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;

namespace Haraltd.Stack.Platform.Windows.Server;

internal partial class Tray : IDisposable
{
    private Icon _icon;
    private Stream _iconStream;
    private TrayIconWithContextMenu _trayInstance;

    private bool _isCreated;
    private OperationToken _token = OperationToken.None;

    internal void Create(OperationToken token, string socketPath)
    {
        _token = token;

        _iconStream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("Haraltd.Stack.Platform.Windows.Resources.shimicon.ico");
        _icon = new Icon(_iconStream!);

        var trayIcon = new TrayIconWithContextMenu
        {
            Icon = _icon.Handle,
            ToolTip = $"Haraltd\n\nStarted at socket '{socketPath}'",
            ContextMenu = new PopupMenu
            {
                Items = { new PopupMenuItem("Stop", (_, _) => _token.Release()) },
            },
        };

        trayIcon.Create();
        Thread.Sleep(1000);

        _trayInstance = trayIcon;
        _isCreated = true;
    }

    internal void ShowInfo(string info)
    {
        if (!_isCreated)
            return;

        _trayInstance.ShowNotification(nameof(NotificationIcon.Info), info, NotificationIcon.Info);
    }

    internal ErrorData ShowError(ErrorData error)
    {
        if (!_isCreated)
            return error;

        var errmsg = "An unknown error occurred";
        if (error.Metadata.TryGetValue("exception", out var message))
            errmsg = message.ToString();

        _trayInstance.ShowNotification(
            nameof(NotificationIcon.Error),
            errmsg!,
            NotificationIcon.Error
        );

        return error;
    }

    public void Dispose()
    {
        _trayInstance?.Dispose();
        _trayInstance = null;

        _iconStream?.Dispose();
        _iconStream = null;

        _icon?.Dispose();
        _icon = null;
    }
}
