using CobaltCoreModding.Definitions.ExternalItems;

namespace Shockah.Soggins;

public sealed class ApiImplementation : ISogginsApi
{
	private const string IsSmugEnabledKey = "IsSmugEnabled";

	private static ModEntry Instance => ModEntry.Instance;

	public Tooltip FrogproofCardTraitTooltip
		=> new CustomTTGlossary(CustomTTGlossary.GlossaryType.cardtrait, () => (Spr)Instance.FrogproofSprite.Id!.Value, () => I18n.FrogproofCardTraitName, () => I18n.FrogproofCardTraitText);

	public ExternalStatus FrogproofingStatus
		=> Instance.FrogproofingStatus;

	public Tooltip FrogproofingTooltip
		=> new TTGlossary($"status.{Instance.FrogproofingStatus.Id!.Value}");

	public ExternalStatus SmugStatus
		=> Instance.SmugStatus;

	public Tooltip GetSmugTooltip()
		=> new TTGlossary($"status.{Instance.SmugStatus.Id!.Value}");

	public Tooltip GetSmugTooltip(State state, Ship ship)
		=> GetSmugTooltip();

	public bool IsSmugEnabled(Ship ship)
		=> Instance.KokoroApi.TryGetExtensionData(ship, IsSmugEnabledKey, out bool value) && value;

	public void SetSmugEnabled(Ship ship, bool enabled = true)
		=> Instance.KokoroApi.SetExtensionData(ship, IsSmugEnabledKey, enabled);

	public int GetMinSmug(Ship ship)
		=> -Constants.BotchChances.Length / 2;

	public int GetMaxSmug(Ship ship)
		=> Constants.BotchChances.Length / 2;

	public int? GetSmug(Ship ship)
	{
		if (!IsSmugEnabled(ship))
			return null;
		return ship.Get((Status)Instance.SmugStatus.Id!.Value);
	}

	public bool IsOversmug(Ship ship)
	{
		var smug = GetSmug(ship);
		return smug is not null && smug.Value > GetMaxSmug(ship);
	}

	public double GetSmugBotchChance(State state, Ship ship, Card? card)
		=> Instance.SmugStatusManager.GetSmugBotchChance(state, ship, card);

	public double GetSmugDoubleChance(State state, Ship ship, Card? card)
		=> Instance.SmugStatusManager.GetSmugDoubleChance(state, ship, card);

	public int GetTimesBotchedThisCombat(State state, Combat combat)
		=> state.ship.Get((Status)Instance.BotchesStatus.Id!.Value);

	public SmugResult RollSmugResult(State state, Ship ship, Rand rng, Card? card)
	{
		var botchChance = GetSmugBotchChance(state, ship, card);
		var doubleChance = GetSmugDoubleChance(state, ship, card);
		return SmugStatusManager.GetSmugResult(rng, botchChance, doubleChance);
	}

	public Card GenerateAndTrackApology(State state, Combat combat, Rand rng)
		=> SmugStatusManager.GenerateAndTrackApology(state, combat, rng);

	public Card MakePlaceholderApology()
		=> new RandomPlaceholderApologyCard();

	public void RegisterSmugHook(ISmugHook hook, double priority)
		=> Instance.SmugStatusManager.Register(hook, priority);

	public void UnregisterSmugHook(ISmugHook hook)
		=> Instance.SmugStatusManager.Unregister(hook);

	public bool IsFrogproof(Card card)
		=> card is ChipShot;

	public bool IsFrogproof(State state, Combat? combat, Card card, FrogproofHookContext context)
		=> Instance.FrogproofManager.IsFrogproof(state, combat, card, context);

	public void RegisterFrogproofHook(IFrogproofHook hook, double priority)
		=> Instance.FrogproofManager.Register(hook, priority);

	public void UnregisterFrogproofHook(IFrogproofHook hook)
		=> Instance.FrogproofManager.Unregister(hook);
}
