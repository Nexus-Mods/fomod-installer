using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.Schema;

namespace FomodInstaller.Scripting.XmlScript.Parsers
{
  /// <summary>
  /// Parses version 6.0 xml script files.
  /// </summary>
  public class Parser60 : Parser50
  {
    #region Constructors

    /// <summary>
    /// A simple constructor that initializes the object with the given values.
    /// </summary>
    /// <param name="p_xelScript">The xmlscript file.</param>
    /// <param name="p_xstXmlScriptType">The <see cref="XmlScriptType"/> that describes
    /// XML script type metadata.</param>
    public Parser60(XElement p_xelScript, XmlScriptType p_xstXmlScriptType)
      : base(p_xelScript, p_xstXmlScriptType)
    {
    }

    #endregion

    #region Parsing Methods

    /// <summary>
    /// Reads the condition from the given node.
    /// </summary>
    /// <param name="p_xelCondition">The node from which to load the condition.</param>
    /// <returns>An <see cref="ICondition"/> representing the condition described in the given node.</returns>
    protected override ICondition LoadCondition(XElement p_xelCondition)
    {
        if (p_xelCondition == null)
            return null;
        switch (p_xelCondition.GetSchemaInfo().SchemaType.Name)
        {
            case "versionDependency":
                switch (p_xelCondition.Name.LocalName)
                {
                    case "loaderDependency":
                    {
                        Version verMinLoaderVersion = ParseVersion(p_xelCondition.Attribute("version").Value);
                        string loaderType = p_xelCondition.Attribute("type").Value;
                        string comparison = p_xelCondition.Attribute("comparison")?.Value ?? "e";
                        return new LoaderVersionCondition(verMinLoaderVersion, loaderType, Enum.TryParse(comparison, true, out VersionComparisonType comparisonType) ? comparisonType : VersionComparisonType.E);
                    }
                    default:
                        return base.LoadCondition(p_xelCondition);
                }
            default:
                return base.LoadCondition(p_xelCondition);
        }
    }

    #endregion
  }
}
