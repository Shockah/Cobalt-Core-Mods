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
			}
		}
	}
}