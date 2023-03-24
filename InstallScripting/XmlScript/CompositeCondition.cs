﻿using System.Collections.Specialized;
using FomodInstaller.Interface;
using FomodInstaller.Utils;
using FomodInstaller.Utils.Collections;

namespace FomodInstaller.Scripting.XmlScript
{
	/// <summary>
	/// The possible relations of conditions.
	/// </summary>
	[Flags]
	public enum ConditionOperator
	{
		/// <summary>
		/// Indicates all contained conditions must be satisfied in order for this condition to be satisfied.
		/// </summary>
		And=1,

		/// <summary>
		/// Indicates at least one listed condition must be satisfied in order for this condition to be satisfied.
		/// </summary>
		Or=2
	}

	/// <summary>
	/// A condition that requires a combination of sub-conditions to be fulfilled.
	/// </summary>
	/// <remarks>
	/// The combination of sub-conditions that must be fulfilled is determined by an
	/// operator (e.g., and, or).
	/// </remarks>
	public class CompositeCondition : ObservableObject, ICondition
	{
		private ThreadSafeObservableList<ICondition> m_lstConditions = new ThreadSafeObservableList<ICondition>();

    #region Properties

    /// <summary>
    /// Gets the <see cref="ConditionOperator"/> specifying which of the sub-conditions
    /// must be fulfilled in order for this condition to be fulfilled.
    /// </summary>
    /// <value>The <see cref="ConditionOperator"/> specifying which of the sub-conditions
    /// must be fulfilled in order for this condition to be fulfilled.</value>
    public ConditionOperator Operator { get; } = ConditionOperator.And;

    /// <summary>
    /// Gets the sub-conditions.
    /// </summary>
    /// <value>The sub-conditions.</value>
    public IList<ICondition> Conditions
		{
			get
			{
				return m_lstConditions;
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// A simple constructor that initializes the object with the given values.
		/// </summary>
		/// <param name="p_dopOperator">The operator that specifies what combination of sub-conditions
		/// must be fulfilled in order for this dependancy to be fulfilled.</param>
		public CompositeCondition(ConditionOperator p_dopOperator)
		{
			Operator = p_dopOperator;
			m_lstConditions.CollectionChanged += new NotifyCollectionChangedEventHandler(ListConditions_CollectionChanged);
		}

		void ListConditions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			OnPropertyChanged(() => Conditions);
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
			bool booAllFulfilled = (Operator == ConditionOperator.And) ? true : false;
			bool booThisFulfilled = true;
			foreach (ICondition conCondition in m_lstConditions)
			{
				booThisFulfilled = conCondition.GetIsFulfilled(csmState, coreDelegates);
				switch (Operator)
				{
					case ConditionOperator.And:
						booAllFulfilled &= booThisFulfilled;
						break;
					case ConditionOperator.Or:
						booAllFulfilled |= booThisFulfilled;
						break;
				}
			}
			return booAllFulfilled;
		}

		/// <summary>
		/// Gets a message describing whether or not the condition is fulfilled.
		/// </summary>
		/// <remarks>
		/// If the condition is fulfilled the message is "Passed." If the condition is not fulfilled the
		/// message uses the pattern:
		///		File '&lt;file>' is not &lt;state>.
		/// </remarks>
		/// <param name="coreDelegates">The Core delegates component.</param>
    /// <param name="invert">Invert the logic in the message, explaining why it passed instead of why not</param>
		/// <returns>A message describing whether or not the condition is fulfilled.</returns>
		/// <seealso cref="ICondition.GetMessage(ICoreDelegates)"/>
		public string GetMessage(ConditionStateManager csmState, ICoreDelegates coreDelegates, bool invert)
        {
            bool booAllFulfilled = (Operator == ConditionOperator.And) ? true : false;
            bool booThisFulfilled = true;
            ICondition? conCondition = null;

            List<string> lines = new List<string>();

            for (Int32 i = 0; i < m_lstConditions.Count; i++)
            {
                conCondition = m_lstConditions[i];
                booThisFulfilled = conCondition.GetIsFulfilled(csmState, coreDelegates);
                if (!booThisFulfilled)
                    lines.Add(conCondition.GetMessage(csmState, coreDelegates, invert));

                booAllFulfilled = Operator == ConditionOperator.And
                  ? booAllFulfilled & booThisFulfilled
                  : booAllFulfilled | booThisFulfilled;
            }

            string sep = (Operator == ConditionOperator.Or) ? " OR\n" : "\n";
            string message = string.Join(sep, lines);

            return booAllFulfilled && !invert ? "Passed" : message;
        }

        #endregion
    }
}
