using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Kokoro;

internal sealed class ActionCostStatusResource : IKokoroApi.IActionCostApi.IResource
{
	[JsonProperty]
	public readonly Status Status;

	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	public readonly IKokoroApi.IActionCostApi.StatusResourceTarget Target;

	[JsonIgnore]
	public string ResourceKey
		=> $"Status.{(Target == IKokoroApi.IActionCostApi.StatusResourceTarget.Player ? "Player" : "Enemy")}.{Status.Key()}";

	[JsonIgnore]
	public Spr? CostUnsatisfiedIcon { get; }

	[JsonIgnore]
	public Spr? CostSatisfiedIcon { get; }

	[JsonConstructor]
	public ActionCostStatusResource(Status status, IKokoroApi.IActionCostApi.StatusResourceTarget target)
	{
		this.Status = status;
		this.Target = target;
	}

	public ActionCostStatusResource(Status status, IKokoroApi.IActionCostApi.StatusResourceTarget target, Spr? costUnsatisfiedIcon, Spr? costSatisfiedIcon) : this(status, target)
	{
		this.CostUnsatisfiedIcon = costUnsatisfiedIcon;
		this.CostSatisfiedIcon = costSatisfiedIcon;
	}

	public int GetCurrentResourceAmount(State state, Combat combat)
	{
		var ship = Target == IKokoroApi.IActionCostApi.StatusResourceTarget.Player ? state.ship : combat.otherShip;
		return ship.Get(Status);
	}

	public void PayResource(State state, Combat combat, int amount)
	{
		var ship = Target == IKokoroApi.IActionCostApi.StatusResourceTarget.Player ? state.ship : combat.otherShip;
		ship.Add(Status, -amount);
	}

	public void RenderPrefix(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (Target != IKokoroApi.IActionCostApi.StatusResourceTarget.EnemyWithOutgoingArrow)
			return;

		if (!dontRender)
			Draw.Sprite(StableSpr.icons_outgoing, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 8;
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

	public void RenderSingle(G g, ref Vec position, IKokoroApi.IActionCostApi.IResource? satisfiedResource, bool isDisabled, bool dontRender)
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