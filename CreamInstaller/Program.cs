using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace CreamInstaller
{
    public static class Program
    {
        public static readonly string ApplicationName = Application.CompanyName + " v" + Application.ProductVersion + ": " + Application.ProductName;
        public static readonly Assembly EntryAssembly = Assembly.GetEntryAssembly();
        public static readonly Process CurrentProcess = Process.GetCurrentProcess();
        public static readonly string CurrentProcessFilePath = CurrentProcess.MainModule.FileName;
        public static readonly string CurrentProcessDirectory = CurrentProcessFilePath.Substring(0, CurrentProcessFilePath.LastIndexOf("\\"));
        public static readonly string BackupFileExtension = ".creaminstaller.backup";

        [STAThread]
        private static void Main()
        {
            Mutex mutex = new(true, "CreamInstaller", out bool createdNew);
            if (createdNew)
            {
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ApplicationExit += new EventHandler(OnApplicationExit);
                retry:
                try
                {
                    Application.Run(new MainForm());
                }
                catch (Exception e)
                {
                    if (ExceptionHandler.OutputException(e)) goto retry;
                }
            }
            mutex.Close();
        }

        public static bool IsProgramRunningDialog(Form form, ProgramSelection selection)
        {
            if (selection.AreSteamApiDllsLocked)
            {
                if (new DialogForm(form).Show(ApplicationName, SystemIcons.Error,
                $"ERROR: {selection.Name} is currently running!" +
                "\n\nPlease close the program/game to continue . . . ",
                "Retry", "Cancel") == DialogResult.OK)
                {
                    return IsProgramRunningDialog(form, selection);
                }
            }
            else
            {
                return true;
            }
            return false;
        }

        public static bool IsFilePathLocked(this string filePath)
        {
            if (!File.Exists(filePath)) return false;
            bool Locked = false;
            try
            {
                File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None).Close();
            }
            catch (IOException)
            {
                Locked = true;
            }
            return Locked;
        }

        public static SelectForm SelectForm;
        public static InstallForm InstallForm;

        public static List<ProgramSelection> ProgramSelections = new();

        public static bool Canceled = false;
        public static string OutputFile = null; // placeholder, won't exist in new system
        public static ZipArchive OutputArchive = null; // placeholder, won't exist in new system

        public static void Cleanup(bool cancel = true)
        {
            Canceled = cancel;
            SteamCMD.Kill();
        }

        private static void OnApplicationExit(object s, EventArgs e) => Cleanup();
    }
}