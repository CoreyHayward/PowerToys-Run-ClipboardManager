using Windows.ApplicationModel.DataTransfer;
using Clipboard = Windows.ApplicationModel.DataTransfer.Clipboard;

namespace Community.PowerToys.Run.Plugin.ClipboardManager;
internal class ClipboardManager
{
    private readonly HashSet<string> _clipboardItems = [];
    public IReadOnlySet<string> ClipboardItems => _clipboardItems.Reverse().ToHashSet();

    public ClipboardManager()
    {
        var clipboardHistoryResult = Clipboard.GetHistoryItemsAsync().AsTask().Result;
        var clipboardTextItems =
            clipboardHistoryResult.Items
            .Where(x => x.Content.Contains(StandardDataFormats.Text))
            .Select(x => x.Content.GetTextAsync().AsTask().Result)
            .ToList();

        foreach (var clipboardItemText in clipboardTextItems)
        {
            _clipboardItems.Add(clipboardItemText);
        }

        Clipboard.HistoryChanged += Clipboard_HistoryChanged;
    }

    public void ClearHistory()
    {
        _clipboardItems.Clear();
        Clipboard.ClearHistory();
    }

    public static void SetStringAsClipboardContent(string text)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);
    }

    private void Clipboard_HistoryChanged(object? sender, ClipboardHistoryChangedEventArgs e)
    {
        var item = Clipboard.GetContent();
        if (!item.Contains(StandardDataFormats.Text))
        {
            return;
        }

        var text = item.GetTextAsync().AsTask().Result;
        _clipboardItems.Add(text);
    }
}
