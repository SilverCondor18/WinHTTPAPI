using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace WinHTTPAPI
{
    [RunInstaller(true)]
    public partial class Installer1 : System.Configuration.Install.Installer
    {
        ServiceProcessInstaller ProcInstaller;
        ServiceInstaller ServInstaller;
        public Installer1()
        {
            InitializeComponent();
            ProcInstaller = new ServiceProcessInstaller();
            ProcInstaller.Account = ServiceAccount.LocalSystem;
            ServInstaller = new ServiceInstaller();
            ServInstaller.ServiceName = "WinHTTPAPI";
            ServInstaller.DisplayName = "WinHTTPAPI";
            ServInstaller.StartType = ServiceStartMode.Automatic;
            Installers.Add(ProcInstaller);
            Installers.Add(ServInstaller);
        }
    }
}
