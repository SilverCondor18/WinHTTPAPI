using System;

namespace WinHTTPAPI
{
    public class RemoteCmd
    {
        public string CmdLabel { get; set; }
        public string CmdObject { get; set; }
        public string CmdArgs { get; set; }
        public TimeSpan CmdWait { get; set; }
        public long CmdWaitTicks
        {
            get
            {
                return Convert.ToInt64(CmdWait.TotalMilliseconds);
            }
            set
            {
                CmdWait = TimeSpan.FromMilliseconds(value);
            }
        }
        public RemoteCmd()
        {
            CmdWait = TimeSpan.FromMinutes(1);
        }
    }
}
