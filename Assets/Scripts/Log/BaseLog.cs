using System.IO;

namespace Log
{
    public class BaseLog
    {
        private StreamWriter _writer;

        public BaseLog(string fileName, string logsPath)
        {
            // Create a log file with a unique name based on the current date and time
            
            // Create a new StreamWriter to write to the log file
            _writer = new StreamWriter(Path.Combine(logsPath, fileName), false);
        }

        public void Destroy()
        {
            // Close the StreamWriter when the application is closing
            try
            {
                if (_writer != null)
                {
                    _writer.Flush();
                    _writer.Close();
                    _writer.Dispose();
                }
            }
            finally
            {
                _writer = null;
            }
        }

        public void Log(string logEntry)
        {
            _writer.WriteLine(logEntry);
            _writer.Flush();
        }
    }
}