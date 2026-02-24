using System.Linq;
using CobaltCoreModding.Definitions.ExternalItems;
using Microsoft.Extensions.Logging;
using Nickel;

namespace Shockah.Soggins;

public sealed class ApiImplementation : ISogginsApi
{
	private const string IsSmugEnabledKey = "IsSmugEnabled";
	internal const string IsRunWithSmugKey = "IsRunWithSmug";

	private static ModEntry Instance => ModEntry.Instance;

	public ExternalDeck SogginsDeck
		=> Instance.SogginsDeck;

	public Deck SogginsVanillaDeck
		=> (Deck)Instance.SogginsDeck.Id!.Value;

	public ICardTraitEntry FrogproofTrait
		=> Instance.FrogproofTrait;

	public Tooltip FrogproofCardTraitTooltip
		=> new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Frogproof")
		{
			Icon = (Spr)Instance.FrogproofSprite.Id!.Value,
			TitleColor = Colors.cardtrait,
			Title = I18n.FrogproofCardTraitName,
			Description = I18n.FrogproofCardTraitText,
		};

	public ExternalStatus FrogproofingStatus
		=> Instance.FrogproofingStatus;

	public Status FrogproofingVanillaStatus
		=> (Status)Instance.FrogproofingStatus.Id!.Value;

	public Tooltip FrogproofingTooltip
		=> new TTGlossary($"status.{Instance.FrogproofingStatus.Id!.Value}");

	public ExternalStatus SmugStatus
		=> Instance.SmugStatus;

	public Status SmugVanillaStatus
		=> (Status)Instance.SmugStatus.Id!.Value;

	public Tooltip GetSmugTooltip()
		=> new TTGlossary($"status.{Instance.SmugStatus.Id!.Value}");

	public Tooltip GetSmugTooltip(State state, Ship ship)
		=> GetSmugTooltip();

	public bool IsRunWithSmug(State state)
		=> Instance.Helper.ModData.GetModDataOrDefault<bool>(state, IsRunWithSmugKey);

	public bool IsSmugEnabled(State state, Ship ship)
		=> Instance.Helper.ModData.GetModDataOrDefault<bool>(ship, IsSmugEnabledKey);

	public void SetSmugEnabled(State state, Ship ship, bool enabled = true)
	{
		if (enabled && ship == state.ship)
			Instance.Helper.ModData.SetModData(state, IsRunWithSmugKey, true);
		Instance.Helper.ModData.SetModData(ship, IsSmugEnabledKey, enabled);
	}

	public int GetMinSmug(Ship ship)
		=> -Constants.BotchChances.Length / 2;

	public int GetMaxSmug(Ship ship)
		=> Constants.BotchChances.Length / 2;

	public int? GetSmug(State state, Ship ship)
	{
		if (!IsSmugEnabled(state, ship))
			return null;
		return ship.Get((Status)Instance.SmugStatus.Id!.Value);
	}

	public bool IsOversmug(State state, Ship ship)
	{
		var smug = GetSmug(state, ship);
		return smug is not null && smug.Value > GetMaxSmug(ship);
	}

	public double GetSmugBotchChance(State state, Ship ship, Card? card)
		=> Instance.SmugStatusManager.GetSmugBotchChance(state, ship, card);

	public double GetSmugDoubleChance(State state, Ship ship, Card? card)
		=> Instance.SmugStatusManager.GetSmugDoubleChance(state, ship, card);

	public int GetTimesBotchedThisCombat(State state, Combat combat)
		=> state.EnumerateAllArtifacts().OfType<BotchTrackerArtifact>().FirstOrDefault()?.Botches ?? 0;

	public SmugResult RollSmugResult(State state, Ship ship, Rand rng, Card? card)
	{
		var botchChance = GetSmugBotchChance(state, ship, card);
		var doubleChance = GetSmugDoubleChance(state, ship, card);
		return SmugStatusManager.GetSmugResult(rng, botchChance, doubleChance);
	}

	public bool IsCurrentlyDoubling(State state, Combat combat)
		=> Instance.SmugStatusManager.IsDoubling;

	public Card GenerateAndTrackApology(State state, Combat combat, Rand rng)
		=> SmugStatusManager.GenerateAndTrackApology(state, combat, rng);

	public Card MakePlaceholderApology()
		=> new RandomPlaceholderApologyCard();

	public void RegisterSmugHook(ISmugHook hook, double priority)
		=> Instance.SmugStatusManager.Register(hook, priority);

	public void UnregisterSmugHook(ISmugHook hook)
		=> Instance.SmugStatusManager.Unregister(hook);

	public bool IsFrogproof(State state, Combat? combat, Card card, FrogproofHookContext context)
		=> FrogproofManager.IsFrogproof(state, card);

	public void RegisterFrogproofHook(IFrogproofHook hook, double priority)
	{
		var originalHook = Instance.Helper.Utilities.Unproxy(hook);
		var modAssemblyName = originalHook.GetType().Assembly.GetName().Name;
		Instance.Logger!.LogWarning("Mod {Mod} attempted to use `RegisterFrogproofHook`, but this method is no longer supported.", modAssemblyName);
	}

	public void UnregisterFrogproofHook(IFrogproofHook hook)
	{
	}
}
