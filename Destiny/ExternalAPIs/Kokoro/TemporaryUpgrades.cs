namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="ITemporaryUpgradesApi"/>
		ITemporaryUpgradesApi TemporaryUpgrades { get; }
		
		/// <summary>
		/// Allows access to temporary card upgrades, which revert after combat.
		/// </summary>
		public interface ITemporaryUpgradesApi
		{
			/// <summary>
			/// A tooltip for a temporary card <b><i>upgrade</i></b> (used when a card goes from no upgrade to some upgrade).
			/// </summary>
			Tooltip UpgradeTooltip { get; }
			
			/// <summary>
			/// Registers a new hook related to temporary upgrades.
			/// </summary>
			/// <param name="hook">The hook.</param>
			/// <param name="priority">The priority for the hook. Higher priority hooks are called before lower priority ones. Defaults to <c>0</c>.</param>
			void RegisterHook(IHook hook, double priority = 0);
			
			/// <summary>
			/// Unregisters the given hook related to temporary upgrades.
			/// </summary>
			/// <param name="hook">The hook.</param>
			void UnregisterHook(IHook hook);
			
			/// <summary>
			/// A hook related to temporary upgrades.
			/// </summary>
			public interface IHook : IKokoroV2ApiHook
			{
				/// <summary>
				/// An event called whenever a card's temporary upgrade changed.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				void OnTemporaryUpgrade(IOnTemporaryUpgradeArgs args) { }

				/// <summary>
				/// The arguments for the <see cref="OnTemporaryUpgrade"/> hook method.
				/// </summary>
				public interface IOnTemporaryUpgradeArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The card the temporary upgrade changed for.
					/// </summary>
					Card Card { get; }
					
					/// <summary>
					/// The old temporary upgrade.
					/// </summary>
					Upgrade? OldTemporaryUpgrade { get; }
					
					/// <summary>
					/// The new temporary upgrade.
					/// </summary>
					Upgrade? NewTemporaryUpgrade { get; }
					
					/// <summary>
					/// The old upgrade (including the persistent upgrade, if there was no temporary upgrade).
					/// </summary>
					Upgrade OldUpgrade { get; }
					
					/// <summary>
					/// The new upgrade (including the persistent upgrade, if there is no temporary upgrade).
					/// </summary>
					Upgrade NewUpgrade { get; }
				}
			}
		}
	}
}
