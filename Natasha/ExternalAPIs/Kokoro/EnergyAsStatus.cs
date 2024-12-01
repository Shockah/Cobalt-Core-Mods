namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IEnergyAsStatusApi"/>
		IEnergyAsStatusApi EnergyAsStatus { get; }

		/// <summary>
		/// Allows treating <see cref="Combat.energy"/> as if it was a status for <see cref="AStatus"/> and <see cref="AVariableHint"/>.
		/// </summary>
		public interface IEnergyAsStatusApi
		{
			/// <summary>
			/// Casts the action to <see cref="IVariableHint"/>, if it is one.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <returns>The <see cref="IVariableHint"/>, if the given action is one, or <c>null</c> otherwise.</returns>
			IVariableHint? AsVariableHint(AVariableHint action);
			
			/// <summary>
			/// Creates a new variable hint action for the current amount of energy.
			/// </summary>
			/// <param name="tooltipOverride">An override value for the current amount of energy, used in tooltips. See <see cref="IVariableHint.TooltipOverride"/>.</param>
			/// <returns>The new variable hint action.</returns>
			IVariableHint MakeVariableHint(int? tooltipOverride = null);

			/// <summary>
			/// Casts the action to <see cref="IStatusAction"/>, if it is one.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <returns>The <see cref="IStatusAction"/>, if the given action is one, or <c>null</c> otherwise.</returns>
			IStatusAction? AsStatusAction(AStatus action);
			
			/// <summary>
			/// Creates a new <see cref="AStatus"/> action that changes the current amount of energy.
			/// </summary>
			/// <param name="amount">A modifier amount. See <see cref="AStatus.statusAmount"/>.</param>
			/// <param name="mode">A modifier mode. See <see cref="AStatus.mode"/>.</param>
			/// <returns></returns>
			IStatusAction MakeStatusAction(int amount, AStatusMode mode = AStatusMode.Add);
			
			/// <summary>
			/// A variable hint action for the current amount of energy.
			/// </summary>
			public interface IVariableHint : ICardAction<AVariableHint>
			{
				/// <summary>
				/// An override value for the current amount of energy, used in tooltips.
				/// </summary>
				/// <remarks>
				/// This can be used to correct the amount of energy if a card does not cost 0, or otherwise changes the amount of energy between its actions.
				/// </remarks>
				int? TooltipOverride { get; set; }

				/// <summary>
				/// Sets <see cref="TooltipOverride"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IVariableHint SetTooltipOverride(int? value);
			}
			
			/// <summary>
			/// An <see cref="AStatus"/> wrapper action that changes the current amount of energy.
			/// </summary>
			public interface IStatusAction : ICardAction<AStatus>;
		}
	}
}
