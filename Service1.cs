using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinHTTPAPI
{
    public partial class Service1 : ServiceBase
    {
        private static HttpListener apiListener { get; set; }
        private static WinHTTPAPIConfig config { get; set; }
        private static LogManager lm { get; set; }
        private static Thread ServiceMainThread = new Thread(new ThreadStart(() =>
        {
            while (!IsStopped)
            {
                try
                {
                    HttpListenerContext ctx = apiListener.GetContext();
                    new Thread(new ThreadStart(() =>
                    {
                        try
                        {
                            using (ClientProcessor cp = new ClientProcessor(ctx, lm, config.VerboseLog))
                            {
                                cp.ProcessContext();
                            }
                        }
                        catch (Exception ex)
                        {
                            if (lm != null && !IsStopped)
                            {
                                LogRecord lr = new LogRecord(ex.Message, ctx?.User.Identity.Name, ctx?.Request.Url.Segments[1], null, null);
                                lm.WriteLog(lr);
                            }
                        }
                    })).Start();
                }
                catch
                { }
            }
        }));
        private static bool IsStopped = false;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            DirectoryInfo di = new DirectoryInfo(@"C:\WinHTTPAPI");
            if (!di.Exists)
            {
                di.Create();
            }
            FileInfo fi = new FileInfo(Path.Combine(di.FullName, "whapi.conf"));
            if (!fi.Exists)
            {
                config = WinHTTPAPIConfig.Default;
                config.SaveToFile(fi);
            }
            else
            {
                config = WinHTTPAPIConfig.FromFile(fi);
            }
            lm = new LogManager(new FileInfo(config.LogDestinationFile));
            apiListener = new HttpListener();
            apiListener.AuthenticationSchemes = AuthenticationSchemes.Negotiate;
            apiListener.Prefixes.Add("http://*:2950/");
            apiListener.Start();
            ServiceMainThread.Start();
        }

        protected override void OnStop()
        {
            IsStopped = true;
            apiListener.Stop();
            ServiceMainThread.Abort();
            lm.Dispose();
        }
    }
}
