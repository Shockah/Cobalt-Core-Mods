using System;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IVariableHintTargetPlayerApi"/>
		IVariableHintTargetPlayerApi VariableHintTargetPlayer { get; }
		
		/// <inheritdoc cref="IVariableHintTargetPlayerApi"/>
		[Obsolete($"Use `{nameof(VariableHintTargetPlayer)}` instead.")]
		IVariableHintTargetPlayerApi VariableHintTargetPlayerTargetPlayer { get; }

		/// <summary>
		/// Allows specifying the enemy as the target of a variable hint.
		/// </summary>
		public interface IVariableHintTargetPlayerApi
		{
			/// <summary>
			/// Casts the action to <see cref="IVariableHint"/>, if it is one.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <returns>The <see cref="IVariableHint"/>, if the given action is one, or <c>null</c> otherwise.</returns>
			IVariableHint? AsVariableHint(AVariableHint action);
			
			/// <summary>
			/// Creates a wrapper for <see cref="AVariableHint"/> that allows specifying the enemy as the target.
			/// </summary>
			/// <param name="action">The variable hint to wrap.</param>
			/// <returns>The new wrapper.</returns>
			IVariableHint MakeVariableHint(AVariableHint action);
			
			/// <summary>
			/// A wrapper for <see cref="AVariableHint"/> that allows specifying the enemy as the target.
			/// </summary>
			public interface IVariableHint : ICardAction<AVariableHint>
			{
				/// <summary>
				/// Whether this variable hint targets the player (<c>true</c>) or the enemy (<c>false</c>).
				/// </summary>
				bool TargetPlayer { get; set; }

				/// <summary>
				/// Sets <see cref="TargetPlayer"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IVariableHint SetTargetPlayer(bool value);
			}
		}
	}
}
