using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MonService
{

    public class ActiveSession
    {
        const int WTS_CURRENT_SESSION = -1;

        [DllImport("Wtsapi32.dll")]
        public static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WTS_INFO_CLASS wtsInfoClass, out IntPtr ppBuffer, out uint pBytesReturned);

        [DllImport("Wtsapi32.dll")]
        public static extern void WTSFreeMemory(IntPtr pointer);

        public enum WTS_INFO_CLASS
        {
            WTSInitialProgram,
            WTSApplicationName,
            WTSWorkingDirectory,
            WTSOEMId,
            WTSSessionId,
            WTSUserName,
            WTSWinStationName,
            WTSDomainName,
            WTSConnectState,
            WTSClientBuildNumber,
            WTSClientName,
            WTSClientDirectory,
            WTSClientProductId,
            WTSClientHardwareId,
            WTSClientAddress,
            WTSClientDisplay,
            WTSClientProtocolType
        }

        public static int? GetActiveUserSessionId()
        {
            IntPtr buffer = IntPtr.Zero;
            uint bytesReturned;

            if (WTSQuerySessionInformation(IntPtr.Zero, WTS_CURRENT_SESSION, WTS_INFO_CLASS.WTSSessionId, out buffer, out bytesReturned))
            {
                int sessionId = Marshal.ReadInt32(buffer);
                WTSFreeMemory(buffer);
                return sessionId;
            }
            return null;
        }
    }
}