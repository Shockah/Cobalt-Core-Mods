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

	[JsonIgnore]
	public int? IconWidth { get; }

	[JsonConstructor]
	public ActionCostStatusResource(Status status, IKokoroApi.IActionCostApi.StatusResourceTarget target)
	{
		this.Status = status;
		this.Target = target;
	}

	public ActionCostStatusResource(Status status, IKokoroApi.IActionCostApi.StatusResourceTarget target, Spr? costUnsatisfiedIcon, Spr? costSatisfiedIcon, int? iconWidth) : this(status, target)
	{
		this.CostUnsatisfiedIcon = costUnsatisfiedIcon;
		this.CostSatisfiedIcon = costSatisfiedIcon;
		this.IconWidth = iconWidth;
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
		position.x += IconWidth ?? 8;
	}

	public List<Tooltip> GetTooltips(State state, Combat? combat, int amount)
	{
		string nameFormat = Target == IKokoroApi.IActionCostApi.StatusResourceTarget.Player
			? I18n.StatusPlayerCostActionName : I18n.StatusEnemyCostActionName;
		string descriptionFormat = Target == IKokoroApi.IActionCostApi.StatusResourceTarget.Player
			? I18n.StatusPlayerCostActionDescription : I18n.StatusEnemyCostActionDescription;

		var icon = CostSatisfiedIcon ?? CostUnsatisfiedIcon ?? DB.statuses[Status].icon;
		string name = string.Format(nameFormat, Status.GetLocName().ToUpper());
		string description = string.Format(descriptionFormat, amount, Status.GetLocName().ToUpper());

		return new()
		{
			new CustomTTGlossary(
				CustomTTGlossary.GlossaryType.action,
				() => icon,
				() => name,
				() => description,
				key: $"{name}\n{description}"
			)
		};
	}
}

internal sealed class ActionCostImpl : IKokoroApi.IActionCostApi.IActionCost
{
	[JsonProperty]
	public IReadOnlyList<IKokoroApi.IActionCostApi.IResource> PotentialResources { get; }

	[JsonProperty]
	public int ResourceAmount { get; }

	[JsonProperty]
	public int? IconOverlap { get; }

	[JsonIgnore]
	public Spr? CostUnsatisfiedIcon { get; }

	[JsonIgnore]
	public Spr? CostSatisfiedIcon { get; }

	[JsonIgnore]
	public int? IconWidth { get; }

	[JsonIgnore]
	public IKokoroApi.IActionCostApi.CustomCostTooltipProvider? CustomTooltipProvider { get; }

	[JsonConstructor]
	public ActionCostImpl(IReadOnlyList<IKokoroApi.IActionCostApi.IResource> potentialResources, int resourceAmount, int? iconOverlap)
	{
		this.PotentialResources = potentialResources;
		this.ResourceAmount = resourceAmount;
		this.IconOverlap = iconOverlap;
	}

	public ActionCostImpl(IReadOnlyList<IKokoroApi.IActionCostApi.IResource> potentialResources, int resourceAmount, int? iconOverlap, Spr? costUnsatisfiedIcon, Spr? costSatisfiedIcon, int? iconWidth, IKokoroApi.IActionCostApi.CustomCostTooltipProvider? customTooltipProvider) : this(potentialResources, resourceAmount, iconOverlap)
	{
		this.CostUnsatisfiedIcon = costUnsatisfiedIcon;
		this.CostSatisfiedIcon = costSatisfiedIcon;
		this.IconWidth = iconWidth;
		this.CustomTooltipProvider = customTooltipProvider;
	}

	public void RenderSingle(G g, ref Vec position, IKokoroApi.IActionCostApi.IResource? satisfiedResource, bool isDisabled, bool dontRender)
	{
		if ((satisfiedResource is null ? CostUnsatisfiedIcon : CostSatisfiedIcon) is { } overriddenIcon)
		{
			if (!dontRender)
				Draw.Sprite(overriddenIcon, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
			position.x += IconWidth ?? 8;
		}
		else
		{
			(satisfiedResource ?? PotentialResources.FirstOrDefault())?.Render(g, ref position, isSatisfied: satisfiedResource is not null, isDisabled, dontRender);
		}
	}

	public List<Tooltip> GetTooltips(State state, Combat? combat)
	{
		if (CustomTooltipProvider is not null)
			return CustomTooltipProvider(state, combat, PotentialResources, ResourceAmount);
		return PotentialResources.FirstOrDefault()?.GetTooltips(state, combat, ResourceAmount) ?? new();
	}
}