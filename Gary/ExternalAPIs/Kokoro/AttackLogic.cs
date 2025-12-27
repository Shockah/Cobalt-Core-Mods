namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IAttackLogicApi"/>
		IAttackLogicApi AttackLogic { get; }
		
		/// <summary>
		/// Allows modifying how attacks behave via a hook.
		/// </summary>
		public interface IAttackLogicApi
		{
			/// <summary>
			/// Registers a new hook related to attack logic.
			/// </summary>
			/// <param name="hook">The hook.</param>
			/// <param name="priority">The priority for the hook. Higher priority hooks are called before lower priority ones. Defaults to <c>0</c></param>
			void RegisterHook(IHook hook, double priority = 0);
			
			/// <summary>
			/// Unregisters the given hook related to attack logic.
			/// </summary>
			/// <param name="hook">The hook.</param>
			void UnregisterHook(IHook hook);

			/// <summary>
			/// Checks whether a midrow object would visually stop an attack from going through it.
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="combat">The current combat.</param>
			/// <param name="ship">The ship the check is for.</param>
			/// <param name="worldX">The world X coordinate the check is for.</param>
			/// <param name="object">The midrow object being checked. If <c>null</c>, defaults to the object at <see cref="worldX"/>.</param>
			/// <returns>Whether a midrow object would visually stop an attack from going through it.</returns>
			bool MidrowObjectVisuallyStopsAttacks(State state, Combat combat, Ship ship, int worldX, StuffBase? @object = null);

			/// <summary>
			/// A hook related to attack logic.
			/// </summary>
			public interface IHook : IKokoroV2ApiHook
			{
				/// <summary>
				/// Allows modifying how a (usually attack) highlight is rendered.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns><c>true</c> if the event is considered handled and no further hooks should be called; <c>false</c> otherwise.</returns>
				bool ModifyHighlightRendering(IModifyHighlightRenderingArgs args) => true;

				/// <summary>
				/// Controls whether a midrow object should visually look like it stops attacks going through it.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns><c>true</c> if the object should visually stop attacks, <c>false</c> if it should not, <c>null</c> if this hook does not care. Defaults to <c>null</c>.</returns>
				bool? ModifyMidrowObjectVisuallyStoppingAttacks(IModifyMidrowObjectVisuallyStoppingAttacksArgs args) => null;

				/// <summary>
				/// The arguments for the <see cref="ModifyHighlightRendering"/> hook method.
				/// </summary>
				public interface IModifyHighlightRenderingArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }

					/// <summary>
					/// The ship the highlight is being rendered for.
					/// </summary>
					Ship Ship { get; }
					
					/// <summary>
					/// The part the highlight is being rendered for.
					/// </summary>
					Part Part { get; }
					
					/// <summary>
					/// The world X coordinate at which the highlight is rendered.
					/// </summary>
					int WorldX { get; }
					
					/// <summary>
					/// The midrow object at the world X coordinate at which the highlight is rendered, if any.
					/// </summary>
					StuffBase? Object { get; }
					
					/// <summary>
					/// The color of the highlight.
					/// This value can be modified by the hook.
					/// </summary>
					Color HighlightColor { get; set; }
					
					/// <summary>
					/// Whether the highlight cap ends in the midrow (usually due to a collision with a midrow object).
					/// This value can be modified by the hook.
					/// </summary>
					bool StopsInMidrow { get; set; }
				}

				/// <summary>
				/// The arguments for the <see cref="ModifyMidrowObjectVisuallyStoppingAttacks"/> hook method.
				/// </summary>
				public interface IModifyMidrowObjectVisuallyStoppingAttacksArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The ship the check is for.
					/// </summary>
					Ship Ship { get; }
					
					/// <summary>
					/// The world X coordinate the check is for.
					/// </summary>
					int WorldX { get; }
					
					/// <summary>
					/// The midrow object being checked.
					/// </summary>
					StuffBase Object { get; }
				}
			}
		}
	}
}