using System;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace FomodInstaller.Scripting.CSharpScript
{
    /// <summary>
    /// Runs a C# script.
    /// </summary>
    /// <remarks>
    /// This class is meant to be run in a sandboxed domain in order to limit the possible
    /// damage from malicious or poorly written code.
    /// </remarks>
    public class ScriptRunner : MarshalByRefObject
    {
        private CSharpScriptFunctionProxy m_csfFunctions = null;

        #region Constructors

        /// <summary>
        /// A simple construtor that initializes the object with the given dependencies.
        /// </summary>
        /// <param name="p_csfFunctions">The object that implements the script functions.</param>
        public ScriptRunner(CSharpScriptFunctionProxy p_csfFunctions)
        {
            m_csfFunctions = p_csfFunctions;
        }

        #endregion

        /// <summary>
        /// Executes the given script.
        /// </summary>
        /// <param name="p_bteScript">The bytes of the assembly containing the script to execute.</param>
        /// <returns><c>true</c> if the script completes successfully;
        /// <c>false</c> otherwise.</returns>
        public bool Execute(byte[] p_bteScript)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Control.CheckForIllegalCrossThreadCalls = false;

            Assembly asmScript = Assembly.Load(p_bteScript);

            object s = asmScript.CreateInstance("Script");
            if (s == null)
            {
                m_csfFunctions.ExtendedMessageBox("C# Script did not contain a 'Script' class in the root namespace.", "Error", null);
                return false;
            }

            try
            {
                MethodInfo mifMethod = null;
                try
                {
                    for (Type tpeScriptType = s.GetType(); mifMethod == null; tpeScriptType = tpeScriptType.BaseType)
                        mifMethod = tpeScriptType.GetMethod("Setup", new Type[] { typeof(CSharpScriptFunctionProxy) });
                    mifMethod.Invoke(s, new object[] { m_csfFunctions });
                    var onActivate = s.GetType().GetMethod("OnActivate");
                    object res = onActivate.Invoke(s, null);
                    return (bool)res;
                }
                catch (System.Reflection.TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
            }
            catch (System.Security.SecurityException ex)
            {
                string strMessage = "Mod installer incompatible.";
                StringBuilder stbException = new StringBuilder();
                stbException
                    .Append("The mod installer attempted to access a location outside the mod directory.")
                    .AppendLine()
                    .Append("While this is likely to be harmless, due to technical and security reasons we cannot ensure " +
                            "compatibility with this type of installer.")
                    .AppendLine()
                    .Append("We ask you to instead install the mod manually and apologize for the inconvenience.")
                    .AppendLine()
                    .AppendLine()
                    .Append(ex.ToString());
                m_csfFunctions.ExtendedMessageBox(strMessage, "Error", stbException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                StringBuilder stbException = new StringBuilder(ex.ToString());
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    stbException.AppendLine().AppendLine().Append(ex.ToString());
                }
                string strMessage = "An exception occurred in the script.";
                m_csfFunctions.ExtendedMessageBox(strMessage, "Error", stbException.ToString());
                return false;
            }
        }
    }
}
