
using System;
using System.IO;

namespace MonService
{

    public class SimpleLogger
    {
        private readonly string _logFilePath;

        public SimpleLogger(string logFilePath)
        {
            _logFilePath = logFilePath;
        }

        public void Log(string message)
        {
            try
            {
                string logMessage = $"{DateTime.Now}: {message}";
                File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
            } catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

}
