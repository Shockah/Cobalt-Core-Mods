using System.Collections.Generic;
using System.Linq;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	IActionCostApi ActionCosts { get; }

	public interface IActionCostApi
	{
		CardAction Make(IActionCost cost, CardAction action);
		CardAction Make(IReadOnlyList<IActionCost> costs, CardAction action);

		IActionCost Cost(IReadOnlyList<IResource> potentialResources, int amount = 1, int? iconOverlap = null, Spr? costUnsatisfiedIcon = null, Spr? costSatisfiedIcon = null, int? iconWidth = null, CustomCostTooltipProvider? customTooltipProvider = null);
		IActionCost Cost(IResource resource, int amount = 1, int? iconOverlap = null, CustomCostTooltipProvider? customTooltipProvider = null);

		IResource StatusResource(Status status, Spr costUnsatisfiedIcon, Spr costSatisfiedIcon, int? iconWidth = null);
		IResource StatusResource(Status status, StatusResourceTarget target, Spr costUnsatisfiedIcon, Spr costSatisfiedIcon, int? iconWidth = null);
		IResource EnergyResource();

		public delegate List<Tooltip> CustomCostTooltipProvider(State state, Combat? combat, IReadOnlyList<IResource> potentialResources, int amount);

		public interface IActionCost
		{
			IReadOnlyList<IResource> PotentialResources { get; }
			int ResourceAmount { get; }
			Spr? CostUnsatisfiedIcon { get; }
			Spr? CostSatisfiedIcon { get; }

			void RenderPrefix(G g, ref Vec position, bool isDisabled, bool dontRender)
				=> PotentialResources.FirstOrDefault()?.RenderPrefix(g, ref position, isDisabled, dontRender);

			void RenderSuffix(G g, ref Vec position, bool isDisabled, bool dontRender)
				=> PotentialResources.FirstOrDefault()?.RenderSuffix(g, ref position, isDisabled, dontRender);

			void RenderSingle(G g, ref Vec position, IResource? satisfiedResource, bool isDisabled, bool dontRender);
			List<Tooltip> GetTooltips(State state, Combat? combat) => [];
		}

		public interface IResource
		{
			string ResourceKey { get; }
			Spr? CostUnsatisfiedIcon { get; }
			Spr? CostSatisfiedIcon { get; }

			int GetCurrentResourceAmount(State state, Combat combat);
			void PayResource(State state, Combat combat, int amount);
			void RenderPrefix(G g, ref Vec position, bool isDisabled, bool dontRender) { }
			void RenderSuffix(G g, ref Vec position, bool isDisabled, bool dontRender) { }
			void Render(G g, ref Vec position, bool isSatisfied, bool isDisabled, bool dontRender);
			List<Tooltip> GetTooltips(State state, Combat? combat, int amount) => [];
		}

		public enum StatusResourceTarget
		{
			Player,
			Enemy,
			EnemyWithOutgoingArrow
		}
	}
}