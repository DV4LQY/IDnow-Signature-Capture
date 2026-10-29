using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace IDNowWacomBridge
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try
            {
                bool createdNew;
                using (Mutex instanceMutex = new Mutex(true, @"Local\IDNowWacomBridge-v1", out createdNew))
                {
                    if (!createdNew)
                    {
                        MessageBox.Show(
                            "The IDNow Wacom STU-430 bridge is already running.",
                            "IDNow Wacom Bridge",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        return;
                    }

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new BridgeContext());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "The IDNow Wacom STU-430 bridge could not start.\r\n\r\n" + ex.Message,
                    "IDNow Wacom Bridge",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }

    internal sealed class BridgeContext : ApplicationContext
    {
        private readonly NotifyIcon notifyIcon;
        private SynchronizationContext uiContext;
        private KeyboardHook keyboardHook;
        private bool initialized;
        private int captureInProgress;

        internal BridgeContext()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Capture signature now", null, delegate { BeginCapture(FindIdNowWindow()); });
            menu.Items.Add("Open signature folder", null, delegate { OpenSignatureFolder(); });
            menu.Items.Add("Exit bridge", null, delegate { ExitThread(); });

            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "IDNow Wacom STU-430 bridge — press F6 in IDNow",
                Visible = true,
                ContextMenuStrip = menu
            };
            notifyIcon.DoubleClick += delegate { BeginCapture(FindIdNowWindow()); };

            Application.Idle += InitializeBridge;
        }

        private void InitializeBridge(object sender, EventArgs e)
        {
            Application.Idle -= InitializeBridge;
            try
            {
                uiContext = SynchronizationContext.Current;
                if (uiContext == null)
                {
                    throw new InvalidOperationException("Windows Forms synchronization context was not initialized.");
                }

                keyboardHook = new KeyboardHook();
                keyboardHook.F6PressedInIdNow += delegate(IntPtr idNowWindow)
                {
                    uiContext.Post(delegate(object _) { BeginCapture(idNowWindow); }, null);
                };
                keyboardHook.Start();
                initialized = true;

                ShowStatus("Bridge active", "In IDNow, select the signature panel and press F6 to capture from the Wacom STU-430.");
                MessageBox.Show(
                    "The STU-430 bridge is active.\r\n\r\n" +
                    "Leave it running, return to IDNow, select the signature panel, and press F6.\r\n\r\n" +
                    "The bridge and IDNow must run at the same Windows privilege level.",
                    "IDNow Wacom Bridge",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "The IDNow Wacom STU-430 bridge could not start.\r\n\r\n" + ex.Message,
                    "IDNow Wacom Bridge",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                ExitThread();
            }
        }

        private void BeginCapture(IntPtr idNowWindow)
        {
            if (!initialized)
            {
                ShowStatus("Bridge starting", "Please wait for the bridge-active confirmation before starting a capture.");
                return;
            }

            if (idNowWindow == IntPtr.Zero)
            {
                ShowStatus("IDNow not found", "Open IDNow first, then press F6 while its window is active.");
                return;
            }

            if (Interlocked.Exchange(ref captureInProgress, 1) != 0)
            {
                return;
            }

            string captureExe = CaptureExecutablePath();
            if (!File.Exists(captureExe))
            {
                Interlocked.Exchange(ref captureInProgress, 0);
                ShowStatus("Capture utility missing", captureExe);
                return;
            }

            string outputFolder = SignatureOutputFolder();
            DateTime startedUtc = DateTime.UtcNow;

            try
            {
                Directory.CreateDirectory(outputFolder);
                ProcessStartInfo startInfo = new ProcessStartInfo(captureExe)
                {
                    WorkingDirectory = Path.GetDirectoryName(captureExe),
                    UseShellExecute = false
                };
                startInfo.EnvironmentVariables["IDNOW_WACOM_OUTPUT"] = outputFolder;
                startInfo.EnvironmentVariables["IDNOW_WACOM_OWNER_HANDLE"] =
                    idNowWindow.ToInt64().ToString(CultureInfo.InvariantCulture);
                startInfo.EnvironmentVariables["IDNOW_WACOM_AUTOSTART"] = "1";

                Process captureProcess = Process.Start(startInfo);
                if (captureProcess == null)
                {
                    throw new InvalidOperationException("Windows did not start the STU-430 capture utility.");
                }

                ShowStatus("Wacom capture started", "Sign on the STU-430 and tap OK on the pad.");
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        captureProcess.WaitForExit();
                        string savedSignature = FindLatestSignature(outputFolder, startedUtc.AddSeconds(-2));
                        uiContext.Post(delegate(object _)
                        {
                            ActivateIdNow(idNowWindow);
                            if (savedSignature == null)
                            {
                                ShowStatus("Capture cancelled", "No new signature BMP was produced.");
                            }
                            else
                            {
                                ShowStatus("Signature ready", "The BMP is on the clipboard. In IDNow, right-click the selected signature panel and choose Paste.");
                            }
                            Interlocked.Exchange(ref captureInProgress, 0);
                        }, null);
                    }
                    catch (Exception ex)
                    {
                        uiContext.Post(delegate(object _)
                        {
                            ShowStatus("Capture bridge error", ex.Message);
                            Interlocked.Exchange(ref captureInProgress, 0);
                        }, null);
                    }
                });
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref captureInProgress, 0);
                ShowStatus("Capture bridge error", ex.Message);
            }
        }

        private static IntPtr FindIdNowWindow()
        {
            Process[] processes = Process.GetProcessesByName("IDNow");
            foreach (Process process in processes)
            {
                try
                {
                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return process.MainWindowHandle;
                    }
                }
                catch (InvalidOperationException)
                {
                    // A process closed during enumeration.
                }
            }
            return IntPtr.Zero;
        }

        private static string CaptureExecutablePath()
        {
            string configured = Environment.GetEnvironmentVariable("IDNOW_WACOM_CAPTURE_EXE");
            if (!string.IsNullOrEmpty(configured))
            {
                return configured;
            }

            string workRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
            return Path.Combine(workRoot, "stu-sdk-samples", "extra samples", "csharp", "IDNowWacomCapture", "bin", "Release", "IDNowWacomCapture.exe");
        }

        private static string SignatureOutputFolder()
        {
            string configured = Environment.GetEnvironmentVariable("IDNOW_WACOM_OUTPUT");
            if (!string.IsNullOrEmpty(configured))
            {
                return configured;
            }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "IDNow Signatures");
        }

        private static string FindLatestSignature(string folder, DateTime notOlderThanUtc)
        {
            if (!Directory.Exists(folder))
            {
                return null;
            }

            string candidate = null;
            DateTime latest = DateTime.MinValue;
            foreach (string file in Directory.GetFiles(folder, "IDNow_Signature_*.bmp"))
            {
                DateTime timestamp = File.GetLastWriteTimeUtc(file);
                if (timestamp >= notOlderThanUtc && timestamp > latest)
                {
                    latest = timestamp;
                    candidate = file;
                }
            }
            return candidate;
        }

        private static void ActivateIdNow(IntPtr idNowWindow)
        {
            if (idNowWindow == IntPtr.Zero)
            {
                return;
            }
            NativeMethods.SetForegroundWindow(idNowWindow);
        }

        private static void OpenSignatureFolder()
        {
            string folder = SignatureOutputFolder();
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
        }

        private void ShowStatus(string title, string message)
        {
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.BalloonTipText = message;
            notifyIcon.ShowBalloonTip(5000);
        }

        protected override void ExitThreadCore()
        {
            Application.Idle -= InitializeBridge;
            if (keyboardHook != null)
            {
                keyboardHook.Dispose();
            }
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            base.ExitThreadCore();
        }
    }

    internal sealed class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int VK_F6 = 0x75;

        private NativeMethods.LowLevelKeyboardProc callback;
        private IntPtr hookHandle;

        internal event Action<IntPtr> F6PressedInIdNow;

        internal void Start()
        {
            callback = HookCallback;
            IntPtr module = NativeMethods.GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName);
            hookHandle = NativeMethods.SetWindowsHookEx(WH_KEYBOARD_LL, callback, module, 0);
            if (hookHandle == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to install the IDNow F6 keyboard hook.");
            }
        }

        private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0 && (wParam.ToInt32() == WM_KEYDOWN || wParam.ToInt32() == WM_SYSKEYDOWN))
            {
                int virtualKey = Marshal.ReadInt32(lParam);
                if (virtualKey == VK_F6)
                {
                    IntPtr idNowWindow = ForegroundIdNowWindow();
                    if (idNowWindow != IntPtr.Zero)
                    {
                        Action<IntPtr> handler = F6PressedInIdNow;
                        if (handler != null)
                        {
                            handler(idNowWindow);
                        }
                        return new IntPtr(1);
                    }
                }
            }
            return NativeMethods.CallNextHookEx(hookHandle, code, wParam, lParam);
        }

        private static IntPtr ForegroundIdNowWindow()
        {
            IntPtr window = NativeMethods.GetForegroundWindow();
            if (window == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            uint processId;
            NativeMethods.GetWindowThreadProcessId(window, out processId);
            try
            {
                Process process = Process.GetProcessById((int)processId);
                return string.Equals(process.ProcessName, "IDNow", StringComparison.OrdinalIgnoreCase) ? window : IntPtr.Zero;
            }
            catch (ArgumentException)
            {
                return IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            if (hookHandle != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(hookHandle);
                hookHandle = IntPtr.Zero;
            }
            callback = null;
        }
    }

    internal static class NativeMethods
    {
        internal delegate IntPtr LowLevelKeyboardProc(int code, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr module, uint threadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnhookWindowsHookEx(IntPtr hookHandle);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr CallNextHookEx(IntPtr hookHandle, int code, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr window);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(string moduleName);
    }
}
