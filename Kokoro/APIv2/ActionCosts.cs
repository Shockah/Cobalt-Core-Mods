using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IActionCostsApi ActionCosts { get; }

		public interface IActionCostsApi
		{
			public interface IHook : IKokoroV2ApiHook
			{
				void OnActionCostsTransactionFinished(IOnActionCostsTransactionFinishedArgs args) { }

				public interface IOnActionCostsTransactionFinishedArgs
				{
					State State { get; }
					Combat Combat { get; }
					Card? Card { get; }
					IWholeTransactionPaymentResult TransactionPaymentResult { get; }
				}
			}
			
			public interface ICostAction : ICardAction<CardAction>
			{
				ICost Cost { get; set; }
				CardAction Action { get; set; }
				IDictionary<IResource, int> ResourceAmountDisplayOverrides { get; set; }

				ICostAction SetCost(ICost value);
				ICostAction SetAction(CardAction value);
				ICostAction SetResourceAmountDisplayOverride(IResource resource, int amount);
				ICostAction SetResourceAmountDisplayOverrides(IDictionary<IResource, int> value);
			}
			
			public interface IResource
			{
				string ResourceKey { get; }
				
				int GetCurrentResourceAmount(State state, Combat combat);
				void Pay(State state, Combat combat, int amount);
				List<Tooltip> GetTooltips(State state, Combat? combat, int amount);
			}

			public interface IStatusResource : IResource
			{
				Status Status { get; }
				bool TargetPlayer { get; }
			}

			public interface IEnergyResource : IResource;

			public interface IPaymentEnvironment
			{
				int GetAvailableResource(IResource resource);
				bool TryPayResource(IResource resource, int amount);
			}

			public interface IMockPaymentEnvironment : IPaymentEnvironment
			{
				IMockPaymentEnvironment SetAvailableResource(IResource resource, int amount);
			}

			public interface ITransaction
			{
				IReadOnlyDictionary<IResource, int> Resources { get; }
				IReadOnlyList<ITransactionPayment> Payments { get; }

				IWholeTransactionPaymentResult TestPayment(IPaymentEnvironment environment);
				IWholeTransactionPaymentResult Pay(IPaymentEnvironment environment);

				ITransaction AddPayment(IReadOnlyList<ICost> context, IResource resource, int amount);
			}

			public interface ITransactionPayment
			{
				IReadOnlyList<ICost> Context { get; }
				IResource Resource { get; }
				int Amount { get; }
			}

			public interface ITransactionPaymentResult
			{
				ITransactionPayment Payment { get; }
				int Paid { get; }
				int Unpaid { get; }
			}

			public interface IWholeTransactionPaymentResult
			{
				ITransaction Transaction { get; }
				IReadOnlyDictionary<IResource, int> PaidResources { get; }
				IReadOnlyDictionary<IResource, int> UnpaidResources { get; }
				IReadOnlyList<ITransactionPaymentResult> Payments { get; }
				int TotalPaid { get; }
				int TotalUnpaid { get; }
			}

			public interface ICost
			{
				IReadOnlyList<IResource> MonitoredResources { get; }

				IEnumerable<ITransaction> GetPossibleTransactions(IReadOnlyList<ICost> context, ITransaction baseTransaction);
				void Render(G g, ref Vec position, bool isDisabled, bool dontRender, IWholeTransactionPaymentResult transactionPaymentResult);
				List<Tooltip> GetTooltips(State state, Combat? combat);
			}

			public interface ICombinedCost : ICost
			{
				IList<ICost> Costs { get; set; }
				int Spacing { get; set; }

				ICombinedCost SetCosts(IEnumerable<ICost> value);
				ICombinedCost SetSpacing(int value);
			}

			public interface IResourceCost : ICost
			{
				IList<IResource> PotentialResources { get; set; }
				int Amount { get; set; }
				ResourceCostDisplayStyle DisplayStyle { get; set; }
				int Spacing { get; set; }
				bool ShowOutgoingIcon { get; set; }
				Spr? CostSatisfiedIconOverride { get; set; }
				Spr? CostUnsatisfiedIconOverride { get; set; }

				IResourceCost SetPotentialResources(IEnumerable<IResource> value);
				IResourceCost SetAmount(int value);
				IResourceCost SetDisplayStyle(ResourceCostDisplayStyle value);
				IResourceCost SetSpacing(int value);
				IResourceCost SetShowOutgoingIcon(bool value);
				IResourceCost SetCostSatisfiedIconOverride(Spr? value);
				IResourceCost SetCostUnsatisfiedIconOverride(Spr? value);
			}
			
			[JsonConverter(typeof(StringEnumConverter))]
			public enum ResourceCostDisplayStyle
			{
				RepeatedIcon,
				IconAndNumber
			}

			public interface IResourceCostIcon
			{
				int Amount { get; }
				Spr CostSatisfiedIcon { get; }
				Spr CostUnsatisfiedIcon { get; }
			}
			
			void RegisterHook(IHook hook, double priority = 0);
			void UnregisterHook(IHook hook);

			ICostAction? AsCostAction(CardAction action);
			ICostAction MakeCostAction(ICost cost, CardAction action);

			void RegisterResourceCostIcon(IResource resource, Spr costSatisfiedIcon, Spr costUnsatisfiedIcon, int amount = 1);
			void RegisterStatusResourceCostIcon(Status status, Spr costSatisfiedIcon, Spr costUnsatisfiedIcon, int amount = 1, bool? targetPlayer = null);
			IReadOnlyList<IResourceCostIcon> GetResourceCostIcons(IResource resource, int amount);
			
			IStatusResource? AsStatusResource(IResource resource);
			IStatusResource MakeStatusResource(Status status, bool targetPlayer = true);

			IEnergyResource? AsEnergyResource(IResource resource);
			IEnergyResource EnergyResource { get; }

			IResourceCost? AsResourceCost(ICost cost);
			IResourceCost MakeResourceCost(IResource resource, int amount);
			IResourceCost MakeResourceCost(IEnumerable<IResource> potentialResources, int amount);
			
			ICombinedCost? AsCombinedCost(ICost cost);
			ICombinedCost MakeCombinedCost(IEnumerable<ICost> costs);

			IMockPaymentEnvironment MakeMockPaymentEnvironment(IPaymentEnvironment? @default = null);
			IPaymentEnvironment MakeStatePaymentEnvironment(State state, Combat combat);
			ITransaction MakeTransaction();
			ITransaction GetBestTransaction(ICost cost, IPaymentEnvironment environment);
		}
	}
}
