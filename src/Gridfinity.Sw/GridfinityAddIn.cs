using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Gridfinity.Core;
using Microsoft.Win32;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swpublished;

namespace Gridfinity.Sw
{
    /// <summary>
    /// SolidWorks add-in shell. Adds a "Gridfinity" menu with two commands: the tray designer dialog, and a
    /// one-click "build from JSON file". Register with regasm /codebase (see scripts\register.cmd).
    /// </summary>
    [ComVisible(true)]
    [Guid("5E1C3C0A-4F4D-4C2B-9B4C-6E1D6A0F7A11")]
    [ProgId("Gridfinity.SolidWorksAddIn")]
    public class GridfinityAddIn : ISwAddin
    {
        private const string AddInTitle = "Gridfinity Tray Builder";
        private const string AddInDescription = "Parametric Gridfinity trays and part holders.";
        private const int GroupId = 0x47464E59; // arbitrary, must stay constant between releases

        private ISldWorks _app;
        private int _cookie;
        private ICommandGroup _group;

        // ---- ISwAddin ------------------------------------------------------------------------
        /// <summary>Where start-up progress is written, so a missing menu can be diagnosed: %TEMP%\Gridfinity_addin.log</summary>
        private static string LogPath { get { return Path.Combine(Path.GetTempPath(), "Gridfinity_addin.log"); } }

        private static void Log(string message)
        {
            try { File.AppendAllText(LogPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + System.Environment.NewLine); }
            catch { /* logging must never break the add-in */ }
        }

        private bool _building;
        private bool _menuCreated;
        private SldWorks _events;   // kept so the event hooks stay alive
        private readonly System.Diagnostics.Stopwatch _sinceConnect = new System.Diagnostics.Stopwatch();
        private const int MenuDelayMs = 4000;

        private bool StartupFinished()
        {
            try { return (bool)((dynamic)_app).StartupProcessCompleted; }
            catch { return true; } // cannot tell, so do not wait
        }

        // The menu has to be created on SolidWorks' interface thread. ConnectToSW can run elsewhere while SolidWorks is
        // starting, and a menu made there never shows. SolidWorks events always arrive on the interface thread.
        private int OnIdle()
        {
            // SolidWorks reports start-up as finished before its menus are built, so also wait a few seconds.
            if (_sinceConnect.ElapsedMilliseconds < MenuDelayMs) return 0;
            Unhook();
            CreateMenu("first idle " + (_sinceConnect.ElapsedMilliseconds / 1000.0).ToString("0.0") + " s after connecting");
            return 0;
        }

        private int OnActiveDocChanged()
        {
            if (_sinceConnect.ElapsedMilliseconds < MenuDelayMs) return 0; // too early; the idle event will do it
            Unhook();
            CreateMenu("first document");
            return 0;
        }

        private void Hook()
        {
            try
            {
                _events = (SldWorks)_app;
                _events.OnIdleNotify += OnIdle;
                _events.ActiveModelDocChangeNotify += OnActiveDocChanged;
            }
            catch (Exception ex)
            {
                Log("Could not subscribe to SolidWorks events (" + ex.Message + "); creating the menu now.");
                _events = null;
                CreateMenu("no events");
            }
        }

        private void Unhook()
        {
            if (_events == null) return;
            try { _events.OnIdleNotify -= OnIdle; } catch { }
            try { _events.ActiveModelDocChangeNotify -= OnActiveDocChanged; } catch { }
            _events = null;
        }

        public bool ConnectToSW(object thisSw, int cookie)
        {
            // Any failure is logged and the add-in still reports "loaded", so the log can be read afterwards.
            try
            {
                Log("ConnectToSW start (cookie " + cookie + ", assembly " + typeof(GridfinityAddIn).Assembly.Location + ")");
                _app = (ISldWorks)thisSw;
                _cookie = cookie;
                _menuCreated = false;
                _app.SetAddinCallbackInfo2(0, this, _cookie);

                // Creating the menu inside ConnectToSW does not work at start-up (the log says it succeeded, but the
                // Tools menu stays empty), so always wait for SolidWorks' first idle event a few seconds later.
                _sinceConnect.Restart();
                Hook();
                return true;
            }
            catch (Exception ex)
            {
                Log("ConnectToSW failed: " + ex);
                return true;
            }
        }

