using System;

namespace WinHTTPAPI
{
    public class RemoteCmdResult
    {
        public int ReturnCode { get; set; }
        public string StdOut { get; set; }
        public string StdErr { get; set; }
    }
}
