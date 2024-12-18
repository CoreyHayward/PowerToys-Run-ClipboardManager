// Copyright (c) Corey Hayward. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Wox.Infrastructure;
using Wox.Plugin;
using Wox.Plugin.Common.Win32;
using Clipboard = Windows.ApplicationModel.DataTransfer.Clipboard;

namespace Community.PowerToys.Run.Plugin.ClipboardManager
{
    public class Main : IPlugin, ISettingProvider, IContextMenu
    {
        private ClipboardManager _clipboardManager = new();
        private PluginInitContext _context;
        private string _iconPath = "Images/ClipboardManager.light.png";
        private bool _directPaste;
        private int _beginTypeDelay;
        private int _maxResults;
        private string _pasterPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Paster", "Paster.exe");

        public string Name => "ClipboardManager";

        public string Description => "Searches the clipboard history and pastes the selected item";

        public static string PluginID => "2d9351ea495848d98f4771c27ac211e4";

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>()
        {
            new PluginAdditionalOption()
            {
                Key = "PasteDelay",
                DisplayLabel = "Paste Delay (ms)",
                DisplayDescription = "Sets how long in milliseconds to wait before paste occurs",
                NumberValue = 200,
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
            },
            new PluginAdditionalOption()
            {
                Key = "PasteBehaviour",
                DisplayLabel = "Paste Behaviour",
                DisplayDescription = "Sets what selecting a value will do",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Combobox,
                ComboBoxItems = PasteBehaviour.GetAll().ToDictionary(x => x.Name, x => x.Id.ToString()).ToList(),
                ComboBoxValue = PasteBehaviour.DirectPaste.Id,
            },
            new PluginAdditionalOption()
            {
                Key = "MaxResults",
                DisplayLabel = "Maximum Results",
                DisplayDescription = "Sets the maximum number of results to show",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
                NumberValue = -1,
                NumberBoxMin = -1,
            }
        };

        public void Init(PluginInitContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _context = context;
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());
        }

