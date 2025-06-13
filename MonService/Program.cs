using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32.SafeHandles;
using Microsoft.Win32.TaskScheduler;
using Serilog;

namespace MonService
{
  class Program
  {
    private static string ProcessNameToWatch = ""; // without .exe
    private static string SelfToWatch = ""; // without .exe
    private static string ProcessPath = "";

    private static Process? process;
    private static int SW_HIDE = 1;
    private static int SW_SHOWDEFAULT = 10;
    private static int SW_MINIMIZE = 2;
    static string localPath = Path.Combine(
      Directory.GetCurrentDirectory(),
      "..",
      "LatestVersion.zip"
    );

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [STAThreadAttribute]
    static void Main(string[] args)
    {
      var handle = GetConsoleWindow();
      ShowWindow(handle, SW_MINIMIZE);
      ShowWindow(handle, SW_SHOWDEFAULT);
      ShowWindow(handle, SW_HIDE);

      var configuration = new ConfigurationBuilder()
        .SetBasePath(System.IO.Directory.GetCurrentDirectory()) //set the base directory
        .AddJsonFile("appsettings.json")
        .Build();

      var logPathFormat = configuration.GetSection("Serilog:WriteTo:0:Args:pathFormat").Value;
      var dynamicPrefix = Environment.UserName + "-umservicelogs"; // This can be any value determined at runtime
      // var newlogPathFormat = logPathFormat.Replace("{BaseFileName}", $"{dynamicPrefix}-umservicelogs");
      string userDirectory =
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + logPathFormat;

      Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .WriteTo.File(
          Path.Combine(userDirectory, $"{dynamicPrefix}.txt"),
          rollingInterval: RollingInterval.Day
        )
        .CreateLogger();
      //Read the other app settings from appsettings.json
      readAppSettings();

      //If an already existing MonService Process is running during Parallel Run
      if (!CountProcessRunning(SelfToWatch))
      {
        string userName = Environment.UserName;
        while (true)
        {
          try
          {
            if (!IsProcessRunning(ProcessNameToWatch))
            {
              RestartProcess();
              Log.Information($"Restarting the data collection process at {DateTimeOffset.Now}");
            }
            Thread.Sleep(60 * 1000);
          }
          catch (Exception ex)
          {
            Log.Error("Error in calling monitoruser", ex.StackTrace);
          }
        }
      }
    }

    static void readAppSettings()
    {
      Log.Information("Within ReadAppSettings");
      var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

      IConfigurationRoot configuration = builder.Build();

      ProcessNameToWatch = configuration["ProcessNameToWatch"];
      SelfToWatch = configuration["SelfToWatch"];
      ProcessPath = configuration["ProcessPath"];
    }

    public static void ReplaceFiles(string localPath)
    {
      string targetDirectory = Path.Combine(Directory.GetCurrentDirectory(), "..");
      string monitorUserFile = Path.Combine(targetDirectory, "MonitorUser");
      string tempExtractPath = Path.Combine(targetDirectory, "TempExtract");

      // Delete the MonitorUser file if it exists
      try
      {
        // Ensure the temporary extraction directory is clean
        if (Directory.Exists(tempExtractPath))
        {
          Directory.Delete(tempExtractPath, true);
        }

        ZipFile.ExtractToDirectory(localPath, tempExtractPath, true);

        if (Directory.Exists(monitorUserFile))
        {
          Directory.Delete(monitorUserFile, true);
          Log.Information($"File '{monitorUserFile}' deleted successfully.");
        }
        else
        {
          Log.Information($"File '{monitorUserFile}' does not exist.");
        }

        //Move the Extract folder to Destination folder
        Directory.Move(tempExtractPath, monitorUserFile);
        //ZipFile.ExtractToDirectory(localPath, monitorUserFile, true); // Overwrite files
        Log.Information($"Files extracted from {localPath} to {monitorUserFile} successfully.");

        // Delete the localpath ZIP
        File.Delete(localPath);
        Log.Information($"File '{localPath}' deleted successfully.");
      }
      catch (Exception ex)
      {
        Log.Error(
          $"Exception occurred while extracting files from {localPath} to {monitorUserFile}: {ex}"
        );
      }

      Log.Information("***********************************");
      Log.Information("Update Compeleted Successfully");
      Log.Information("***********************************");
    }

