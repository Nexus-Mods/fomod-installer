using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FomodInstaller.Interface;
using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace FomodInstaller.Scripting.CSharpScript
{
    /// <summary>
    /// Executes a C# script.
    /// </summary>
    public class CSharpScriptExecutor : ScriptExecutorBase
    {

        private static Regex m_regScriptClass = new Regex(@"(class\s+Script\s*:.*?)(\S*BaseScript)");
        private static Regex m_regFommUsing = new Regex(@"\s*using\s*fomm.Scripting\s*;");
        private CSharpScriptFunctionProxy m_csfFunctions = null;

        #region Properties

        /// <summary>
        /// Gets the type of the base script for all C# scripts.
        /// </summary>
        /// <value>The type of the base script for all C# scripts.</value>
        protected Type BaseScriptType { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// A simple constructor that initializes the object with the given values.
        /// </summary>
        public CSharpScriptExecutor(CSharpScriptFunctionProxy p_csfFunctions, Type p_typBaseScriptType)
        {
            m_csfFunctions = p_csfFunctions;
            BaseScriptType = p_typBaseScriptType;
        }

        #endregion

        #region ScriptExecutorBase Members

        /// <summary>
        /// Executes the script.
        /// </summary>
        /// <param name="p_scpScript">The C# Script to execute.</param>
        /// <param name="p_strDataPath">Path where data for this script (i.e. screenshots) is stored.</param>
        /// <param name="p_dynPreset">install preset (not supported in this executor)</param>
        /// <returns><c>true</c> if the script completes successfully;
        /// <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="p_scpScript"/> is not a
        /// <see cref="CSharpScript"/>.</exception>
        public override Task<IList<Instruction>> DoExecute(IScript p_scpScript, string p_strDataPath, object? p_dynPreset)
        {
            if (!(p_scpScript is CSharpScript))
                throw new ArgumentException("The given script must be of type CSharpScript.", "p_scpScript");

            CSharpScript cscScript = (CSharpScript)p_scpScript;

            byte[] bteScript = Compile(cscScript.Code);
            if (bteScript == null)
                return Task.FromResult<IList<Instruction>>(null);

            IList<Instruction> instructions = new List<Instruction>();
            m_csfFunctions.SetInstructionContainer(instructions);

            ScriptRunner srnRunner = new ScriptRunner(m_csfFunctions);

            return Task.Run(() =>
            {

                if (!srnRunner.Execute(bteScript))
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
        /// Compiles the given C# script code.
        /// </summary>
        /// <remarks>
        /// The compiled script is not loaded into the current domain.
        /// </remarks>
        /// <param name="p_strCode">The code to compile.</param>
        /// <returns>The bytes of the assembly containing the script to execute.</returns>
        protected byte[] Compile(string p_strCode)
        {
            CSharpScriptCompiler sccCompiler = new CSharpScriptCompiler();
            IEnumerable<Diagnostic> cecErrors = null;

            string strBaseScriptClassName = m_regScriptClass.Match(p_strCode).Groups[2].ToString();
            string strCode = m_regScriptClass.Replace(p_strCode, "using " + BaseScriptType.Namespace + ";\r\n$1" + BaseScriptType.Name);
            Regex regOtherScriptClasses = new Regex(string.Format(@"(class\s+\S+\s*:.*?)(?<!\w){0}", strBaseScriptClassName));
            strCode = regOtherScriptClasses.Replace(strCode, "$1" + BaseScriptType.Name);
            strCode = m_regFommUsing.Replace(strCode, "");
            byte[] bteAssembly = sccCompiler.Compile(strCode, BaseScriptType, out cecErrors);

            if (cecErrors != null)
            {
                IEnumerable<Diagnostic> errors = cecErrors.Where(d => d.Severity == DiagnosticSeverity.Error);
                IEnumerable<Diagnostic> warnings = cecErrors.Where(d => d.Severity == DiagnosticSeverity.Warning);

                StringBuilder stbErrors = new StringBuilder();

                if (errors.Count() > 0)
                {
                    stbErrors.Append("<h3 style='color:red'>Errors</h3><ul>");
                    foreach (Diagnostic cerError in errors)
                    {
                      LinePosition pos = cerError.Location.GetLineSpan().StartLinePosition;
                      stbErrors.AppendFormat("<li><b>{0},{1}:</b> {2} <i>(Error {3})</i></li>", pos.Line, pos.Character, cerError.GetMessage(), cerError.Id);
                    }
                    stbErrors.Append("</ul>");
                }
                if (warnings.Count() > 0)
                {
                    stbErrors.Append("<h3 style='color:#ffd700;'>Warnings</h3><ul>");
                    foreach (Diagnostic cerError in warnings)
                    {
                      LinePosition pos = cerError.Location.GetLineSpan().StartLinePosition;
                      stbErrors.AppendFormat("<li><b>{0},{1}:</b> {2} <i>(Error {3})</i></li>", pos.Line, pos.Character, cerError.GetMessage(), cerError.Id);
                    }
                    stbErrors.Append("</ul>");
                }
                if (errors.Count() > 0)
                {
                    string strMessage = "Could not compile script; errors were found.";
                    m_csfFunctions.ExtendedMessageBox(strMessage, "Error", stbErrors.ToString());
                    return null;
                }
            }
            return bteAssembly;
        }
    }
}
