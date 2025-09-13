using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Stack.Base;

namespace Haraltd.Stack.Platform.MacOS.Server;

[Register("AppDelegate")]
public class AppDelegate(
    NSApplication app,
    ErrorData error,
    IServer serverInstance,
    OperationToken token
) : NSApplicationDelegate
{
    private NSStatusItem _item = null!;
    private NSMenu _menu = null!;

    private bool _isClosed;

    public override void DidFinishLaunching(NSNotification notification)
    {
        if (error != Errors.ErrorNone)
        {
            ShowErrorModal();
            CloseItemOnActivated(null, null!);
            return;
        }

        var bar = NSStatusBar.SystemStatusBar;
        _item = bar.CreateStatusItem(NSStatusItemLength.Square);

        _item.Button.Image = NSImage.ImageNamed("StatusBar");
        _item.Button.Title = "Haraltd";

        _menu = new NSMenu();

        var name = new NSMenuItem(_item.Button.Title);
        name.Enabled = false;
        _menu.AddItem(name);

        var sep = NSMenuItem.SeparatorItem;
        _menu.AddItem(sep);

        var closeItem = new NSMenuItem("Close Instance");
        closeItem.Activated += CloseItemOnActivated;
        _menu.AddItem(closeItem);

        Task.Run(() =>
        {
            token.Wait();
            CloseItemOnActivated(null, null!);
        });

        _item.Menu = _menu;
    }

    private void ShowErrorModal()
    {
        var alert = new NSAlert();
        alert.MessageText = "Error";
        alert.AlertStyle = NSAlertStyle.Informational;

        error.Metadata.TryGetValue("exception", out var value);
        alert.InformativeText =
            (value != null ? value.ToString() : error.Description) ?? "Unknown error";

        alert.RunModal();
        CloseItemOnActivated(null, null!);
    }

    private void CloseItemOnActivated(object? sender, EventArgs e)
    {
        if (_isClosed)
            return;

        _isClosed = true;

        app.Stop(app);
        app.AbortModal();
    }

    public override void WillTerminate(NSNotification notification)
    {
        serverInstance?.StopCurrentInstance();
        serverInstance?.Dispose();
    }
}
