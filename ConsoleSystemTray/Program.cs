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
    static void Main(string[] args) {
      var cmds = Utils.GetCommondLines(args);
      string path = cmds.GetArgument("-p");
      string arguments = cmds.GetArgument("-a", true);
      string icon = cmds.GetArgument("-i", true);
      string baseDirectory = cmds.GetArgument("-d", true);
      string tip = cmds.GetArgument("-t", true);

      Icon trayIcon;
      if (!string.IsNullOrWhiteSpace(icon)) {
        trayIcon = new Icon(icon);
      } else {
        trayIcon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
      }

      ProcessStartInfo processStartInfo = new ProcessStartInfo() {
        FileName = path,
        ErrorDialog = true,
      };
      if (!string.IsNullOrWhiteSpace(arguments)) {
        processStartInfo.Arguments = arguments;
      }
      if (!string.IsNullOrWhiteSpace(baseDirectory)) {
        processStartInfo.WorkingDirectory = baseDirectory;
      }

      var p = Process.Start(processStartInfo);
      ExitAtSame(p);
      SwitchWindow(GetConsoleWindow());

      string trapText = !string.IsNullOrWhiteSpace(tip) ? tip : GetTrayText(p);
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
  }
}
