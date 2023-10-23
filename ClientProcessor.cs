using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Cassia;
using Newtonsoft.Json;

namespace WinHTTPAPI
{
    internal class ClientProcessor : IDisposable
    {
        private HttpListenerContext clientContext { get; set; }
        private HttpListenerRequest request { get; set; }
        private HttpListenerResponse response { get; set; }
        private string requestedMethod { get; set; }
        private ServiceMessage resultMessage = new ServiceMessage();
        private NameValueCollection requestedParams { get; set; }
        private LogManager lm { get; set; }
        private string requestBodyBuffer;
        private bool verboseLog;
        private string userName;
        public ClientProcessor(HttpListenerContext context, LogManager lm, bool verboseLog)
        {
            clientContext = context;
            request = clientContext.Request;
            response = clientContext.Response;
            response.ContentType = "application/json";
            response.ContentEncoding = Encoding.UTF8;
            requestedMethod = request.Url.Segments[1];
            requestedParams = HttpUtility.ParseQueryString(request.Url.Query);
            this.lm = lm;
            this.verboseLog = verboseLog;
        }
        public void ProcessContext()
        {
            bool isAuth = false;
            bool isUser = false;
            bool isAdmin = false;
            WindowsIdentity wi = (WindowsIdentity)clientContext.User.Identity;
            userName = wi.Name;
            if (wi.IsAuthenticated)
            {
                WindowsPrincipal wp = new WindowsPrincipal(wi);
                isAdmin = wp.IsInRole("WinHTTPAPIAdministrators");
                isUser = wp.IsInRole("WinHTTPAPIUsers");
                isAuth = isAdmin || isUser;
            }
            if (isAuth)
            {
                switch (requestedMethod)
                {
                    case "check":
                        {
                            SendCheck();
                            break;
                        }
                    case "permissions":
                        {
                            resultMessage.State = "success";
                            if (isAdmin)
                            {
                                resultMessage.Message = "WinHTTPAPI Admin";
                                resultMessage.AdditionalInformation = "admin";
                            }
                            else if (isUser)
                            {
                                resultMessage.Message = "WinHTTPAPI";
                                resultMessage.AdditionalInformation = "user";
                            }
                            else
                            {
                                PermissionsReject();
                            }
                            break;
                        }
                    case "version":
                        {
                            SendVersion();
                            break;
                        }
                    case "isotime":
                        {
                            SendIsoTime();
                            break;
                        }
                    case "hostname":
                        {
                            SendHostName();
                            break;
                        }
                    case "pathinfo":
                        {
                            SendPathInfo();
                            break;
                        }
                    case "list":
                        {
                            SendObjectsList();
                            break;
                        }
                    case "rootlist":
                        {
                            SendRootObjectsList();
                            break;
                        }
                    case "remotecmd":
                        {
                            if (isAdmin)
                            {
                                ReceiveCmd();
                            }
                            else
                            {
                                PermissionsReject();
                            }
                            break;
                        }
                    case "mkdir":
                        {
                            if (isAdmin)
                            {
                                MakeDir();
                            }
                            else
                            {
                                PermissionsReject();
                            }
                            break;
                        }
                    case "upload":
                        {
                            if (isAdmin)
                            {
                                ReceiveFile();
                            }
                            else
                            {
                                PermissionsReject();
                            }
                            break;
                        }
                    case "getfile":
                        {
                            SendFile();
                            break;
                        }
                    case "copy":
                        {
                            if (isAdmin)
                            {
                                RemoteCopy();
                            }
                            else
                            {
                                PermissionsReject();
                            }
                            break;
                        }
                    case "move":
                        {
                            if (isAdmin)
                            {
                                RemoteMove();
                            }
                            else
                            {
                                PermissionsReject();
                            }
                            break;
                        }
                    case "remove":
                        {
                            if (isAdmin)
                            {
                                RemoveObject();
                            }
                            else
                            {
                                PermissionsReject();
                            }
                            break;
                        }
                    case "servername":
                        {
                            resultMessage.State = "success";
                            resultMessage.Message = Environment.MachineName;
                            break;
                        }
                    case "logon":
                        {
                            SendLogonList();
                            break;
                        }
                    case "kickuser":
                        {
                            if (isAdmin)
                            {
                                KickUser();
                            }
                            else
                            {
                                PermissionsReject();
                            }
                            break;
                        }
                    case "listdisks":
                        {
                            ListDisks();
                            break;
                        }
                    case "reboot":
                        {
                            RebootServer();
                            break;
                        }
                    default:
                        {
                            resultMessage.State = "exception";
                            resultMessage.Message = "Undefined action";
                            resultMessage.AdditionalInformation = "Incorrect method: " + requestedMethod;
                            break;
                        }
                }
                LogRecord lr = new LogRecord(verboseLog ? JsonConvert.SerializeObject(resultMessage) : "State = " + resultMessage.State, userName, requestedMethod, request.Url.Query, verboseLog ? requestBodyBuffer : null);
                lm.WriteLog(lr);
            }
            else
            {
                resultMessage.State = "exception";
                resultMessage.Message = "Lack permissions";
                resultMessage.AdditionalInformation = "User: " + userName;
            }
        }

