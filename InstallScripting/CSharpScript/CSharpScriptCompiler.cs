using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
// using Microsoft.CSharp

namespace FomodInstaller.Scripting.CSharpScript
{
  /// <summary>
  /// Compiles C# scripts into executable assemblies.
  /// </summary>
  public class CSharpScriptCompiler
  {
    /// <summary>
    /// Compiles the given code into an assembly.
    /// </summary>
    /// <remarks>
    /// The compiled assembly is not loaded into the current app domain.
    /// </remarks>
    /// <param name="p_strCode">The C# code to compile</param>
    /// <param name="p_tpeBaseScriptType">The type of the base script from which the class in the code derives.</param>
    /// <param name="p_cecErrors">The collection in which to return any compilation errors.</param>
    /// <returns>The bytes of the compiled assembly.</returns>
    public byte[] Compile(string p_strCode, Type p_tpeBaseScriptType, out IEnumerable<Diagnostic> p_cecErrors)
    {
      HashSet<Assembly> referencedAssemblies = new HashSet<Assembly>()
      {
          typeof(object).Assembly,
          Assembly.Load(new AssemblyName("System")),
          Assembly.Load(new AssemblyName("System.Drawing")),
          Assembly.Load(new AssemblyName("System.Drawing.Common")),
          Assembly.Load(new AssemblyName("System.Drawing.Primitives")),
          Assembly.Load(new AssemblyName("System.ComponentModel.Primitives")),
          Assembly.Load(new AssemblyName("System.Runtime")),
          Assembly.Load(new AssemblyName("System.Text.RegularExpressions")),
          Assembly.Load(new AssemblyName("System.Windows.Forms")),
          Assembly.Load(new AssemblyName("System.Xml")),

          Assembly.Load(new AssemblyName("System.Windows.Extensions")),
          Assembly.Load(new AssemblyName("System.Windows.Forms.Primitives")),
      };

      List<string> setAssemblies = new List<string>();
      Type tpeReference = p_tpeBaseScriptType;
      while (tpeReference != null)
      {
        referencedAssemblies.Add(Assembly.GetAssembly(tpeReference));
        tpeReference = tpeReference.BaseType;
      }

      CSharpCompilation compilation = CSharpCompilation.Create("TestProgram", new[] {
          CSharpSyntaxTree.ParseText(p_strCode)
        }, referencedAssemblies
          .Select(assembly => MetadataReference.CreateFromFile(assembly.Location)),
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
      );

      MemoryStream memoryStream = new MemoryStream();
      EmitResult emitResult = compilation.Emit(memoryStream);
      memoryStream.Position = 0;

      p_cecErrors = emitResult.Success ? null : emitResult.Diagnostics;

      return memoryStream.ToArray();

      /*
      Dictionary<string, string> dicOptions = new Dictionary<string, string>();
      dicOptions["CompilerVersion"] = "v4.0";
      CSharpCodeProvider ccpProvider = new CSharpCodeProvider(dicOptions);
      CompilerParameters cpmOptions = new CompilerParameters();
      cpmOptions.GenerateExecutable = false;
      cpmOptions.GenerateInMemory = false;
      cpmOptions.OutputAssembly = GetTempFileName();
      cpmOptions.IncludeDebugInformation = false;
      cpmOptions.ReferencedAssemblies.Add("System.dll");
      cpmOptions.ReferencedAssemblies.Add("System.Drawing.dll");
      cpmOptions.ReferencedAssemblies.Add("System.Windows.Forms.dll");
      cpmOptions.ReferencedAssemblies.Add("System.Xml.dll");

      //reference the assemblies we need for the base script
      List<string> setAssemblies = new List<string>();
      Type tpeReference = p_tpeBaseScriptType;
      while (tpeReference != null)
      {
        setAssemblies.Add(Assembly.GetAssembly(tpeReference).Location);
        tpeReference = tpeReference.BaseType;
      }
      foreach (string strReference in setAssemblies)
        cpmOptions.ReferencedAssemblies.Add(strReference);

      CompilerResults crsResults = ccpProvider.CompileAssemblyFromSource(cpmOptions, p_strCode);
      p_cecErrors = null;
      if (crsResults.Errors.HasErrors || crsResults.Errors.HasWarnings)
      {
        p_cecErrors = crsResults.Errors;
        return null;
      }
      byte[] bteAssembly = File.ReadAllBytes(crsResults.PathToAssembly);
      try
      {
        File.Delete(crsResults.PathToAssembly);
      } catch (Exception e)
      {
        Console.WriteLine("failed to delete temporary file {0}: {1}", crsResults.PathToAssembly, e);
        // what are people doing to their system? Oh well, this alone isn't worth doing anything about,
        // what's one more abandoned temporary file?
      }
      return bteAssembly;
      */
    }

    private static string GetTempFileName()
    {
      return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    }
  }
}
