using FomodInstaller.Interface;
using System;
using System.Threading.Tasks;

namespace FomodInstaller.Scripting.XmlScript
{
  /// <summary>
  /// A condition that requires a minimum version of the script extender to be installed.
  /// </summary>
  public class SEVersionCondition : VersionCondition
  {
    #region Constructors
    private string m_strExtender;

    /// <summary>
    /// A simple constructor that initializes the object with the given values.
    /// </summary>
    /// <param name="p_verVersion">The minimum required version the script extender.</param>
    /// <param name="p_strExtender">Identifier for the game, which is the 4 letters name like "fose" or "nvse" or "skse"</param>
    public SEVersionCondition(Version p_verVersion, string p_strExtender)
      : base(p_verVersion)
    {
      m_strExtender = p_strExtender;
    }

    #endregion

    /// <summary>
    /// Gets whether or not the condition is fulfilled.
    /// </summary>
    /// <remarks>
    /// The dependency is fulfilled if the specified minimum version of
    /// NVSE is installed.
    /// </remarks>
    /// <param name="p_csmStateManager">The manager that tracks the currect install state.</param>
    /// <returns><c>true</c> if the condition is fulfilled;
    /// <c>false</c> otherwise.</returns>
    /// <seealso cref="ICondition.GetIsFulfilled(ConditionStateManager)"/>
    public override bool GetIsFulfilled(ConditionStateManager p_csmStateManager, CoreDelegates coreDelegates)
    {
      Version verInstalledVersion = null;

      Task.Run(async () =>
      {
        verInstalledVersion = new Version(await coreDelegates.context.GetExtenderVersion(m_strExtender));
      }).Wait();

      return ((verInstalledVersion != null) && (verInstalledVersion >= MinimumVersion));
    }

    /// <summary>
    /// Gets a message describing whether or not the condition is fulfilled.
    /// </summary>
    /// If the dependency is fulfilled the message is "Passed." If the dependency is not fulfilled the
    /// message informs the user of the installed version.
    /// <param name="p_csmStateManager">The manager that tracks the currect install state.</param>
    /// <returns>A message describing whether or not the condition is fulfilled.</returns>
    /// <seealso cref="ICondition.GetMessage(ConditionStateManager)"/>
    public override string GetMessage(ConditionStateManager p_csmStateManager, CoreDelegates coreDelegates)
    {
      Version verInstalledVersion = null;

      Task.Run(async () =>
      {
        verInstalledVersion = new Version(await coreDelegates.context.GetExtenderVersion(m_strExtender));
      }).Wait();

      if (verInstalledVersion == null)
        return String.Format("This mod requires {0} v{1} or higher. Please download from http://{0}.silverlock.org", m_strExtender, MinimumVersion);
      else if (verInstalledVersion < MinimumVersion)
        return String.Format("This mod requires {0} v{1} or higher. You have {2}. Please update from http://{0}.silverlock.org", m_strExtender, MinimumVersion, verInstalledVersion);
      else
        return "Passed";
    }
  }
}
