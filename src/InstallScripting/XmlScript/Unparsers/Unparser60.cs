using System.Xml.Linq;

namespace FomodInstaller.Scripting.XmlScript.Unparsers
{
	/// <summary>
	/// Unparses version 6.0 XML scripts.
	/// </summary>
	public class Unparser60 : Unparser50
	{
		#region Constructors

		/// <summary>
		/// A simple constructor that initializes the object with the given values.
		/// </summary>
		/// <param name="p_xscScript">The script to unparse.</param>
		public Unparser60(XmlScript p_xscScript)
			: base(p_xscScript)
		{
		}

		#endregion

		#region Unparsing Methods

		/// <summary>
		/// Unparses the given <see cref="ICondition"/>, giving the generated mod the specified name.
		/// </summary>
		/// <remarks>
		/// This method can be overidden to provide game-specific or newer-version unparsing.
		/// </remarks>
		/// <param name="p_cndCondition">The <see cref="ICondition"/> for which to generate XML.</param>
		/// <param name="p_strNodeName">The name to give to the generated node.</param>
		/// <returns>The XML representation of the given <see cref="ICondition"/>.</returns>
		protected override XElement UnparseCondition(ICondition p_cndCondition, string p_strNodeName)
		{
			if (p_cndCondition is LoaderVersionCondition condition)
			{
				XElement xelGameVersion = new XElement("loaderDependency",
                    new XAttribute("type", condition.Type),
                    new XAttribute("version", condition.MinimumVersion.ToString()),
                    new XAttribute("comparison", condition.VersionComparisonType.ToString().ToLowerInvariant()));
				return xelGameVersion;
			}
			return base.UnparseCondition(p_cndCondition, p_strNodeName);
		}

		#endregion
	}
}
