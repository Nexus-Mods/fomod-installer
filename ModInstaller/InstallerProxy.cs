using FomodInstaller.Interface;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FomodInstaller.ModInstaller
{
    public class InstallerProxy
    {
        private Installer mInstaller;

        public InstallerProxy()
        {
            mInstaller = new Installer();
        }

        public async Task<object> TestSupported(dynamic input)
        {
            object[] files = (object[])input.files;
            List<string> allowedTypes;
            try
            {
                allowedTypes = new List<string>(((object[])input.allowedTypes).Cast<string>());
            } catch (RuntimeBinderException)
            {
                allowedTypes = new List<string>{ "XmlScript", "CSharpScript", "Basic" };
            }
            return await mInstaller.TestSupported(new List<string>(files.Cast<string>()), allowedTypes);
        }

        public async Task<object> Install(dynamic input)
        {
            object[] files = (object[])input.files;
            object[] stopPatterns = (object[])input.stopPatterns;
            object pluginPath = input.pluginPath;
            // this is actually the temporary path where the files requested in TestSupported
            // were put.
            string destinationPath = (string)input.scriptPath;
            var progressCB = (Func<object, Task<object>>)input.progressDelegate;
            dynamic choices = null;
            try
            {
                choices = input.choices;
            }
            catch (RuntimeBinderException) {
            }
            CoreDelegates coreDelegates = new CoreDelegates(input.coreDelegates);
            return await mInstaller.Install(
                new List<string>(files.Cast<string>()),
                new List<string>(stopPatterns.Cast<string>()),
                (string)pluginPath,
                destinationPath,
                choices,
                (bool)(input.validate ?? true),
                (ProgressDelegate)((int percent) => progressCB(percent)),
                coreDelegates
            );
        }
    }
}