        private void SendCheck()
        {
            resultMessage.State = "success";
            resultMessage.Message = "Access granted";
            resultMessage.AdditionalInformation = "WinHTTPAPI v" + Assembly.GetExecutingAssembly().GetName().Version.ToString() + " (" + Environment.MachineName + ")";
        }
        private void PermissionsReject()
        {
            resultMessage.State = "exception";
            resultMessage.Message = "Do not have permissions for this action";
            resultMessage.AdditionalInformation = "User: " + userName;
        }
        private void SendLogonList()
        {
            try
            {
                List<LogonUserInfo> SessionsList = new List<LogonUserInfo>();
                ITerminalServicesManager manager = new TerminalServicesManager();
                using (ITerminalServer server = manager.GetLocalServer())
                {
                    foreach (ITerminalServicesSession session in server.GetSessions())
                    {
                        if (session.UserName != string.Empty)
                        {
                            SessionsList.Add(new LogonUserInfo()
                            {
                                SessionID = session.SessionId,
                                UserName = session.UserName,
                                ClientName = session.ClientName,
                                ClientIP = session.ClientIPAddress?.ToString(),
                                ConnectionState = Convert.ToString(session.ConnectionState)
                            });
                        }
                    }
                }
                resultMessage.State = "success";
                resultMessage.Message = "Active sessions list extracted";
                resultMessage.AdditionalInformation = JsonConvert.SerializeObject(SessionsList);
            }
            catch (Exception ex)
            {
                resultMessage.State = "exception";
                resultMessage.Message = ex.Message;
                resultMessage.AdditionalInformation = ex.StackTrace;
            }
        }
        private void KickUser()
        {
            try
            {
                string TargetUser = requestedParams["username"];
                bool SessionHasFound = false;
                if (TargetUser == null || TargetUser == string.Empty)
                {
                    throw new Exception("Undefined username");
                }
                ITerminalServicesManager manager = new TerminalServicesManager();
                using (ITerminalServer server = manager.GetLocalServer())
                {
                    foreach (ITerminalServicesSession session in server.GetSessions())
                    {
                        if (session.UserName == TargetUser)
                        {
                            SessionHasFound = true;
                            using (Process LogOff = new Process())
                            {
                                LogOff.StartInfo = new ProcessStartInfo();
                                LogOff.StartInfo.FileName = "logoff";
                                LogOff.StartInfo.UseShellExecute = false;
                                LogOff.StartInfo.Arguments = Convert.ToString(session.SessionId);
                                LogOff.Start();
                                LogOff.WaitForExit();
                            }
                            resultMessage.State = "success";
                            resultMessage.Message = "User " + session.UserName + " logoff success";
                        }
                    }
                }
                if (!SessionHasFound)
                {
                    throw new Exception("Active session has not been found ( " + TargetUser + " )");
                }
            }
            catch (Exception ex)
            {
                resultMessage.State = "exception";
                resultMessage.Message = ex.Message;
                resultMessage.AdditionalInformation = ex.StackTrace;
            }
        }
        private void RebootServer()
        {
            try
            {
                Process PsShutdown = new Process();
                PsShutdown.StartInfo = new ProcessStartInfo(@"C:\Windows\System32\shutdown.exe", "/r /t 0");
                PsShutdown.StartInfo.UseShellExecute = false;
                PsShutdown.Start();
                resultMessage.State = "success";
                resultMessage.Message = "Reboot initiated";
            }
            catch (Exception ex)
            {
                resultMessage.State = "exception";
                resultMessage.Message = ex.Message;
                resultMessage.AdditionalInformation = ex.StackTrace;
            }
        }

