using System;
using Nickel;

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
			/// The card trait that is being added to temporarily upgraded cards.
			/// </summary>
			/// <remarks>
			/// This trait is not meant to be added via <see cref="IModCards"/> methods - it is purely visual, and adding it will not change any behavior.
			/// </remarks>
			ICardTraitEntry CardTrait { get; }
		
			/// <summary>
			/// A tooltip for a temporary card <b><i>upgrade</i></b> (used when a card goes from no upgrade to some upgrade).
			/// </summary>
			Tooltip UpgradeTooltip { get; }
			
			/// <summary>
			/// A tooltip for a temporary card <b><i>downgrade</i></b> (used when a card goes from some upgrade to no upgrade).
			/// </summary>
			Tooltip DowngradeTooltip { get; }
			
			/// <summary>
			/// A tooltip for a temporary card <b><i>sidegrade</i></b> (used when a card goes from some upgrade to another upgrade).
			/// </summary>
			Tooltip SidegradeTooltip { get; }

			/// <summary>
			/// Returns the permanent upgrade of a card.
			/// </summary>
			/// /// <remarks>
			/// The <see cref="Card.upgrade"/> field's value will reflect the temporary upgrade if there is one set, or the permanent upgrade otherwise.
			/// </remarks>
			/// <param name="card">The card to get the upgrade for.</param>
			/// <returns>The permanent upgrade of a card.</returns>
			[Obsolete($"Use the {nameof(GetPermanentUpgrade)}(State state, Card card) override instead.")]
			Upgrade GetPermanentUpgrade(Card card);

			/// <summary>
			/// Returns the permanent upgrade of a card.
			/// </summary>
			/// /// <remarks>
			/// The <see cref="Card.upgrade"/> field's value will reflect the temporary upgrade if there is one set, or the permanent upgrade otherwise.
			/// </remarks>
			/// <param name="state">The game state.</param>
			/// <param name="card">The card to get the upgrade for.</param>
			/// <returns>The permanent upgrade of a card.</returns>
			Upgrade GetPermanentUpgrade(State state, Card card);
			
			/// <summary>
			/// Returns the temporary upgrade of a card.
			/// </summary>
			/// <remarks>
			/// The <see cref="Card.upgrade"/> field's value will reflect the temporary upgrade if there is one set, or the permanent upgrade otherwise.
			/// </remarks>
			/// <param name="card">The card to get the upgrade for.</param>
			/// <returns>The temporary upgrade of a card.</returns>
			[Obsolete($"Use the {nameof(GetTemporaryUpgrade)}(State state, Card card) override instead.")]
			Upgrade? GetTemporaryUpgrade(Card card);
			
			/// <summary>
			/// Returns the temporary upgrade of a card.
			/// </summary>
			/// <remarks>
			/// The <see cref="Card.upgrade"/> field's value will reflect the temporary upgrade if there is one set, or the permanent upgrade otherwise.
			/// </remarks>
			/// <param name="state">The game state.</param>
			/// <param name="card">The card to get the upgrade for.</param>
			/// <returns>The temporary upgrade of a card.</returns>
			Upgrade? GetTemporaryUpgrade(State state, Card card);
			
			/// <summary>
			/// Sets the permanent upgrade of a card.
			/// </summary>
			/// /// <remarks>
			/// The <see cref="Card.upgrade"/> field's value will reflect the temporary upgrade if there is one set, or the permanent upgrade otherwise.
			/// </remarks>
			/// <param name="card">The card to set the upgrade for.</param>
			/// <param name="upgrade">The upgrade to set.</param>
			[Obsolete($"Use the {nameof(SetPermanentUpgrade)}(State state, Card card, Upgrade upgrade) override instead.")]
			void SetPermanentUpgrade(Card card, Upgrade upgrade);

			/// <summary>
			/// Sets the permanent upgrade of a card.
			/// </summary>
			/// /// <remarks>
			/// The <see cref="Card.upgrade"/> field's value will reflect the temporary upgrade if there is one set, or the permanent upgrade otherwise.
			/// </remarks>
			/// <param name="state">The game state.</param>
			/// <param name="card">The card to set the upgrade for.</param>
			/// <param name="upgrade">The upgrade to set.</param>
			void SetPermanentUpgrade(State state, Card card, Upgrade upgrade);
			
			/// <summary>
			/// Sets the temporary upgrade of a card.
			/// </summary>
			/// /// <remarks>
			/// The <see cref="Card.upgrade"/> field's value will reflect the temporary upgrade if there is one set, or the permanent upgrade otherwise.
			/// </remarks>
			/// <param name="card">The card to set the upgrade for.</param>
			/// <param name="upgrade">The upgrade to set.</param>
			[Obsolete($"Use the {nameof(SetTemporaryUpgrade)}(State state, Card card, Upgrade upgrade) override instead.")]
			void SetTemporaryUpgrade(Card card, Upgrade? upgrade);
			
			/// <summary>
			/// Sets the temporary upgrade of a card.
			/// </summary>
			/// /// <remarks>
			/// The <see cref="Card.upgrade"/> field's value will reflect the temporary upgrade if there is one set, or the permanent upgrade otherwise.
			/// </remarks>
			/// <param name="state">The game state.</param>
			/// <param name="card">The card to set the upgrade for.</param>
			/// <param name="upgrade">The upgrade to set.</param>
			void SetTemporaryUpgrade(State state, Card card, Upgrade? upgrade);

			/// <summary>
			/// Casts the action to <see cref="ISetTemporaryUpgradeAction"/>, if it is one.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <returns>The <see cref="ISetTemporaryUpgradeAction"/>, if the given action is one, or <c>null</c> otherwise.</returns>
			ISetTemporaryUpgradeAction? AsSetTemporaryUpgradeAction(CardAction action);
			
			/// <summary>
			/// Creates a new action that sets a given card's temporary upgrade.
			/// </summary>
			/// <param name="cardId">The ID of the card to set the upgrade for.</param>
			/// <param name="upgrade">The upgrade to set.</param>
			/// <returns>The new action.</returns>
			ISetTemporaryUpgradeAction MakeSetTemporaryUpgradeAction(int cardId, Upgrade? upgrade);
			
			/// <summary>
			/// Casts the action to <see cref="IChooseTemporaryUpgradeAction"/>, if it is one.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <returns>The <see cref="IChooseTemporaryUpgradeAction"/>, if the given action is one, or <c>null</c> otherwise.</returns>
			IChooseTemporaryUpgradeAction? AsChooseTemporaryUpgradeAction(CardAction action);
			
			/// <summary>
			/// Creates a new action that will present a screen to choose a temporary upgrade to set for the given card.
			/// </summary>
			/// <param name="cardId">The ID of the card to set the upgrade for.</param>
			/// <returns>The new action.</returns>
			IChooseTemporaryUpgradeAction MakeChooseTemporaryUpgradeAction(int cardId);
			
			/// <summary>
			/// Allows modifying a <see cref="CardUpgrade"/> route to upgrade cards temporarily.
			/// </summary>
			/// <param name="route">The route.</param>
			/// <returns>A wrapper, granting access to the modifications.</returns>
			ICardUpgrade ModifyCardUpgrade(CardUpgrade route);
			
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
			/// A <see cref="CardUpgrade"/> route wrapper, which allows modifying it to upgrade cards temporarily.
			/// </summary>
			public interface ICardUpgrade : IRoute<CardUpgrade>
			{
				/// <summary>
				/// Whether the applied upgrade should be temporary.
				/// </summary>
				bool IsTemporaryUpgrade { get; set; }

				/// <summary>
				/// Sets <see cref="IsTemporaryUpgrade"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardUpgrade SetIsTemporaryUpgrade(bool value);
			}

			/// <summary>
			/// An action that sets a given card's temporary upgrade.
			/// </summary>
			public interface ISetTemporaryUpgradeAction : ICardAction<CardAction>
			{
				/// <summary>
				/// The ID of a card to set the upgrade for.
				/// </summary>
				int CardId { get; set; }
				
				/// <summary>
				/// The upgrade to set.
				/// </summary>
				Upgrade? Upgrade { get; set; }

				/// <summary>
				/// Sets <see cref="CardId"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ISetTemporaryUpgradeAction SetCardId(int value);
				
				/// <summary>
				/// Sets <see cref="Upgrade"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ISetTemporaryUpgradeAction SetUpgrade(Upgrade? value);
			}
			
			/// <summary>
			/// An action that will present a screen to choose a temporary upgrade to set for the given card.
			/// </summary>
			public interface IChooseTemporaryUpgradeAction : ICardAction<CardAction>
			{
				/// <summary>
				/// The ID of a card to set the upgrade for.
				/// </summary>
				int CardId { get; set; }

				/// <summary>
				/// Sets <see cref="CardId"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IChooseTemporaryUpgradeAction SetCardId(int value);
			}
			
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