        public List<Result> Query(Query query)
        {
            if (!Clipboard.IsHistoryEnabled())
            {
                return [GetHistoryDisabledResult()];
            }

            var clipboardTextItems = _clipboardManager.ClipboardItems;
            if (clipboardTextItems.Count == 0)
            {
                return [GetNoItemsResult()];
            }

            if (string.IsNullOrWhiteSpace(query?.Search))
            {
                return clipboardTextItems.Take(_maxResults == -1 ? clipboardTextItems.Count : _maxResults).Select(CreateResult).ToList();
            }

            var results = new List<Result>();
            if (query.Search == "-")
            {
                results.Add(GetClearHistoryResult(query));
            }

            var matchingItems =
                clipboardTextItems
                    .Where(x => x.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
                    .Take(_maxResults == -1 ? clipboardTextItems.Count : _maxResults)
                    .Select(CreateResult);

            results.AddRange(matchingItems);
            return results;
        }

        private Result CreateResult(string text)
            => new Result()
            {
                Title = text.Split(Environment.NewLine, 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).First(),
                SubTitle = "Paste this value",
                IcoPath = _iconPath,
                Action = (context) =>
                {
                    ClipboardManager.SetStringAsClipboardContent(text);
                    if (!_directPaste)
                    {
                        return true;
                    }

                    Task.Run(() => RunAsSTAThread(() =>
                    {
                        Thread.Sleep(_beginTypeDelay);
                        SendKeys.SendWait("^(v)");
                    }));
                    return true;
                },
                ContextData = text,
            };

        private Result GetHistoryDisabledResult()
            => new Result()
            {
                Title = "Clipboard History is not enabled",
                SubTitle = "Select this option to enable clipboard history",
                IcoPath = _iconPath,
                Action = (context) =>
                {
                    try
                    {
                        var clipboardKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Clipboard", true);
                        clipboardKey!.SetValue("EnableClipboardHistory", "1", RegistryValueKind.DWord);
                        _context.API.ChangeQuery(_context.CurrentPluginMetadata.ActionKeyword, true);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Error occurred enabling the clipboard history", ex);
                        return false;
                    }
                }
            };

        private Result GetNoItemsResult()
            => new Result()
            {
                Title = "There's nothing here...",
                SubTitle = "There are no items in your clipboard history. Copy some text to see it here.",
                IcoPath = _iconPath
            };

        private Result GetClearHistoryResult(Query query)
            => new Result()
            {
                Title = "Clear clipboard history",
                SubTitle = "This will remove all entries from the clipboard history",
                IcoPath = _iconPath,
                Action = (context) =>
                {
                    _clipboardManager.ClearHistory();
                    _context.API.ChangeQuery(query.ActionKeyword, true);
                    return true;
                }
            };

        [DllImport("User32.dll")]
        static extern IntPtr GetForegroundWindow();

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            if (selectedResult.ContextData is null)
            {
                return [];
            }

            return new List<ContextMenuResult>
            {
                new()
                {
                    Title = "Run as administrator (Ctrl+Shift+Enter)",
                    Glyph = "\xE7EF",
                    FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                    AcceleratorKey = Key.Enter,
                    AcceleratorModifiers = ModifierKeys.Control | ModifierKeys.Shift,
                    Action = _ =>
                    {
                        ClipboardManager.SetStringAsClipboardContent((string)selectedResult.ContextData);
                        if (!_directPaste)
                        {
                            return true;
                        }

                        Task.Run(() => RunAsSTAThread(() =>
                        {
                            Thread.Sleep(_beginTypeDelay);
                            var foregroundWindow = GetForegroundWindow();
                            Helper.OpenInShell(_pasterPath, runAs: Helper.ShellRunAsType.Administrator, runWithHiddenWindow: true);
                            NativeMethods.SetForegroundWindow(foregroundWindow);
                        }));

                        return true;
                    },
                },
                new()
                {
                    Title = "Edit (Ctrl+E)",
                    Glyph = "\xE70F",
                    FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                    AcceleratorKey = Key.E,
                    AcceleratorModifiers = ModifierKeys.Control,
                    Action = c =>
                    {
                        _ = EditAsync(selectedResult);

                        return true;

                        static async Task EditAsync(Result selectedResult)
                        {
                            var text = (string)selectedResult.ContextData;
                            var tempFile = Path.GetTempFileName();
                            File.WriteAllText(tempFile, text);
                            await Task.Run(() => {
                                Process process = new()
                                {
                                    StartInfo =
                                    {
                                        FileName = tempFile,
                                        UseShellExecute = true,
                                    }
                                };
                                process.Start();
                                process.WaitForExit();
                            });

                            ClipboardManager.SetStringAsClipboardContent(File.ReadAllText(tempFile));
                            File.Delete(tempFile);
                        }
                    }
                }
            };
        }

        private void OnThemeChanged(Theme currentTheme, Theme newTheme)
        {
            UpdateIconPath(newTheme);
        }

        private void UpdateIconPath(Theme theme)
        {
            if (theme == Theme.Light || theme == Theme.HighContrastWhite)
            {
                return;
            }

            _iconPath = "Images/ClipboardManager.dark.png";
        }

        public System.Windows.Controls.Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            if (settings?.AdditionalOptions is null)
            {
                return;
            }

            var typeDelay = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "PasteDelay");
            var maxResults = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "MaxResults");
            var pasteBehaviour = settings.AdditionalOptions.First(x => x.Key == "PasteBehaviour");
            _directPaste = pasteBehaviour.ComboBoxValue == PasteBehaviour.DirectPaste.Id;
            _beginTypeDelay = (int)(typeDelay?.NumberValue ?? 200);
            _maxResults = (int)(maxResults?.NumberValue ?? -1);
        }

        /// <summary>
        /// Start an Action within an STA Thread
        /// </summary>
        /// <param name="action">The action to execute in the STA thread</param>
        static void RunAsSTAThread(Action action)
        {
            AutoResetEvent @event = new AutoResetEvent(false);
            Thread thread = new Thread(
                () =>
                {
                    action();
                    @event.Set();
                });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            @event.WaitOne();
        }
    }
}
