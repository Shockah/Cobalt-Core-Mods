using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Kokoro;

internal sealed class ActionCostStatusResource : IKokoroApi.IActionCostApi.IResource
{
	[JsonProperty]
	public readonly Status Status;

	[JsonProperty]
	public readonly bool TargetPlayer;

	[JsonIgnore]
	public string ResourceKey
		=> $"Status.{(TargetPlayer ? "Player" : "Enemy")}.{Status.Key()}";

	[JsonIgnore]
	public Spr? CostUnsatisfiedIcon { get; }

	[JsonIgnore]
	public Spr? CostSatisfiedIcon { get; }

	[JsonConstructor]
	public ActionCostStatusResource(Status status, bool targetPlayer)
	{
		this.Status = status;
		this.TargetPlayer = targetPlayer;
	}

	public ActionCostStatusResource(Status status, bool targetPlayer, Spr? costUnsatisfiedIcon, Spr? costSatisfiedIcon) : this(status, targetPlayer)
	{
		this.CostUnsatisfiedIcon = costUnsatisfiedIcon;
		this.CostSatisfiedIcon = costSatisfiedIcon;
	}

	public int GetCurrentResourceAmount(State state, Combat combat)
	{
		var ship = TargetPlayer ? state.ship : combat.otherShip;
		return ship.Get(Status);
	}

	public void PayResource(State state, Combat combat, int amount)
	{
		var ship = TargetPlayer ? state.ship : combat.otherShip;
		ship.Add(Status, -amount);
	}

	public void Render(G g, ref Vec position, bool isSatisfied, bool isDisabled, bool dontRender)
	{
		var icon = (isSatisfied ? CostSatisfiedIcon : CostUnsatisfiedIcon) ?? DB.statuses[Status].icon;
		if (!dontRender)
			Draw.Sprite(icon, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 8;
	}
}

internal sealed class ActionCostImpl : IKokoroApi.IActionCostApi.IActionCost
{
	[JsonProperty]
	public IReadOnlyList<IKokoroApi.IActionCostApi.IResource> PotentialResources { get; }

	[JsonProperty]
	public int ResourceAmount { get; }

	[JsonIgnore]
	public Spr? CostUnsatisfiedIcon { get; }

	[JsonIgnore]
	public Spr? CostSatisfiedIcon { get; }

	[JsonConstructor]
	public ActionCostImpl(IReadOnlyList<IKokoroApi.IActionCostApi.IResource> potentialResources, int resourceAmount)
	{
		this.PotentialResources = potentialResources;
		this.ResourceAmount = resourceAmount;
	}

	public ActionCostImpl(IReadOnlyList<IKokoroApi.IActionCostApi.IResource> potentialResources, int resourceAmount, Spr? costUnsatisfiedIcon, Spr? costSatisfiedIcon) : this(potentialResources, resourceAmount)
	{
		this.CostUnsatisfiedIcon = costUnsatisfiedIcon;
		this.CostSatisfiedIcon = costSatisfiedIcon;
	}

	public void Render(G g, ref Vec position, IKokoroApi.IActionCostApi.IResource? satisfiedResource, bool isDisabled, bool dontRender)
	{
		if ((satisfiedResource is null ? CostUnsatisfiedIcon : CostSatisfiedIcon) is { } overriddenIcon)
		{
			if (!dontRender)
				Draw.Sprite(overriddenIcon, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
			position.x += 8;
		}
		else
		{
			(satisfiedResource ?? PotentialResources.FirstOrDefault())?.Render(g, ref position, isSatisfied: satisfiedResource is not null, isDisabled, dontRender);
		}
	}
}