        private void ListDisks()
        {
            try
            {
                resultMessage.State = "success";
                resultMessage.Message = "Disks info extracted";
                resultMessage.AdditionalInformation = JsonConvert.SerializeObject(RemoteDiskInfo.FromLocalDrives());
            }
            catch (Exception ex)
            {
                resultMessage.State = "exception";
                resultMessage.Message = ex.Message;
                resultMessage.AdditionalInformation = ex.StackTrace;
            }
        }
        private void ReceiveFile()
        {
            try
            {
                string path = requestedParams["path"];
                if (string.IsNullOrEmpty(path))
                {
                    throw new Exception("Undefined destination path");
                }
                FileInfo pathFileInfo = new FileInfo(path);
                using (Stream dest = pathFileInfo.Open(((pathFileInfo.Exists) ? FileMode.Truncate : FileMode.OpenOrCreate), FileAccess.Write))
                using (Stream source = request.InputStream)
                {
                    source.CopyTo(dest);
                }
                pathFileInfo.Refresh();
                resultMessage.State = "success";
                resultMessage.Message = "File accepted";
                resultMessage.AdditionalInformation = JsonConvert.SerializeObject(FilesystemObject.From(pathFileInfo));
            }
            catch (Exception ex)
            {
                resultMessage.State = "exception";
                resultMessage.Message = ex.Message;
                resultMessage.AdditionalInformation = ex.StackTrace;
            }
        }

        private void RemoveObject()
        {
            try
            {
                string path = requestedParams["path"];
                FilesystemObject obj = FilesystemObject.From(path);
                if (obj.ObjectType == FilesystemObjectType.Unknown)
                {
                    throw new FileNotFoundException();
                }
                else
                {
                    obj.Remove();
                    resultMessage.State = "success";
                    resultMessage.Message = "Object has deleted";
                    resultMessage.AdditionalInformation = "Path: " + path + "\r\nObject type: " + obj.ObjectType;
                }
            }
            catch (Exception ex)
            {
                resultMessage.State = "exception";
                resultMessage.Message = ex.Message;
                resultMessage.AdditionalInformation = ex.StackTrace;
            }
        }
        private void ReceiveCmd()
        {
            try
            {
                RemoteCmd cmd;
                using (StreamReader sr = new StreamReader(request.InputStream, Encoding.UTF8))
                {
                    requestBodyBuffer = sr.ReadToEnd();
                    cmd = JsonConvert.DeserializeObject<RemoteCmd>(requestBodyBuffer);
                }
                if (cmd != null)
                {
                    Process CmdExec = new Process();
                    CmdExec.StartInfo = new ProcessStartInfo();
                    StringBuilder CmdOutputBuilder = new StringBuilder();
                    StringBuilder CmdErrorBuilder = new StringBuilder();
                    CmdExec.StartInfo.FileName = "cmd.exe";
                    CmdExec.StartInfo.Arguments = "/C \"" + cmd.CmdObject;
                    if (!string.IsNullOrEmpty(cmd.CmdArgs))
                    {
                        CmdExec.StartInfo.Arguments += " " + cmd.CmdArgs;
                    }
                    CmdExec.StartInfo.Arguments += "\"";
                    CmdExec.StartInfo.UseShellExecute = false;
                    CmdExec.StartInfo.RedirectStandardOutput = true;
                    CmdExec.StartInfo.RedirectStandardError = true;
                    CmdExec.StartInfo.StandardOutputEncoding = Encoding.GetEncoding(866);
                    CmdExec.StartInfo.StandardErrorEncoding = CmdExec.StartInfo.StandardOutputEncoding;
                    CmdExec.Start();
                    CmdOutputBuilder.Append(CmdExec.StandardOutput.ReadToEnd());
                    CmdErrorBuilder.Append(CmdExec.StandardError.ReadToEnd());
                    if (CmdExec.WaitForExit(Convert.ToInt32(cmd.CmdWait.TotalMilliseconds)))
                    {
                        resultMessage.State = "success";
                        RemoteCmdResult cmdResult = new RemoteCmdResult();
                        cmdResult.StdErr = CmdErrorBuilder.ToString();
                        cmdResult.StdOut = CmdOutputBuilder.ToString();
                        cmdResult.ReturnCode = CmdExec.ExitCode;
                        resultMessage.Message = "Command executed";
                        resultMessage.AdditionalInformation = JsonConvert.SerializeObject(cmdResult);
                    }
                    else
                    {
                        CmdExec.Kill();
                        throw new Exception("Process time exceeded");
                    }

                }
                else
                {
                    throw new Exception("Command has not received");
                }
            }
            catch (Exception ex)
            {
                resultMessage.State = "exception";
                resultMessage.Message = ex.Message;
                resultMessage.AdditionalInformation = ex.StackTrace;
            }
        }

