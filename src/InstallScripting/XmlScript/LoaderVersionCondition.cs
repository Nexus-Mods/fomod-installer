using FomodInstaller.Interface;
using System;

namespace FomodInstaller.Scripting.XmlScript
{
    /// <summary>
    /// A condition that requires a minimum version of the script extender to be installed.
    /// </summary>
    public class LoaderVersionCondition : VersionCondition
    {
        #region Properties

        public string Type { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// A simple constructor that initializes the object with the given values.
        /// </summary>
        /// <param name="p_verVersion">The minimum required version the script extender.</param>
        /// <param name="p_strExtender">Identifier for the game, which is the 4 letters name like "fose" or "nvse" or "skse"</param>
        public LoaderVersionCondition(Version p_verVersion, string p_strExtender)
            : base(p_verVersion)
        {
            Type = p_strExtender;
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
            var versionString = coreDelegates.context.GetExtenderVersion(Type);
            if (versionString == null) return false;
            
            if (!Version.TryParse(versionString, out var version)) return false;

            return version >= MinimumVersion;
        }

        /// <summary>
        /// Gets a message describing whether the condition is fulfilled.
        /// </summary>
        /// If the dependency is fulfilled, the message is "Passed." If the dependency is not fulfilled, the
        /// message informs the user of the installed version.
        /// <param name="p_csmStateManager">The manager that tracks the current install state.</param>
        /// <returns>A message describing whether the condition is fulfilled.</returns>
        /// <seealso cref="ICondition.GetMessage(ConditionStateManager)"/>
        public override string GetMessage(ConditionStateManager p_csmStateManager, CoreDelegates coreDelegates, bool invert)
        {
            var verInstalledVersion = coreDelegates.context.GetExtenderVersion(Type) is {} versionStr ? Version.TryParse(versionStr, out var version) ? version : null : null;

            if (verInstalledVersion == null && !invert)
                return $"This mod requires {Type} v{MinimumVersion} or higher";
            if (verInstalledVersion < MinimumVersion)
                return $"This mod requires {Type} v{MinimumVersion} or higher. You have {verInstalledVersion}";
            return "Passed";
        }
    }
}