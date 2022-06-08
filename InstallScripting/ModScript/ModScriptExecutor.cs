using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows.Forms;
using FomodInstaller.Interface;

namespace FomodInstaller.Scripting.ModScript
{
    /// <summary>
    /// Executes a Mod Script script.
    /// </summary>
    public class ModScriptExecutor : ScriptExecutorBase
    {
        private ModScriptFunctionProxy m_msfFunctions = null;
        private string m_strVirtualActivatorPath = String.Empty;

        #region Constructors

        /// <summary>
        /// A simple constructor that initializes the object with the given values.
        /// </summary>
        /// <param name="p_msfFunctions">The proxy providing the implementations of the functions available to the mod script script.</param>
        public ModScriptExecutor(ModScriptFunctionProxy p_msfFunctions)
        {
            m_msfFunctions = p_msfFunctions;
        }

        #endregion

        #region ScriptExecutorBase Members

        /// <summary>
        /// Executes the script.
        /// </summary>
        /// <param name="p_scpScript">The Mod Script to execute.</param>
        /// <param name="p_strDataPath">path where script data is stored.</param>
        /// <param name="p_dynPreset">preset for the installer</param>
        /// <returns><c>true</c> if the script completes successfully;
        /// <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="p_scpScript"/> is not a
        /// <see cref="ModScript"/>.</exception>
        public override Task<IList<Instruction>> DoExecute(IScript p_scpScript, string p_strDataPath, dynamic p_dynPreset)
        {
            if (!(p_scpScript is ModScript))
                throw new ArgumentException("The given script must be of type ModScript.", "p_scpScript");

            ModScript mscScript = (ModScript)p_scpScript;

            if (string.IsNullOrEmpty(mscScript.Code))
                return null;

            object[] args = { m_msfFunctions };

            IList<Instruction> instructions = new List<Instruction>();
            m_msfFunctions.SetInstructionContainer(instructions);

            ScriptRunner srnRunner = null;
            try
            {
                srnRunner = new ScriptRunner(m_msfFunctions);
            }
            catch (Exception e)
            {
                // TODO rethrow because the transition layer to js seems to have trouble serializing the original exception
                //   of course we want to maintain more of the error message and this shouldn't be here but closer to the
                //   "edge".
                return Task.Run(new Func<IList<Instruction>>(() =>
                {
                    throw new Exception("failed to create runner: " + e.GetType().ToString() + "\n" + e.Message + "\n" + e.StackTrace + "\n" + e.Data.ToString());
                }));
            }
            finally
            {
                // AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
            return Task.Run(() =>
            {
                if (!srnRunner.Execute(mscScript.Code.Substring(3)))
                {
                    return null;
                }
                else
                {
                    return instructions;
                }
            });
        }

        #endregion

        /// <summary>
        /// Handles the <see cref="AppDomain.AssemblyResolve"/> event.
        /// </summary>
        /// <remarks>
        /// Assemblies that have been load dynamically aren't accessible by assembly name. So, when, for example,
        /// this class looks for the assembly containing the ScriptRunner type that was CreateInstanceFromAndUnwrap-ed,
        /// the class can't find the type. This handler searches through loaded assemblies and finds the required assembly.
        /// </remarks>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="args">A <see cref="ResolveEventArgs"/> describing the event arguments.</param>
        /// <returns>The assembly being looked for, or <c>null</c> if the assembly cannot
        /// be found.</returns>
        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            foreach (Assembly asmLoaded in AppDomain.CurrentDomain.GetAssemblies())
                if (asmLoaded.FullName == args.Name)
                    return asmLoaded;
            return null;
        }
    }
}
