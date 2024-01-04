using System.Collections.Generic;
using System.Linq;

namespace Shockah.Kokoro;

public sealed class AResourceCost : CardAction
{
	public List<IKokoroApi.IActionCostApi.IActionCost>? Costs;
	public CardAction? Action;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		if (Action is null)
			return;
		if (Costs is null)
		{
			c.QueueImmediate(Action);
			return;
		}

		var resourceState = GetCurrentResourceState(s, c, Costs.SelectMany(c => c.PotentialResources));
		var (_, resourcePayment, canPay) = GetResourcePayment(resourceState, Costs);

		if (!canPay)
			return;

		foreach (var (resourceKey, amount) in resourcePayment)
		{
			var resource = Costs.SelectMany(c => c.PotentialResources).First(r => r.ResourceKey == resourceKey);
			resource.PayResource(s, c, amount);
		}
		c.QueueImmediate(Action);
	}

	internal void RenderCosts(G g, ref Vec position, bool isDisabled, bool dontRender, List<(string ResourceKey, bool IsSatisfied)> payment)
	{
		if (Costs is null)
			return;

		int resourceIndex = 0;
		foreach (var cost in Costs)
		{
			for (int i = 0; i < cost.ResourceAmount; i++)
			{
				var (resourceKey, isResourceSatisfied) = payment[resourceIndex];
				var resource = cost.PotentialResources.FirstOrDefault(r => r.ResourceKey == resourceKey);
				cost.Render(g, ref position, isResourceSatisfied ? resource : null, isDisabled, dontRender);
				position.x -= 2;
				resourceIndex++;
			}
		}
		position.x += 2;
	}

	public override List<Tooltip> GetTooltips(State s)
	{
		List<Tooltip> tooltips = new();
		return tooltips;
	}

	public static Dictionary<string, int> GetCurrentResourceState(State state, Combat combat, IEnumerable<IKokoroApi.IActionCostApi.IResource> potentialResources)
	{
		Dictionary<string, int> resourceState = new();
		foreach (var resource in potentialResources)
			if (!resourceState.ContainsKey(resource.ResourceKey))
				resourceState[resource.ResourceKey] = resource.GetCurrentResourceAmount(state, combat);
		return resourceState;
	}

	public static (List<(string ResourceKey, bool IsSatisfied)> Payment, Dictionary<string, int> GroupedPayment, bool IsSatisfied) GetResourcePayment(Dictionary<string, int> resourceState, List<IKokoroApi.IActionCostApi.IActionCost> costs)
	{
		List<List<IKokoroApi.IActionCostApi.IResource>> toPay = new();
		foreach (var cost in costs)
			for (int i = 0; i < cost.ResourceAmount; i++)
				toPay.Add(cost.PotentialResources.ToList());

		List<(string ResourceKey, bool IsSatisfied)> payment = GetBestResourcePaymentOptions(resourceState, toPay).FirstOrDefault() ?? new();
		Dictionary<string, int> groupedPayment = payment.GroupBy(k => k.ResourceKey).ToDictionary(g => g.Key, g => g.Count());
		bool isSatisfied = payment.All(e => e.IsSatisfied);
		return (Payment: payment, GroupedPayment: groupedPayment, IsSatisfied: isSatisfied);
	}

	private static IEnumerable<List<(string ResourceKey, bool IsSatisfied)>> GetBestResourcePaymentOptions(Dictionary<string, int> resourceState, List<List<IKokoroApi.IActionCostApi.IResource>> toPay)
		=> GetResourcePaymentOptions(new(), toPay)
			.Select(o =>
			{
				List<(string ResourceKey, bool IsSatisfied)> resultOption = new();
				Dictionary<string, int> currentState = new(resourceState);
				foreach (var resourceKey in o)
				{
					bool isSatisfied = currentState.GetValueOrDefault(resourceKey) > 0;
					if (isSatisfied)
						currentState[resourceKey] = currentState.GetValueOrDefault(resourceKey) - 1;
					resultOption.Add((ResourceKey: resourceKey, IsSatisfied: isSatisfied));
				}
				return resultOption;
			})
			.OrderBy(o => o.Count(e => !e.IsSatisfied));

	private static IEnumerable<List<string>> GetResourcePaymentOptions(List<string> currentPayment, List<List<IKokoroApi.IActionCostApi.IResource>> toPayLeft)
	{
		if (toPayLeft.Count == 0)
		{
			yield return currentPayment;
			yield break;
		}

		var currentToPay = toPayLeft[0];
		var newToPayLeft = toPayLeft.Skip(1).ToList();

		foreach (var resource in currentToPay)
		{
			var newPayment = currentPayment.Append(resource.ResourceKey).ToList();
			foreach (var option in GetResourcePaymentOptions(newPayment, newToPayLeft))
				yield return option;
		}
	}

	private static IEnumerable<string> GetSingleResourcePaymentOptions(Dictionary<string, int> resourceState, IEnumerable<string> resources)
	{
		foreach (var resource in resources)
			if (resourceState.GetValueOrDefault(resource) > 0)
				yield return resource;
	}
}