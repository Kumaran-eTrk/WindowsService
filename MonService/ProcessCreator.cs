using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MonService
{
    public class ProcessCreator
    {
        const int TOKEN_DUPLICATE = 0x0002;
        const int TOKEN_QUERY = 0x0008;
        const uint MAXIMUM_ALLOWED = 0x2000000;
        const int NORMAL_PRIORITY_CLASS = 0x20;

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool DuplicateTokenEx(IntPtr ExistingTokenHandle, uint dwDesiredAccess, IntPtr lpTokenAttributes, int ImpersonationLevel, int TokenType, out IntPtr DuplicateTokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("Wtsapi32.dll")]
        public static extern bool WTSQueryUserToken(uint sessionId, out IntPtr Token);

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        public ProcessCreator()
        {

        }

        SimpleLogger logger;
        public ProcessCreator(SimpleLogger logger)
        {
            this.logger = logger;
        }

        PROCESS_INFORMATION pi;

        public void StopProcess()
        {
            if (pi.dwProcessId <= 0) return;

            try
            {
                var process = Process.GetProcessById(pi.dwProcessId);
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
            catch (ArgumentException)
            {
                // Process not found
                logger.Log("Process is not existing...");
            }
            catch (Exception ex)
            {
                // Handle other exceptions if necessary
                logger.Log("An error occurred: " + ex.Message);

            }
        }

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        public void StartProcessAsCurrentUser(string appPath, string cmdLine = null, string workDir = null, bool visible = true)
        {
            IntPtr hToken = IntPtr.Zero;
            IntPtr hDupedToken = IntPtr.Zero;

            // Fetch the active session
            uint dwSessionId = WTSGetActiveConsoleSessionId();
            Console.WriteLine("DW Session ID " + dwSessionId);
            // Obtain the user token for the session
            if (!WTSQueryUserToken(dwSessionId, out hToken))
            {
                int errCode = (int)GetLastError();
                throw new System.ComponentModel.Win32Exception(errCode);
            }
            else
            {
                Console.WriteLine("FKed up");
            }
            try
            {
                // Duplicate the user token to create a new process
                if (!DuplicateTokenEx(hToken, MAXIMUM_ALLOWED, IntPtr.Zero, 2, 1, out hDupedToken))
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }

                var si = new STARTUPINFO();
                si.cb = Marshal.SizeOf(si);
                si.lpDesktop = @"winsta0\default"; // interactive window station

                pi = new PROCESS_INFORMATION();

                if (!CreateProcessAsUser(hDupedToken, appPath, cmdLine, IntPtr.Zero, IntPtr.Zero, false, NORMAL_PRIORITY_CLASS, IntPtr.Zero, workDir, ref si, out pi))
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            catch(Exception ex)
            {
                logger.Log("Ecception " + ex.StackTrace);
            }
            finally
            {
                if (hToken != IntPtr.Zero)
                {
                    CloseHandle(hToken);
                }
                if (hDupedToken != IntPtr.Zero)
                {
                    CloseHandle(hDupedToken);
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);
    }

}

