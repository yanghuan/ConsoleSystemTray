using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ConsoleSystemTray {
  class Program {
    private const string kHelpCmdString = @"Usage: ConsoleSystemTray.exe [-p filePath]
Arguments 
-p              : the application or document to start

Options
-a              : sets the set of command-line arguments to use when starting the application
-d              : sets the working directory for the process to be started
-i              : sets the current icon of tray
-m              : start minimized
-t              : sets the ToolTip text of tray
-s              : prevent Windows OS from entering Sleep mode
-h              : show the help message and exit
";

    static void Main(string[] args) {
      if (args.Length > 0) {
        try {
          var cmds = Utils.GetCommondLines(args);
          if (cmds.ContainsKey("-h")) {
            ShowHelpInfo();
            return;
          }

          var options = new Options()
          {
            Path = cmds.GetArgument("-p"),
            Arguments = cmds.GetArgument("-a", true),
            BaseDirectory = cmds.GetArgument("-d", true),
            Icon = cmds.GetArgument("-i", true),
            Tip = cmds.GetArgument("-t", true),
            IsPreventSleep = cmds.ContainsKey("-s"),
            IsStartMinimized = cmds.ContainsKey("-m")
          };
          Run(options);
        } catch (CmdArgumentException e) {
          Console.Error.WriteLine(e.Message);
          ShowHelpInfo();
          Environment.ExitCode = -1;
        } catch (Exception e) {
          Console.Error.WriteLine(e.ToString());
          Environment.ExitCode = -1;
        }
      } else {
        ShowHelpInfo();
        Environment.ExitCode = -1;
      }
    }


    private static void ShowHelpInfo() {
      Console.Error.WriteLine(kHelpCmdString);
    }

    private static void Run(Options options) {
      Icon trayIcon;
      if (!string.IsNullOrEmpty(options.Icon)) {
        trayIcon = new Icon(options.Icon);
      } else {
        trayIcon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
      }

      ProcessStartInfo processStartInfo = new ProcessStartInfo() {
        FileName = options.Path,
        ErrorDialog = true,
      };
      if (!string.IsNullOrEmpty(options.Arguments)) {
        processStartInfo.Arguments = options.Arguments;
      }
      if (!string.IsNullOrEmpty(options.BaseDirectory)) {
        processStartInfo.WorkingDirectory = options.BaseDirectory;
      }

      var p = Process.Start(processStartInfo);
      ExitAtSame(p);
      SwitchWindow(GetConsoleWindow());

      if (options.IsPreventSleep) {
        SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED);
      }

      string trapText = !string.IsNullOrEmpty(options.Tip) ? options.Tip : GetTrayText(p);
      NotifyIcon tray = new NotifyIcon {
        Icon = trayIcon,
        Text = trapText,
        BalloonTipTitle = trapText,
        BalloonTipText = trapText,
        Visible = true,
      };
      tray.MouseDoubleClick += (s, e) => {
        SwitchWindow(p.MainWindowHandle);
      };
      if (options.IsStartMinimized) {
        SwitchWindow(p.MainWindowHandle);
      }
      Application.Run();
    }

    private static string GetTrayText(Process p) {
      const int kMaxCount = 63;
      Thread.Sleep(500);  // make MainWindowTitle not empty 
      string title = p.MainWindowTitle; ;
      if (title.Length > kMaxCount) {
        title = title.Substring(title.Length - kMaxCount);
      }
      return title;
    }

    private static void ExitProcess(Process p) {
      try {
        p.Kill();
      } catch {
      }
    }

    private static void ExitAtSame(Process p) {
      p.EnableRaisingEvents = true;
      p.Exited += (s, e) => {
        Environment.Exit(0);
      };
      AppDomain.CurrentDomain.ProcessExit += (s, e) => {
        ExitProcess(p);
      };
    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    private static void SwitchWindow(IntPtr hWnd) {
      bool success = ShowWindow(hWnd, SW_HIDE);
      if (!success) {
        ShowWindow(hWnd, SW_SHOW);
      }
    }

    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll")]
    static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

    [FlagsAttribute]
    public enum EXECUTION_STATE : uint {
      ES_AWAYMODE_REQUIRED = 0x00000040,
      ES_CONTINUOUS = 0x80000000,
      ES_DISPLAY_REQUIRED = 0x00000002,
      ES_SYSTEM_REQUIRED = 0x00000001
    }
  }
}
