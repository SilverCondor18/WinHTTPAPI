using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WinHTTPAPI
{
    public class ServiceMessage
    {
        public string State { get; set; }
        public string Message { get; set; }
        public string AdditionalInformation { get; set; }

        public FilesystemObject getFSObject()
        {
            if (State == "success")
            {
                return JsonConvert.DeserializeObject<FilesystemObject>(AdditionalInformation);
            }
            else
            {
                throw new Exception(FormatExceptionMessage());
            }
        }

        public List<FilesystemObject> getFSList()
        {
            if (State == "success")
            {
                return JsonConvert.DeserializeObject<List<FilesystemObject>>(AdditionalInformation);
            }
            else
            {
                throw new Exception(FormatExceptionMessage());
            }
        }

        public RemoteCmdResult getCmdResult()
        {
            if (State == "success")
            {
                return JsonConvert.DeserializeObject<RemoteCmdResult>(AdditionalInformation);
            }
            else
            {
                throw new Exception(FormatExceptionMessage());
            }
        }

        public List<LogonUserInfo> getLogonList()
        {
            if (State == "success")
            {
                return JsonConvert.DeserializeObject<List<LogonUserInfo>>(AdditionalInformation);
            }
            else
            {
                throw new Exception(FormatExceptionMessage());
            }
        }

        public List<RemoteDiskInfo> getRemoteDiskList()
        {
            if (State == "success")
            {
                return JsonConvert.DeserializeObject<List<RemoteDiskInfo>>(AdditionalInformation);
            }
            else
            {
                throw new Exception(FormatExceptionMessage());
            }
        }

        public string FormatExceptionMessage()
        {
            if (State == "exception")
            {
                return Message + "\r\nДополнительная информация: " + AdditionalInformation;
            }
            else
            {
                return "";
            }
        }
    }
}