        private void SendIsoTime()
        {
            try
            {
                string isoTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                resultMessage.State = "success";
                resultMessage.Message = "Server time extracted";
                resultMessage.AdditionalInformation = isoTime;
            }
            catch (Exception ex)
            {
                resultMessage.State = "exception";
                resultMessage.Message = ex.Message;
                resultMessage.AdditionalInformation = ex.StackTrace;
            }
        }

        private void SendHostName()
        {
            try
            {
                string hostName = Environment.MachineName;
                resultMessage.State = "success";
                resultMessage.Message = hostName;
            }
            catch (Exception ex)
            {
                resultMessage.State = "exception";
                resultMessage.Message = ex.Message;
                resultMessage.AdditionalInformation = ex.StackTrace;
            }
        }

        private void SendVersion()
        {
            resultMessage.State = "success";
            resultMessage.Message = "Service version extracted";
            resultMessage.AdditionalInformation = "WinHTTPAPI v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private void SendObjectsList()
        {
            try
            {
                string dir = requestedParams["path"];
                bool calcHash = Convert.ToBoolean(requestedParams["calchash"]);
                bool noDirSize = Convert.ToBoolean(requestedParams["nodirsize"]);
                DirectoryInfo di = new DirectoryInfo(dir);
                if (!di.Exists)
                {
                    throw new Exception("Object is not exists or not directory");
                }
                else
                {
                    List<FilesystemObject> objList = new List<FilesystemObject>();
                    foreach (DirectoryInfo ldi in di.GetDirectories())
                    {
                        objList.Add(FilesystemObject.From(ldi, !noDirSize));
                    }
                    foreach (FileInfo fi in di.GetFiles())
                    {
                        objList.Add(FilesystemObject.From(fi, calcHash));
                    }
                    resultMessage.State = "success";
                    resultMessage.Message = "Object list extracted";
                    resultMessage.AdditionalInformation = JsonConvert.SerializeObject(objList);
                }
            }
            catch (Exception ex)
            {
                resultMessage.State = "exception";
                resultMessage.Message = ex.Message;
                resultMessage.AdditionalInformation = ex.StackTrace;
            }
        }

        private void SendRootObjectsList()
        {
            try
            {
                List<FilesystemObject> rootList = new List<FilesystemObject>();

                foreach (DriveInfo di in DriveInfo.GetDrives())
                {
                    if (di.IsReady)
                    {
                        FilesystemObject diskAsDir = FilesystemObject.From(new DirectoryInfo(di.Name), false);
                        diskAsDir.Size = di.AvailableFreeSpace;
                        rootList.Add(diskAsDir);
                    }
                }
                resultMessage.State = "success";
                resultMessage.Message = "Object list extracted";
                resultMessage.AdditionalInformation = JsonConvert.SerializeObject(rootList);
            }
            catch (Exception ex)
            {
                resultMessage.State = "exception";
                resultMessage.Message = ex.Message;
                resultMessage.AdditionalInformation = ex.StackTrace;
            }
        }
        private void SendPathInfo()
        {
            try
            {
                string path = requestedParams["path"];
                bool calcHash = Convert.ToBoolean(requestedParams["calchash"]);
                FileInfo fi = new FileInfo(path);

                if (fi.Exists)
                {
                    resultMessage.AdditionalInformation = JsonConvert.SerializeObject(FilesystemObject.From(fi, calcHash));
                }
                else if (Directory.Exists(path))
                {
                    resultMessage.AdditionalInformation = JsonConvert.SerializeObject(FilesystemObject.From(new DirectoryInfo(path)));
                }
                else
                {
                    resultMessage.AdditionalInformation = JsonConvert.SerializeObject(FilesystemObject.From(fi, calcHash));
                }
                resultMessage.State = "success";
                resultMessage.Message = "Path info extracted";
            }
            catch (Exception ex)
            {
                resultMessage.State = "exception";
                resultMessage.Message = ex.Message;
                resultMessage.AdditionalInformation = ex.StackTrace;
            }
        }

        private void MakeDir()
        {
            try
            {
                string path = requestedParams["path"];
                DirectoryInfo di = new DirectoryInfo(path);
                if (!di.Exists)
                {
                    di.Create();
                }
                di.Refresh();
                FilesystemObject dirInfo = FilesystemObject.From(di);
                resultMessage.State = "success";
                resultMessage.Message = "Directory created";
                resultMessage.AdditionalInformation = JsonConvert.SerializeObject(dirInfo);
            }
            catch (Exception ex)
            {
                resultMessage.State = "exception";
                resultMessage.Message = ex.Message;
                resultMessage.AdditionalInformation = ex.StackTrace;
            }
        }

        private void SendFile()
        {
            try
            {
                string path = requestedParams["path"];
                FileInfo fi = new FileInfo(path);
                if (fi.Exists)
                {
                    response.ContentType = "application/octet-stream";
                    response.ContentLength64 = fi.Length;
                    response.AddHeader("Content-Description", "File Transfer");
                    response.AddHeader("Cache-Control", "no-cache, must-revalidate");
                    response.AddHeader("Expires", "0");
                    response.AddHeader("Content-disposition", "attachment; filename=\"" + fi.Name + "\"");
                    using (Stream source = fi.OpenRead())
                    using (Stream dest = response.OutputStream)
                    {
                        source.CopyTo(dest);
                    }
                    resultMessage.State = null;
                }
                else
                {
                    throw new FileNotFoundException();
                }
            }
            catch (Exception ex)
            {
                resultMessage.State = "exception";
                resultMessage.Message = ex.Message;
                resultMessage.AdditionalInformation = ex.StackTrace;
            }
        }

        private void RemoteCopy()
        {
            try
            {
                string source = requestedParams["src"];
                string dest = requestedParams["dest"];
                bool overwrite = Convert.ToBoolean(requestedParams["overwrite"]);
                FilesystemObject srcObject;
                if (Directory.Exists(source))
                {
                    srcObject = FilesystemObject.From(new DirectoryInfo(source));
                }
                else if (File.Exists(source))
                {
                    srcObject = FilesystemObject.From(new FileInfo(source));
                }
                else
                {
                    throw new FileNotFoundException();
                }
                srcObject.CopyTo(dest, overwrite);
                resultMessage.State = "success";
                resultMessage.Message = "Object has copied";
                resultMessage.AdditionalInformation = "Source path: " + source + "\r\nDestination path: " + dest;
            }
            catch (Exception ex)
            {
                resultMessage.State = "exception";
                resultMessage.Message = ex.Message;
                resultMessage.AdditionalInformation = ex.StackTrace;
            }
        }

        private void RemoteMove()
        {
            try
            {
                string source = requestedParams["src"];
                string dest = requestedParams["dest"];
                FilesystemObject srcObject;
                if (File.Exists(source))
                {
                    srcObject = FilesystemObject.From(new FileInfo(source));
                }
                else if (Directory.Exists(source))
                {
                    srcObject = FilesystemObject.From(new DirectoryInfo(source));
                }
                else
                {
                    throw new FileNotFoundException();
                }
                srcObject.MoveTo(dest);
                resultMessage.State = "success";
                resultMessage.Message = "Object has moved";
                resultMessage.AdditionalInformation = JsonConvert.SerializeObject(srcObject);
            }
            catch (Exception ex)
            {
                resultMessage.State = "exception";
                resultMessage.Message = ex.Message;
                resultMessage.AdditionalInformation = ex.StackTrace;
            }
        }
        public void Dispose()
        {
            if (!string.IsNullOrEmpty(resultMessage.State))
            {
                byte[] responseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resultMessage));
                response.ContentType = "application/json";
                response.ContentLength64 = responseBytes.Length;
                using (Stream s = response.OutputStream)
                {
                    s.Write(responseBytes, 0, responseBytes.Length);
                }
            }
            response.Close();
        }
    }
}
