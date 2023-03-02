using FomodInstaller.Interface;

namespace FomodInstaller.Scripting.XmlScript
{
	/// <summary>
	/// A condition that requires a specified flag to have a specific value.
	/// </summary>
	public class FlagCondition : ICondition
	{
		private string m_strFlagName;
		private string m_strValue;

		#region Properties

		/// <summary>
		/// Gets or sets the name of the flag that must have a specific value.
		/// </summary>
		/// <value>The name of the flag that must have a specific value.</value>
		public string FlagName
		{
			get
			{
				return m_strFlagName;
			}
			protected set
			{
				m_strFlagName = value;
			}
		}

		/// <summary>
		/// Gets or sets the value the flag that must have.
		/// </summary>
		/// <value>The value the flag that must have.</value>
		public string Value
		{
			get
			{
				return m_strValue;
			}
			protected set
			{
				m_strValue = value;
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// A simple constructor that initializes the object with the given values.
		/// </summary>
		/// <param name="p_strFile">The name of the falge that must have a specific value.</param>
		/// <param name="p_strValue">The value the flag that must have.</param>
		public FlagCondition(string p_strFlagName, string p_strValue)
		{
			m_strFlagName = p_strFlagName;
			m_strValue = p_strValue;
		}

        #endregion

        #region ICondition Members

        /// <summary>
        /// Gets whether or not the condition is fulfilled.
        /// </summary>
        /// <remarks>
        /// The condition is fulfilled if the specified <see cref="File"/> is in the
        /// specified <see cref="State"/>.
        /// </remarks>
        /// <param name="coreDelegates">The Core delegates component.</param>
        /// <returns><c>true</c> if the condition is fulfilled;
        /// <c>false</c> otherwise.</returns>
        /// <seealso cref="ICondition.GetIsFulfilled(CoreDelegates)"/>
        public bool GetIsFulfilled(ConditionStateManager csmState, ICoreDelegates coreDelegates)
		{
			string? strValue;

            csmState.FlagValues.TryGetValue(FlagName, out strValue);
			if (string.IsNullOrEmpty(Value))
				return string.IsNullOrEmpty(strValue);
			return Value.Equals(strValue);
		}

        /// <summary>1
        /// Gets a message describing whether or not the condition is fulfilled.
        /// </summary>
        /// <remarks>
        /// If the condition is fulfilled the message is "Passed." If the condition is not fulfilled the
        /// message uses the pattern:
        ///		File '&lt;file>' is not &lt;state>.
        /// </remarks>
        /// <param name="coreDelegates">The Core delegates component.</param>
        /// <returns>A message describing whether or not the condition is fulfilled.</returns>
        /// <seealso cref="ICondition.GetMessage(CoreDelegates)"/>
		public string GetMessage(ConditionStateManager csmState, ICoreDelegates coreDelegates, bool invert)
		{
			if (GetIsFulfilled(csmState, coreDelegates) && !invert)
				return "Passed";
			return string.Format("Flag '{0}' is {2}{1}.", FlagName, Value, invert ? "" : "not ");
		}

		#endregion
	}
}