        private void CreateMenu(string reason)
        {
            if (_app == null || _building || _menuCreated) return;
            _building = true;
            _menuCreated = true;
            try
            {
                ICommandManager cm = _app.GetCommandManager(_cookie);
                try { cm.RemoveCommandGroup2(GroupId, true); } catch { }

                int error = 0;
                _group = cm.CreateCommandGroup2(GroupId, "Gridfinity", "Gridfinity tray builder",
                                                "Create parametric Gridfinity trays and part holders", -1, true, ref error);
                Log("[" + reason + "] CreateCommandGroup2: " + (_group == null ? "NULL" : "ok") + ", error code " + error);
                if (_group == null) return;

                int menu = (int)swCommandItemType_e.swMenuItem;
                int first = _group.AddCommandItem2("Gridfinity designer...", -1, "Design a box, baseplate or part holder", "Designer",
                                                   0, "OnNewTray", "CanRun", 1, menu);
                int second = _group.AddCommandItem2("Build from JSON file...", -1, "Build a box or baseplate from a saved definition", "Build from JSON",
                                                    0, "OnBuildFromJson", "CanRun", 2, menu);
                Log("[" + reason + "] AddCommandItem2 returned " + first + " and " + second + " (-1 means it failed)");

                _group.HasToolbar = false;
                _group.HasMenu = true;
                bool activated = _group.Activate();
                Log("[" + reason + "] Activate returned " + activated);
            }
            catch (Exception ex)
            {
                Log("[" + reason + "] CreateMenu failed: " + ex);
            }
            finally
            {
                _building = false;
            }
        }

        public bool DisconnectFromSW()
        {
            Log("DisconnectFromSW");
            Unhook();
            try
            {
                if (_app != null) _app.GetCommandManager(_cookie).RemoveCommandGroup2(GroupId, true);
            }
            catch { }
            _group = null;
            _app = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return true;
        }

        // ---- Command callbacks (invoked by name from SolidWorks, so they must be public) ---------
        public void OnNewTray()
        {
            using (var form = new TrayDesignerForm(_app))
                form.ShowDialog(Owner());
        }

        public void OnBuildFromJson()
        {
            using (var dlg = new OpenFileDialog { Filter = "Gridfinity JSON (*.json)|*.json|All files (*.*)|*.*" })
            {
                if (dlg.ShowDialog(Owner()) != DialogResult.OK) return;
                try
                {
                    string json = File.ReadAllText(dlg.FileName);
                    var builder = new GridfinityBuilder(_app);
                    SwBuildResult r = BaseplateDefinition.LooksLikeBaseplate(json)
                        ? builder.BuildBaseplate(BaseplateDefinition.FromJson(json))
                        : ClipDefinition.LooksLikeClips(json)
                            ? builder.BuildClips(ClipDefinition.FromJson(json))
                            : builder.Build(TrayDefinition.FromJson(json));
                    string text = r.Message;
                    if (r.Warnings.Count > 0) text += "\n\nNotes:\n- " + string.Join("\n- ", r.Warnings.ToArray());
                    MessageBox.Show(Owner(), text, AddInTitle);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(Owner(), ex.Message, AddInTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        public int CanRun() { return 1; } // 1 = enabled

        private IWin32Window Owner()
        {
            try
            {
                IFrame frame = (IFrame)_app.Frame();
                return new WindowHandle(new IntPtr(frame.GetHWndx64()));
            }
            catch
            {
                return null;
            }
        }

        private sealed class WindowHandle : IWin32Window
        {
            public WindowHandle(IntPtr handle) { Handle = handle; }
            public IntPtr Handle { get; private set; }
        }

        // ---- COM registration (regasm /codebase) -----------------------------------------------
        [ComRegisterFunction]
        public static void RegisterFunction(Type t)
        {
            string id = "{" + t.GUID.ToString().ToUpperInvariant() + "}";

            using (RegistryKey key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\SolidWorks\\Addins\\" + id))
            {
                key.SetValue(null, 0, RegistryValueKind.DWord); // 0 = not loaded at startup by default
                key.SetValue("Title", AddInTitle);
                key.SetValue("Description", AddInDescription);
            }

            // Load at SolidWorks startup for the current user.
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\SolidWorks\\AddInsStartup\\" + id))
            {
                key.SetValue(null, 1, RegistryValueKind.DWord);
            }
        }

        [ComUnregisterFunction]
        public static void UnregisterFunction(Type t)
        {
            string id = "{" + t.GUID.ToString().ToUpperInvariant() + "}";
            try { Registry.LocalMachine.DeleteSubKey("SOFTWARE\\SolidWorks\\Addins\\" + id, false); } catch { }
            try { Registry.CurrentUser.DeleteSubKey("Software\\SolidWorks\\AddInsStartup\\" + id, false); } catch { }
        }
    }
}
