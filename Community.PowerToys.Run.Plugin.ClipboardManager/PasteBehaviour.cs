namespace Community.PowerToys.Run.Plugin.ClipboardManager;
internal record PasteBehaviour
{
    public static PasteBehaviour DirectPaste => new(1, "Direct Paste");
    public static PasteBehaviour CopyToClipboard => new(2, "Copy To Clipboard");
    public static List<PasteBehaviour> GetAll() => [DirectPaste, CopyToClipboard];

    private PasteBehaviour(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; }
    public string Name { get; }
}
