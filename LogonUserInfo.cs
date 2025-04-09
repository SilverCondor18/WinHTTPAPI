using System;

namespace WinHTTPAPI
{
    public class LogonUserInfo
    {
        public string UserName { get; set; }
        public string ClientName { get; set; }
        public int ClientBuild {  get; set; }
        public string ClientIP { get; set; }
        public string ConnectionState { get; set; }
        public int SessionID { get; set; }

        public override string ToString()
        {
            return UserName + " : " + ClientName + " : " + ConnectionState;
        }
    }
}