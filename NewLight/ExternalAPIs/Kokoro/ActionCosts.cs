using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IActionCostsApi"/>
		IActionCostsApi ActionCosts { get; }

		/// <summary>
		/// Allows working with and creating actions with resource costs (like Books' Shards).
		/// </summary>
		public interface IActionCostsApi
		{
			/// <summary>
			/// Casts the action to <see cref="ICostAction"/>, if it is one.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <returns>The <see cref="ICostAction"/>, if the given action is one, or <c>null</c> otherwise.</returns>
			ICostAction? AsCostAction(CardAction action);
			
			/// <summary>
			/// Creates a new action with a resource cost.
			/// </summary>
			/// <param name="cost">The cost of the action.</param>
			/// <param name="action">The action to run.</param>
			/// <returns>A new action with a resource cost.</returns>
			ICostAction MakeCostAction(ICost cost, CardAction action);

			/// <summary>
			/// Registers a set of icons for the given resource for the given amount.
			/// </summary>
			/// <remarks>
			/// If an action requires more of a resource, and an icon for a higher amount is registered, it will be used instead of an icon for the lower amount.
			/// For example, if an action requires 27 of a resource, and has icons registered for 10, 5 and 1, Kokoro will display two icons for 10, an icon for 5, and two icons for 1.
			/// </remarks>
			/// <param name="resource">The resource to register the icons for.</param>
			/// <param name="costSatisfiedIcon">The icon that will be used when the resource is satisfied.</param>
			/// <param name="costUnsatisfiedIcon">The icon that will be used when the resource is not satisfied.</param>
			/// <param name="amount">The amount of the resource. Defaults to <c>1</c>.</param>
			void RegisterResourceCostIcon(IResource resource, Spr costSatisfiedIcon, Spr costUnsatisfiedIcon, int amount = 1);
			
			/// <summary>
			/// Registers a set of icons for the given status resource for the given amount.
			/// </summary>
			/// <param name="status">The status resource to register the icons for.</param>
			/// <param name="costSatisfiedIcon">The icon that will be used when the resource is satisfied.</param>
			/// <param name="costUnsatisfiedIcon">The icon that will be used when the resource is not satisfied.</param>
			/// <param name="amount">The amount of the resource. Defaults to <c>1</c>.</param>
			/// <param name="targetPlayer">Whether the new icons are being registered for the player (<c>true</c>), the enemy (<c>false</c>), or both (<c>null</c>, the default).</param>
			void RegisterStatusResourceCostIcon(Status status, Spr costSatisfiedIcon, Spr costUnsatisfiedIcon, int amount = 1, bool? targetPlayer = null);
			
			/// <summary>
			/// Calculates a list of icons which would be displayed for a given amount of a given resource, considering all of the registered icons.
			/// </summary>
			/// <param name="resource">The resource to get the icons for.</param>
			/// <param name="amount">The amount of the resource.</param>
			/// <returns>The list of icons which would be displayed for a given amount of a given resource, considering all of the registered icons.</returns>
			IReadOnlyList<IResourceCostIcon> GetResourceCostIcons(IResource resource, int amount);
			
			/// <summary>
			/// Casts the action to <see cref="IStatusResource"/>, if it is one.
			/// </summary>
			/// <param name="resource">The resource.</param>
			/// <returns>The <see cref="IStatusResource"/>, if the given resource is one, or <c>null</c> otherwise.</returns>
			IStatusResource? AsStatusResource(IResource resource);
			
			/// <summary>
			/// Creates a status resource for the given status.
			/// </summary>
			/// <param name="status">The status.</param>
			/// <param name="targetPlayer">Whether the resource looks for the status on the player's ship (<c>true</c>) or the enemy's ship (<c>false</c>).</param>
			/// <returns>The new status resource.</returns>
			IStatusResource MakeStatusResource(Status status, bool targetPlayer = true);

			/// <summary>
			/// Casts the action to <see cref="IEnergyResource"/>, if it is one.
			/// </summary>
			/// <param name="resource">The resource.</param>
			/// <returns>The <see cref="IEnergyResource"/>, if the given resource is one, or <c>null</c> otherwise.</returns>
			IEnergyResource? AsEnergyResource(IResource resource);
			
			/// <summary>
			/// The energy resource.
			/// </summary>
			IEnergyResource EnergyResource { get; }

			/// <summary>
			/// Casts the action cost to <see cref="IResourceCost"/>, if it is one.
			/// </summary>
			/// <param name="cost">The cost.</param>
			/// <returns>The <see cref="IResourceCost"/>, if the given cost is one, or <c>null</c> otherwise.</returns>
			IResourceCost? AsResourceCost(ICost cost);
			
			/// <summary>
			/// Creates a new resource action cost.
			/// </summary>
			/// <param name="resource">The resource.</param>
			/// <param name="amount">The amount of the resource.</param>
			/// <returns>A new resource action cost.</returns>
			IResourceCost MakeResourceCost(IResource resource, int amount);
			
			/// <summary>
			/// Creates a new resource action cost, which can use multiple different resources as alternatives.
			/// </summary>
			/// <param name="potentialResources">The resources that can be used.</param>
			/// <param name="amount">The amount of the resources.</param>
			/// <returns>A new resource action cost.</returns>
			IResourceCost MakeResourceCost(IEnumerable<IResource> potentialResources, int amount);
			
			/// <summary>
			/// Casts the action cost to <see cref="ICombinedCost"/>, if it is one.
			/// </summary>
			/// <param name="cost">The cost.</param>
			/// <returns>The <see cref="ICombinedCost"/>, if the given cost is one, or <c>null</c> otherwise.</returns>
			ICombinedCost? AsCombinedCost(ICost cost);
			
			/// <summary>
			/// Creates a new combined cost, which requires multiple different costs to be paid.
			/// </summary>
			/// <param name="costs">The costs that need to be paid.</param>
			/// <returns>A new combined action cost.</returns>
			ICombinedCost MakeCombinedCost(IEnumerable<ICost> costs);
			
			/// <summary>
			/// A resource provider which uses actual resources from the game's state by calling <see cref="IResource.GetCurrentResourceAmount"/> and <see cref="IResource.Pay"/>.
			/// </summary>
			IResourceProvider StateResourceProvider { get; }
			
			/// <summary>
			/// Registers a resource provider, which can be used before/after resources from the game's state are spent.
			/// </summary>
			/// <param name="resourceProvider">The resource provider.</param>
			/// <param name="priority">The priority for the provider. Higher priority hooks are called before lower priority ones. Defaults to <c>0</c>.</param>
			void RegisterResourceProvider(IResourceProvider resourceProvider, double priority = 0);
			
			/// <summary>
			/// Unregisters a resource provider.
			/// </summary>
			/// <param name="resourceProvider">The resource provider.</param>
			void UnregisterResourceProvider(IResourceProvider resourceProvider);

			/// <summary>
			/// Creates a new mock payment environment.
			/// </summary>
			/// <param name="default">A payment environment which will be queried for the initial value of each resource. If <c>null</c> (default), initial values will be <c>0</c>.</param>
			/// <returns>A new mock payment environment.</returns>
			IMockPaymentEnvironment MakeMockPaymentEnvironment(IPaymentEnvironment? @default = null);
			
			/// <summary>
			/// Creates a new payment environment, basing on the actual game state.
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="combat">The current combat.</param>
			/// <returns>A new payment environment, basing on the actual game state.</returns>
			IPaymentEnvironment MakeStatePaymentEnvironment(State state, Combat combat);
			
			/// <summary>
			/// Creates a new payment environment, basing on the actual game state.
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="combat">The current combat.</param>
			/// <param name="card">The card this context is for, if any.</param>
			/// <returns>A new payment environment, basing on the actual game state.</returns>
			IPaymentEnvironment MakeStatePaymentEnvironment(State state, Combat combat, Card? card);
			
			/// <summary>
			/// Creates a new payment transaction.
			/// </summary>
			/// <returns>The new payment transaction.</returns>
			ITransaction MakeTransaction();
			
			/// <summary>
			/// Calculates the best payment transaction to fulfill the given cost.
			/// </summary>
			/// <param name="cost">The cost to fulfill.</param>
			/// <param name="environment">The payment environment.</param>
			/// <returns>The payment transaction.</returns>
			ITransaction GetBestTransaction(ICost cost, IPaymentEnvironment environment);

			/// <summary>
			/// Asks all hooks for a modified action cost.
			/// </summary>
			/// <param name="cost">The action cost to modify.</param>
			/// <param name="state">The game state.</param>
			/// <param name="combat">The current combat.</param>
			/// <param name="card">The card the action cost came from, if any.</param>
			/// <param name="action">The action the cost is for, if any.</param>
			/// <returns>The modified action cost.</returns>
			ICost ModifyActionCost(ICost cost, State state, Combat combat, Card? card, CardAction? action);
			
			/// <summary>
			/// Registers a new hook related to action costs.
			/// </summary>
			/// <param name="hook">The hook.</param>
			/// <param name="priority">The priority for the hook. Higher priority hooks are called before lower priority ones. Defaults to <c>0</c>.</param>
			void RegisterHook(IHook hook, double priority = 0);
			
			/// <summary>
			/// Unregisters the given hook related to action costs.
			/// </summary>
			/// <param name="hook">The hook.</param>
			void UnregisterHook(IHook hook);
			
			/// <summary>
			/// Represents an action with a resource cost.
			/// </summary>
			public interface ICostAction : ICardAction<CardAction>
			{
				/// <summary>
				/// The cost of the action.
				/// </summary>
				ICost Cost { get; set; }
				
				/// <summary>
				/// The action to run.
				/// </summary>
				CardAction Action { get; set; }
				
				/// <summary>
				/// Values which visually override how many of a resource the player has.
				/// </summary>
				/// <para>
				/// This can be useful in case of more complex cards, which may change the amount of a required resource before the cost action starts running.
				/// </para>
				IDictionary<IResource, int> ResourceAmountDisplayOverrides { get; set; }

				/// <summary>
				/// Sets <see cref="Cost"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICostAction SetCost(ICost value);
				
				/// <summary>
				/// Sets <see cref="Action"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICostAction SetAction(CardAction value);
				
				/// <summary>
				/// Sets a single value of <see cref="ResourceAmountDisplayOverrides"/>.
				/// </summary>
				/// <param name="resource">The resource to change the override for.</param>
				/// <param name="amount">The amount to set.</param>
				/// <returns>This object after the change.</returns>
				ICostAction SetResourceAmountDisplayOverride(IResource resource, int amount);
				
				/// <summary>
				/// Sets <see cref="ResourceAmountDisplayOverrides"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICostAction SetResourceAmountDisplayOverrides(IDictionary<IResource, int> value);
			}

			/// <summary>
			/// Describes a type that can provide the resources for action costs.
			/// </summary>
			public interface IResourceProvider
			{
				/// <summary>
				/// Returns the current amount of the given resource in the provided game state.
				/// </summary>
				/// <param name="args">The arguments for the method.</param>
				/// <returns>The current amount of the resource.</returns>
				int GetCurrentResourceAmount(IGetCurrentResourceAmountArgs args);
				
				/// <summary>
				/// Decreases the given resource in the provided game state by the given amount.
				/// </summary>
				/// <param name="args">The arguments for the method.</param>
				void PayResource(IPayResourceArgs args);

				/// <summary>
				/// The arguments for the <see cref="GetCurrentResourceAmount"/> method.
				/// </summary>
				public interface IGetCurrentResourceAmountArgs
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
					/// The card this context is for, if any.
					/// </summary>
					Card? Card { get; }
					
					/// <summary>
					/// The resource.
					/// </summary>
					IResource Resource { get; }
				}
				
				/// <summary>
				/// The arguments for the <see cref="PayResource"/> method.
				/// </summary>
				public interface IPayResourceArgs
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
					/// The card this context is for, if any.
					/// </summary>
					Card? Card { get; }
					
					/// <summary>
					/// The resource.
					/// </summary>
					IResource Resource { get; }
					
					/// <summary>
					/// The amount of the resource to remove.
					/// </summary>
					int Amount { get; }
				}
			}
			
			/// <summary>
			/// Represents a type of a resource that can be used in action costs.
			/// </summary>
			public interface IResource
			{
				/// <summary>
				/// A unique key of the resource.
				/// </summary>
				string ResourceKey { get; }
				
				/// <summary>
				/// Returns the current amount of the resource in the provided game state.
				/// </summary>
				/// <param name="state">The game state.</param>
				/// <param name="combat">The current combat.</param>
				/// <returns>The current amount of the resource.</returns>
				int GetCurrentResourceAmount(State state, Combat combat);
				
				/// <summary>
				/// Decreases the resource in the provided game state by the given amount.
				/// </summary>
				/// <param name="state">The game state.</param>
				/// <param name="combat">The current combat.</param>
				/// <param name="amount">The amount of the resource to remove.</param>
				void Pay(State state, Combat combat, int amount);

				/// <summary>
				/// Provides an action that can be used to change the amount of the given resource.
				/// </summary>
				/// <param name="state">The game state.</param>
				/// <param name="combat">The current combat.</param>
				/// <param name="amount">The amount to change to/with, according to the change mode.</param>
				/// <param name="mode">The change mode.</param>
				/// <returns>An action that can be used to change the amount of the given resource.</returns>
				CardAction? GetChangeAction(State state, Combat combat, int amount, AStatusMode mode = AStatusMode.Add)
					=> null;
				
				/// <summary>
				/// Provides a list of tooltips for this resource cost.
				/// </summary>
				/// <param name="state">The game state.</param>
				/// <param name="combat">The current combat.</param>
				/// <param name="amount">The amount of the resource that will be paid.</param>
				/// <returns>The list of tooltips for this resource cost.</returns>
				IReadOnlyList<Tooltip> GetTooltips(State state, Combat combat, int amount);
			}

			/// <summary>
			/// Represents a <see cref="Status">status</see>-based type of a resource that can be used in action costs.
			/// </summary>
			public interface IStatusResource : IResource
			{
				/// <summary>
				/// The <see cref="Status">status</see>.
				/// </summary>
				Status Status { get; }
				
				/// <summary>
				/// Whether the resource looks for the status on the player's ship (<c>true</c>) or the enemy's ship (<c>false</c>).
				/// </summary>
				bool TargetPlayer { get; }
			}

			/// <summary>
			/// Represents the energy resource that can be used in action costs.
			/// </summary>
			public interface IEnergyResource : IResource;

			/// <summary>
			/// Represents some kind of a payment environment - either one based on real game state, or a mock one.
			/// </summary>
			public interface IPaymentEnvironment
			{
				/// <summary>
				/// Returns the available amount of the given resource.
				/// </summary>
				/// <param name="resource">The resource.</param>
				/// <returns>The available amount of the given resource.</returns>
				int GetAvailableResource(IResource resource);
				
				/// <summary>
				/// Attempts to pay the given amount of a resource.
				/// </summary>
				/// <param name="resource">The resource to pay with.</param>
				/// <param name="amount">The amount of the resource to pay.</param>
				/// <returns>Whether payment was successful.</returns>
				bool TryPayResource(IResource resource, int amount);
			}

			/// <summary>
			/// Represents a mock payment environment.
			/// </summary>
			public interface IMockPaymentEnvironment : IPaymentEnvironment
			{
				/// <summary>
				/// Sets the available amount of a resource in this mock environment.
				/// </summary>
				/// <param name="resource">The resource.</param>
				/// <param name="amount">The new available amount.</param>
				/// <returns>This object after the change.</returns>
				IMockPaymentEnvironment SetAvailableResource(IResource resource, int amount);
			}

			/// <summary>
			/// Represents a transaction consisting of multiple payments of various resources.
			/// </summary>
			public interface ITransaction
			{
				/// <summary>
				/// A dictionary summing up the amount to pay per each resource.
				/// </summary>
				IReadOnlyDictionary<IResource, int> Resources { get; }
				
				/// <summary>
				/// A list of all payments in this transaction.
				/// </summary>
				IReadOnlyList<ITransactionPayment> Payments { get; }

				/// <summary>
				/// Tests this transaction against the given payment environment, without actually paying.
				/// </summary>
				/// <param name="environment">The payment environment.</param>
				/// <returns>The result of the transaction.</returns>
				IWholeTransactionPaymentResult TestPayment(IPaymentEnvironment environment);
				
				/// <summary>
				/// Pays for this transaction in the given payment environment.
				/// </summary>
				/// <param name="environment">The payment environment.</param>
				/// <returns>The result of the transaction.</returns>
				IWholeTransactionPaymentResult Pay(IPaymentEnvironment environment);

				/// <summary>
				/// Constructs a new transaction consisting of all payments in this transaction, and an extra payment.
				/// </summary>
				/// <param name="context">The context of the payment, describing the <see cref="ICost"/> it came from.</param>
				/// <param name="resource">The resource to pay with.</param>
				/// <param name="amount">The amount of the resource to pay.</param>
				/// <returns>A new transaction consisting of all payments in this transaction, and an extra payment.</returns>
				ITransaction AddPayment(IReadOnlyList<ICost> context, IResource resource, int amount);
			}

			/// <summary>
			/// Represents a single payment of a <see cref="ITransaction">transaction</see>.
			/// </summary>
			public interface ITransactionPayment
			{
				/// <summary>
				/// The context of the payment, describing the <see cref="ICost"/> it came from.
				/// </summary>
				IReadOnlyList<ICost> Context { get; }
				
				/// <summary>
				/// The resource to pay with.
				/// </summary>
				IResource Resource { get; }
				
				/// <summary>
				/// The amount of the resource to pay.
				/// </summary>
				int Amount { get; }
			}

			/// <summary>
			/// Represents the result of a single <see cref="ITransactionPayment">transaction payment</see>.
			/// </summary>
			public interface ITransactionPaymentResult
			{
				/// <summary>
				/// The payment this result is for.
				/// </summary>
				ITransactionPayment Payment { get; }
				
				/// <summary>
				/// The amount of the resource that could be paid.
				/// </summary>
				int Paid { get; }
				
				/// <summary>
				/// The amount of the resource that could not be paid.
				/// </summary>
				int Unpaid { get; }
			}

			/// <summary>
			/// Represents the result of a whole <see cref="ITransaction">transaction</see> payment.
			/// </summary>
			public interface IWholeTransactionPaymentResult
			{
				/// <summary>
				/// The transaction this result is for.
				/// </summary>
				ITransaction Transaction { get; }
				
				/// <summary>
				/// A dictionary summing up the amount that could be paid per each resource.
				/// </summary>
				IReadOnlyDictionary<IResource, int> PaidResources { get; }
				
				/// <summary>
				/// A dictionary summing up the amount that could not be paid per each resource.
				/// </summary>
				IReadOnlyDictionary<IResource, int> UnpaidResources { get; }
				
				/// <summary>
				/// A list of results for all payments of the transaction.
				/// </summary>
				IReadOnlyList<ITransactionPaymentResult> Payments { get; }
				
				/// <summary>
				/// The total amount of resources that could be paid.
				/// </summary>
				int TotalPaid { get; }
				
				/// <summary>
				/// The total amount of resources that could not be paid.
				/// </summary>
				int TotalUnpaid { get; }
			}

			/// <summary>
			/// Represents an action cost.
			/// </summary>
			public interface ICost
			{
				/// <summary>
				/// A list of resources this action cost monitors.
				/// </summary>
				IReadOnlySet<IResource> MonitoredResources { get; }

				/// <summary>
				/// Enumerates all possible transactions that could satisfy this cost.
				/// </summary>
				/// <param name="context">The current context of transaction payments, describing the <see cref="ICost"/> they came from.</param>
				/// <param name="baseTransaction">The base transaction to build upon.</param>
				/// <returns>An enumerable over all possible transactions that could satisfy this cost.</returns>
				IEnumerable<ITransaction> GetPossibleTransactions(IReadOnlyList<ICost> context, ITransaction baseTransaction);
				
				/// <summary>
				/// Renders the action cost.
				/// </summary>
				/// <param name="g">The global game state.</param>
				/// <param name="position">The modifiable position to render at.</param>
				/// <param name="isDisabled">Whether the action is disabled.</param>
				/// <param name="dontRender"><c>true</c> when the method is only called to retrieve the width of the action, <c>false</c> if it should actually be rendered.</param>
				/// <param name="transactionPaymentResult">The result of the test payment of the chosen best transaction for the cost.</param>
				void Render(G g, ref Vec position, bool isDisabled, bool dontRender, IWholeTransactionPaymentResult transactionPaymentResult);
				
				/// <summary>
				/// Provides a list of tooltips for this action cost.
				/// </summary>
				/// <param name="state">The game state.</param>
				/// <param name="combat">The current combat.</param>
				/// <returns>The list of tooltips for this action cost.</returns>
				IReadOnlyList<Tooltip> GetTooltips(State state, Combat combat);
			}

			/// <summary>
			/// Represents a combined action cost, which requires multiple different costs to be paid.
			/// </summary>
			public interface ICombinedCost : ICost
			{
				/// <summary>
				/// The list of costs that need to be paid.
				/// </summary>
				IList<ICost> Costs { get; set; }
				
				/// <summary>
				/// The rendering spacing between the costs.
				/// </summary>
				int Spacing { get; set; }

				/// <summary>
				/// Sets <see cref="Costs"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICombinedCost SetCosts(IEnumerable<ICost> value);
				
				/// <summary>
				/// Sets <see cref="Spacing"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICombinedCost SetSpacing(int value);
			}

			/// <summary>
			/// Represents a resource action cost.
			/// </summary>
			public interface IResourceCost : ICost
			{
				/// <summary>
				/// The resources that can be used.
				/// </summary>
				IList<IResource> PotentialResources { get; set; }
				
				/// <summary>
				/// The amount of the resources.
				/// </summary>
				int Amount { get; set; }
				
				/// <summary>
				/// The style to render the cost with. Defaults to <see cref="ResourceCostDisplayStyle.RepeatedIcon"/>.
				/// </summary>
				ResourceCostDisplayStyle DisplayStyle { get; set; }
				
				/// <summary>
				/// The spacing between multiple rendered icons.
				/// </summary>
				int Spacing { get; set; }
				
				/// <summary>
				/// Whether an additional "outgoing" icon should be rendered before the cost.
				/// </summary>
				bool ShowOutgoingIcon { get; set; }
				
				/// <summary>
				/// An override for the icons to use when the cost can be paid.
				/// </summary>
				IReadOnlyList<Spr>? CostSatisfiedIconOverride { get; set; }
				
				/// <summary>
				/// An override for the icons to use when the cost cannot be paid.
				/// </summary>
				IReadOnlyList<Spr>? CostUnsatisfiedIconOverride { get; set; }

				/// <summary>
				/// Sets <see cref="PotentialResources"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IResourceCost SetPotentialResources(IEnumerable<IResource> value);
				
				/// <summary>
				/// Sets <see cref="Amount"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IResourceCost SetAmount(int value);
				
				/// <summary>
				/// Sets <see cref="DisplayStyle"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IResourceCost SetDisplayStyle(ResourceCostDisplayStyle value);
				
				/// <summary>
				/// Sets <see cref="Spacing"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IResourceCost SetSpacing(int value);
				
				/// <summary>
				/// Sets <see cref="ShowOutgoingIcon"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IResourceCost SetShowOutgoingIcon(bool value);
				
				/// <summary>
				/// Sets <see cref="CostSatisfiedIconOverride"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IResourceCost SetCostSatisfiedIconOverride(IReadOnlyList<Spr>? value);
				
				/// <summary>
				/// Sets <see cref="CostUnsatisfiedIconOverride"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IResourceCost SetCostUnsatisfiedIconOverride(IReadOnlyList<Spr>? value);
			}
			
			/// <summary>
			/// Describes the style to render <see cref="IResourceCost">resource action costs</see> with.
			/// </summary>
			[JsonConverter(typeof(StringEnumConverter))]
			public enum ResourceCostDisplayStyle
			{
				/// <summary>
				/// Render multiple (overlapping) resource icons in a row.
				/// </summary>
				RepeatedIcon,
				
				/// <summary>
				/// Render a single resource icon, followed by a number for the amount.
				/// </summary>
				IconAndNumber
			}

			/// <summary>
			/// Describes a single icon to render for a resource action cost.
			/// </summary>
			public interface IResourceCostIcon
			{
				/// <summary>
				/// The amount of the resource this icon represents.
				/// </summary>
				int Amount { get; }
				
				/// <summary>
				/// The icon to use if the resource can be paid.
				/// </summary>
				Spr CostSatisfiedIcon { get; }
				
				/// <summary>
				/// The icon to use if the resource cannot be paid.
				/// </summary>
				Spr CostUnsatisfiedIcon { get; }
			}
			
			/// <summary>
			/// A hook related to action costs.
			/// </summary>
			public interface IHook : IKokoroV2ApiHook
			{
				/// <summary>
				/// Allows modifying the action cost before rendering or paying for it.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns><c>true</c> if the event is considered handled and no further hooks should be called; <c>false</c> otherwise.</returns>
				bool ModifyActionCost(IModifyActionCostArgs args) => false;

				/// <summary>
				/// An event called whenever any action costs are paid.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				void OnActionCostsTransactionFinished(IOnActionCostsTransactionFinishedArgs args) { }

				/// <summary>
				/// The arguments for the <see cref="ModifyActionCost"/> hook method.
				/// </summary>
				public interface IModifyActionCostArgs
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
					/// The card the action cost came from, if any.
					/// </summary>
					Card? Card { get; }
					
					/// <summary>
					/// The action the cost is for, if any.
					/// </summary>
					CardAction? Action { get; }
					
					/// <summary>
					/// The action cost to modify, or the modified cost.
					/// </summary>
					ICost Cost { get; set; }
				}

				/// <summary>
				/// The arguments for the <see cref="OnActionCostsTransactionFinished"/> hook method.
				/// </summary>
				public interface IOnActionCostsTransactionFinishedArgs
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
					/// The card the action cost came from, if any.
					/// </summary>
					Card? Card { get; }
					
					/// <summary>
					/// The result of the payment of the chosen best transaction for the cost.
					/// </summary>
					IWholeTransactionPaymentResult TransactionPaymentResult { get; }
				}
			}
		}
	}
}