    private static void RestartProcess()
    {
      Log.Information("Restarting Process");
      try
      {
        ProcessStartInfo startInfo = new ProcessStartInfo();

        Log.Information($"Process Path : {ProcessPath}\\MonitorUser.exe");

        startInfo.FileName = Path.Combine(ProcessPath, "MonitorUser.exe"); // Specify the executable to run
        startInfo.WorkingDirectory = ProcessPath; // Set the working directory

        Log.Information(startInfo.FileName + " : " + startInfo.WorkingDirectory);

        /*                using (Process process = Process.Start(startInfo))
                        {
                            process.WaitForExit();
                        }
        */
        try
        {
          Process process = Process.Start(startInfo);
        }
        catch (Exception ex)
        {
          Log.Debug(ex.StackTrace);
        }
      }
      catch (Exception ex)
      {
        Log.Information("Exception : " + ex.StackTrace);
      }
    }

    private static bool CountProcessRunning(string processName)
    {
      var sessionId = ActiveSession.GetActiveUserSessionId();
      if (sessionId.HasValue)
      {
        Log.Information($"Active User Session ID: {sessionId.Value}");
      }
      else
      {
        Log.Information("Could not retrieve active user session ID.");
      }
      int countofProcess = 0;

      foreach (Process iprocess in Process.GetProcesses())
      {
        // Log.Information($"Process Iteration : {iprocess.ProcessName} - {iprocess.SessionId}");
        if (iprocess.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
        {
          if (iprocess.SessionId == sessionId)
          {
            Log.Information($"{processName} Process is running");
            countofProcess++;
          }
        }
      }
      if (countofProcess > 1)
      {
        return true;
      }
      return false;
    }

    private static bool IsProcessRunning(string processName)
    {
      var sessionId = ActiveSession.GetActiveUserSessionId();
      if (sessionId.HasValue)
      {
        Log.Information($"Active User Session ID: {sessionId.Value}");
      }
      else
      {
        Log.Information("Could not retrieve active user session ID.");
      }

      foreach (Process iprocess in Process.GetProcesses())
      {
        if (iprocess.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
        {
          if (iprocess.SessionId == sessionId)
          {
            process = iprocess;
            Log.Information($"{processName} Process is running");
            return true;
          }
        }
      }
      return false;
    }

    private void RestartProcess(string processPath)
    {
      Console.WriteLine("Restart Process");
      process = Process.Start(processPath);
    }

    protected static void RunScheduledTask(string taskName)
    {
      try
      {
        using (TaskService ts = new TaskService())
        {
          Microsoft.Win32.TaskScheduler.Task task = ts.GetTask(taskName);
          Log.Information("Within RunScheduledTask Task Run Task State : " + task.State);

          if (task != null && task.Enabled)
          {
            Log.Information("Within RunScheduledTask Task Run");
            task.Run();
          }
        }
      }
      catch (Exception ex)
      {
        Log.Information("Error in Stop scheduled Task " + ex.StackTrace.ToString());
      }
    }

    public void StopScheduledTask(string taskName)
    {
      try
      {
        using (TaskService ts = new TaskService())
        {
          Microsoft.Win32.TaskScheduler.Task task = ts.GetTask(taskName);
          if (
            task != null
            && task.Enabled
            && (task.State == TaskState.Running || task.State != TaskState.Queued)
          )
          {
            task.Stop();
          }
        }
      }
      catch (Exception ex)
      {
        Log.Information("Error in Stop scheduled Task " + ex.StackTrace.ToString());
      }
    }
  }

  internal class WindowsImpersonationContext { }
}
