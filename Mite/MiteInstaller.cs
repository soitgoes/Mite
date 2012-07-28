using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Text;

namespace Mite {
    public class MiteInstaller :Installer
    {
        public MiteInstaller()
        {
            this.AfterInstall += new InstallEventHandler(MiteInstaller_AfterInstall);
        }

        void MiteInstaller_AfterInstall(object sender, InstallEventArgs e) {
            File.WriteAllText(@"c:\temp\test.txt", "afterinstall");

        }
    }
}
