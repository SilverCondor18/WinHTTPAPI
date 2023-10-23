using System;
using System.IO;

namespace WinHTTPAPI
{
    public class LogManager : IDisposable
    {
        private StreamWriter sw { get; set; }

        public LogManager(FileInfo logFile)
        {
            sw = new StreamWriter(logFile.Open((logFile.Exists) ? FileMode.Append : FileMode.OpenOrCreate, FileAccess.Write));
        }

        public void WriteLog(LogRecord record)
        {
            sw.WriteLine(record.ToString());
            sw.Flush();
        }

        public void Dispose()
        {
            sw.Dispose();
        }
    }

    public class LogRecord
    {
        public DateTime dateTime { get; set; }
        public string description { get; set; }
        public string userIdentity { get; set; }
        public string requestedMethod { get; set; }
        public string requestedParams { get; set; }
        public string requestedBody { get; set; }
        public LogRecord(string description, string userIdentity, string requestedMethod, string requestedParams, string body)
        {
            dateTime = DateTime.Now;
            this.description = description;
            this.userIdentity = userIdentity;
            this.requestedMethod = requestedMethod;
            this.requestedParams = requestedParams;
            if (!string.IsNullOrEmpty(body))
            {
                requestedBody = body;
            }
            else
            {
                requestedBody = null;
            }
        }
        public override string ToString()
        {
            return String.Format("{0} :: {1} :: {2} :: {3} :: {4} :: {5}", dateTime.ToString("dd.MM.yyyy HH:mm:ss"), description, userIdentity, requestedMethod, requestedParams, (requestedBody == null) ? "null" : requestedBody);
        }
    }
}
