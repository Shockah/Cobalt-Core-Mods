using CobaltCoreModding.Definitions.ExternalItems;
using System;

namespace Shockah.Soggins;

public sealed class ApiImplementation : ISogginsApi
{
	private static ModEntry Instance => ModEntry.Instance;

	private static readonly double[] BotchChances = new double[] { 0.15, 0.14, 0.12, 0.10, 0.08, 0.06, 0.05 };
	private static readonly double[] DoubleChances = new double[] { 0.05, 0.06, 0.08, 0.10, 0.12, 0.14, 0.15 };

	public Tooltip FrogproofCardTraitTooltip
		=> new CustomTTGlossary(CustomTTGlossary.GlossaryType.cardtrait, () => (Spr)Instance.FrogproofSprite.Id!.Value, () => I18n.FrogproofCardTraitName, () => I18n.FrogproofCardTraitText);

	public ExternalStatus FrogproofingStatus
		=> Instance.FrogproofingStatus;

	public ExternalStatus SmugStatus
		=> Instance.SmugStatus;

	public Tooltip GetSmugTooltip()
		=> new TTGlossary($"status.{Instance.SmugStatus.Id!.Value}");

	public Tooltip GetSmugTooltip(State state, Ship ship)
		=> GetSmugTooltip();

	public int GetMinSmug(Ship ship)
		=> -BotchChances.Length / 2;

	public int GetMaxSmug(Ship ship)
		=> BotchChances.Length / 2;

	public int? GetSmug(Ship ship)
	{
		var value = ship.Get((Status)Instance.SmugStatus.Id!.Value);
		return value <= 0 ? null : value - 100;
	}

	public void SetSmug(Ship ship, int? value)
		=> ship.Set((Status)Instance.SmugStatus.Id!.Value, value is null ? 0 : Math.Clamp(value.Value, GetMinSmug(ship), GetMaxSmug(ship) + 1) + 100);

	public CardAction MakeSetSmugAction(State state, int? value, bool targetPlayer = true, bool disabled = false)
	{
		var ship = targetPlayer ? state.ship : (state.route as Combat ?? DB.fakeCombat).otherShip;
		return new AStatus
		{
			status = (Status)Instance.SmugStatus.Id!.Value,
			mode = AStatusMode.Set,
			statusAmount = value is null ? 0 : Math.Clamp(value.Value, GetMinSmug(ship), GetMaxSmug(ship) + 1) + 100,
			targetPlayer = targetPlayer,
			disabled = disabled
		};
	}

	public void AddSmug(Ship ship, int value)
		=> SetSmug(ship, (GetSmug(ship) ?? 0) + value);

	public CardAction MakeAddSmugAction(State state, int value, bool targetPlayer = true, bool disabled = false)
		=> new AStatus
		{
			status = (Status)Instance.SmugStatus.Id!.Value,
			statusAmount = value,
			targetPlayer = targetPlayer,
			disabled = disabled
		};

	public bool IsOversmug(Ship ship)
	{
		var smug = GetSmug(ship);
		return smug is not null && smug.Value > GetMaxSmug(ship);
	}

	public double GetSmugBotchChance(Ship ship)
	{
		var smug = GetSmug(ship);
		if (smug is null)
			return 0;
		else if (smug.Value < GetMinSmug(ship))
			return BotchChances[0];
		else if (smug.Value > GetMaxSmug(ship))
			return 1; // oversmug
		else
			return BotchChances[smug.Value - GetMinSmug(ship)];
	}

	public double GetSmugDoubleChance(Ship ship)
	{
		var smug = GetSmug(ship);
		if (smug is null)
			return 0;
		else if (smug.Value < GetMinSmug(ship))
			return DoubleChances[0];
		else if (smug.Value > GetMaxSmug(ship))
			return 0; // oversmug
		else
			return DoubleChances[smug.Value - GetMinSmug(ship)];
	}

	public int GetTimesBotchedThisCombat(State state, Combat combat)
		=> SmugStatusManager.TimesBotchedThisCombat;

	public bool IsFrogproof(Card card)
		=> card is ChipShot;

	public bool IsFrogproof(State state, Combat? combat, Card card, FrogproofHookContext context)
		=> Instance.FrogproofManager.IsFrogproof(state, combat, card, context);

	public void RegisterFrogproofHook(IFrogproofHook hook, double priority)
		=> Instance.FrogproofManager.Register(hook, priority);

	public void UnregisterFrogproofHook(IFrogproofHook hook)
		=> Instance.FrogproofManager.Unregister(hook);
}
