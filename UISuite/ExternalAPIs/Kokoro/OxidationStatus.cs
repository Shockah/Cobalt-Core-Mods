namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IOxidationStatusApi"/>
		IOxidationStatusApi OxidationStatus { get; }

		/// <summary>
		/// Allows access to the Oxidation status.
		/// At the end of the turn, if the ship has 7+ Oxidation, it loses all of it and gains 1 <see cref="Status.corrode">Corrode</see>.
		/// </summary>
		public interface IOxidationStatusApi
		{
			/// <summary>
			/// The status.
			/// </summary>
			Status Status { get; }
			
			/// <summary>
			/// Returns the current Oxidation trigger threshold, as in how much Oxidation is required for it to turn into <see cref="Status.corrode">Corrode</see>.
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="ship">The ship to check the threshold for.</param>
			/// <returns>The trigger threshold.</returns>
			int GetOxidationStatusThreshold(State state, Ship ship);
			
			/// <summary>
			/// Registers a new hook related to the Oxidation status.
			/// </summary>
			/// <param name="hook">The hook.</param>
			/// <param name="priority">The priority for the hook. Higher priority hooks are called before lower priority ones. Defaults to <c>0</c>.</param>
			void RegisterHook(IHook hook, double priority = 0);
			
			/// <summary>
			/// Unregisters the given hook related to the Oxidation status.
			/// </summary>
			/// <param name="hook">The hook.</param>
			void UnregisterHook(IHook hook);
			
			/// <summary>
			/// A hook related to the Oxidation status.
			/// </summary>
			public interface IHook : IKokoroV2ApiHook
			{
				/// <summary>
				/// Modifies the Oxidation status threshold, as in how much Oxidation is required for it to turn into <see cref="Status.corrode">Corrode</see>.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>The amount by which the threshold should be modified. Defaults to <c>0</c>.</returns>
				int ModifyOxidationRequirement(IModifyOxidationRequirementArgs args) => 0;
				
				/// <summary>
				/// The arguments for the <see cref="ModifyOxidationRequirement"/> hook method.
				/// </summary>
				public interface IModifyOxidationRequirementArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The ship that is currently being checked.
					/// </summary>
					Ship Ship { get; }
					
					/// <summary>
					/// The current threshold.
					/// </summary>
					int Threshold { get; }
				}
			}
		}
	}
}
