using System.Diagnostics;
using System.Runtime.InteropServices;
using Memory;

namespace SuperZoom
{
    public partial class SZ : Form
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public Mem memory = new Mem();
        Process p;

        public bool modulesUpdated = false;

        public string mccProcessSteam = "MCC-Win64-Shipping";
        public string mccProcessWinstore = "MCCWinStore-Win64-Shipping";
        private string selectedProcessName;
        public float UserCurrentStartup;
        public string UserCurrentFOV;
        public float ModifiedFOV = 1.0f;
        public string FovAddress = "haloreach.dll+2A03D4C";

        public SZ()
        {
            InitializeComponent();
            GetProcess();
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;

        private LowLevelProc _proc;
        private IntPtr _hookIDKeyboard = IntPtr.Zero;

        private IntPtr SetHook(int hookType, LowLevelProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(hookType, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    if (key == Keys.C)
                    {
                        memory.WriteMemory(FovAddress, "float", ModifiedFOV.ToString());
                    }
                    if (key == Keys.B)
                    {
                        float IncreaseFOV = ModifiedFOV + 1.5f;
                        if (IncreaseFOV >= 150.0f)
                        {
                            IncreaseFOV = 150.0f;
                        }

                        memory.WriteMemory(FovAddress, "float", IncreaseFOV.ToString());
                        ModifiedFOV = IncreaseFOV;
                    }
                    if (key == Keys.N)
                    {
                        float DecreaseFOV = ModifiedFOV - 1.5f;
                        if (DecreaseFOV <= 1.0f)
                        {
                            DecreaseFOV = 1.0f;
                        }

                        memory.WriteMemory(FovAddress, "float", DecreaseFOV.ToString());
                        ModifiedFOV = DecreaseFOV;
                    }
                }
                else if (wParam == (IntPtr)WM_KEYUP)
                {
                    if (key == Keys.C)
                    {
                        Console.WriteLine("C key released");
                        memory.WriteMemory(FovAddress, "float", UserCurrentFOV);
                    }
                }
            }

            return CallNextHookEx(_hookIDKeyboard, nCode, wParam, lParam);
        }

        public void GetProcess()
        {
            try
            {
                Process[] processes = Process.GetProcesses();
                foreach (Process process in processes)
                {
                    if (process.ProcessName.Equals(mccProcessSteam, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedProcessName = mccProcessSteam;
                        break;
                    }
                    else if (process.ProcessName.Equals(mccProcessWinstore, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedProcessName = mccProcessWinstore;
                        break;
                    }
                }

                p = Process.GetProcessesByName(selectedProcessName)[0];
                memory.OpenProcess(p.Id);

                if (memory == null) return;
                if (memory.theProc == null) return;

                memory.theProc.Refresh();
                memory.modules.Clear();

                foreach (ProcessModule Module in memory.theProc.Modules)
                {
                    if (!string.IsNullOrEmpty(Module.ModuleName) && !memory.modules.ContainsKey(Module.ModuleName)) memory.modules.Add(Module.ModuleName, Module.BaseAddress);
                }

                UserCurrentStartup = memory.ReadFloat(FovAddress);
                UserCurrentFOV = UserCurrentStartup.ToString();

                _proc = KeyboardHookCallback;
                _hookIDKeyboard = SetHook(WH_KEYBOARD_LL, _proc);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}