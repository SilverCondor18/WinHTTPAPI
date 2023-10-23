using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace WinHTTPAPI
{
    internal class WinHTTPAPIConfig
    {
        public string LogDestinationFile { get; set; }
        public bool VerboseLog { get; set; }

        public void SaveToFile(FileInfo fi)
        {
            using (StreamWriter sw = new StreamWriter(fi.Open((fi.Exists) ? FileMode.Truncate : FileMode.OpenOrCreate, FileAccess.Write), Encoding.UTF8))
            {
                sw.Write(JsonConvert.SerializeObject(this));
            }
        }

        public static readonly WinHTTPAPIConfig Default = new WinHTTPAPIConfig()
        {
            LogDestinationFile = @"C:\WinHTTPAPI\de40.log",
            VerboseLog = false
        };

        public static WinHTTPAPIConfig FromFile(FileInfo fi)
        {
            using (StreamReader sr = new StreamReader(fi.OpenRead(), Encoding.UTF8))
            {
                return JsonConvert.DeserializeObject<WinHTTPAPIConfig>(sr.ReadToEnd());
            }
        }
    }
}
