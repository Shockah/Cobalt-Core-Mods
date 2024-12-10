using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Bjorn;

internal sealed class CrystalKnowledgeManager : IRegisterable
{
	internal static readonly Dictionary<string, ICrystalKnowledgeHandler> Handlers = [];
	internal static readonly HashSet<Deck> ShardCharacters = [];
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		SetIsShardCharacter(Deck.shard);
		RegisterHandler($"{package.Manifest.UniqueName}::{nameof(SmartShieldCrystalKnowledgeHandler)}", new SmartShieldCrystalKnowledgeHandler());
		RegisterHandler($"{package.Manifest.UniqueName}::{nameof(AttackCrystalKnowledgeHandler)}", new AttackCrystalKnowledgeHandler());
		RegisterHandler($"{package.Manifest.UniqueName}::{nameof(StunChargeCrystalKnowledgeHandler)}", new StunChargeCrystalKnowledgeHandler());
		RegisterHandler($"{package.Manifest.UniqueName}::{nameof(RelativityCrystalKnowledgeHandler)}", new RelativityCrystalKnowledgeHandler());
		RegisterHandler($"{package.Manifest.UniqueName}::{nameof(MaxShieldCrystalKnowledgeHandler)}", new MaxShieldCrystalKnowledgeHandler());
		RegisterHandler($"{package.Manifest.UniqueName}::{nameof(DrawCrystalKnowledgeHandler)}", new DrawCrystalKnowledgeHandler());
		RegisterHandler($"{package.Manifest.UniqueName}::{nameof(OverdriveCrystalKnowledgeHandler)}", new OverdriveCrystalKnowledgeHandler());
		RegisterHandler($"{package.Manifest.UniqueName}::{nameof(PowerdriveCrystalKnowledgeHandler)}", new PowerdriveCrystalKnowledgeHandler());
		RegisterHandler($"{package.Manifest.UniqueName}::{nameof(EnergyCrystalKnowledgeHandler)}", new EnergyCrystalKnowledgeHandler());
		RegisterHandler($"{package.Manifest.UniqueName}::{nameof(ShardCrystalKnowledgeHandler)}", new ShardCrystalKnowledgeHandler());
		RegisterHandler($"{package.Manifest.UniqueName}::{nameof(MaxShardCrystalKnowledgeHandler)}", new MaxShardCrystalKnowledgeHandler());
	}
	
	public static ICrystalKnowledgeHandler? LookupHandler(string uniqueName)
		=> Handlers.GetValueOrDefault(uniqueName);

	public static void RegisterHandler(string uniqueName, ICrystalKnowledgeHandler handler)
		=> Handlers[uniqueName] = handler;

	public static void UnregisterHandler(string uniqueName)
		=> Handlers.Remove(uniqueName);

	public static void SetIsShardCharacter(Deck deck, bool isShardCharacter = true)
	{
		if (isShardCharacter)
			ShardCharacters.Add(deck);
		else
			ShardCharacters.Remove(deck);
	}
}

internal interface ICrystalKnowledgeHandler
{
	bool IsEnabled(State state) => true;
	CardAction? MakeAction(State state, Combat combat, Card card, int tier, bool isUpgraded);
}

internal sealed class SmartShieldCrystalKnowledgeHandler : ICrystalKnowledgeHandler
{
	public CardAction MakeAction(State state, Combat combat, Card card, int tier, bool isUpgraded)
		=> new SmartShieldAction { TargetPlayer = true, Amount = isUpgraded ? 2 : 1 };
}

internal sealed class AttackCrystalKnowledgeHandler : ICrystalKnowledgeHandler
{
	public CardAction MakeAction(State state, Combat combat, Card card, int tier, bool isUpgraded)
		=> new AAttack { damage = card.GetDmg(state, isUpgraded ? 2 : 1) };
}

internal sealed class StunChargeCrystalKnowledgeHandler : ICrystalKnowledgeHandler
{
	public CardAction MakeAction(State state, Combat combat, Card card, int tier, bool isUpgraded)
		=> new AStatus { targetPlayer = true, status = Status.stunCharge, statusAmount = isUpgraded ? 2 : 1 };
}

internal sealed class RelativityCrystalKnowledgeHandler : ICrystalKnowledgeHandler
{
	public CardAction? MakeAction(State state, Combat combat, Card card, int tier, bool isUpgraded)
		=> tier < 2 ? null : new AStatus { targetPlayer = true, status = RelativityManager.RelativityStatus.Status, statusAmount = isUpgraded ? 2 : 1 };
}

internal sealed class MaxShieldCrystalKnowledgeHandler : ICrystalKnowledgeHandler
{
	public CardAction? MakeAction(State state, Combat combat, Card card, int tier, bool isUpgraded)
		=> tier < 2 ? null : new AStatus { targetPlayer = true, status = Status.maxShield, statusAmount = isUpgraded ? 2 : 1 };
}

internal sealed class DrawCrystalKnowledgeHandler : ICrystalKnowledgeHandler
{
	public bool IsEnabled(State state)
		=> state.characters.Any(c => c.deckType is { } deck && CrystalKnowledgeManager.ShardCharacters.Contains(deck));
	
	public CardAction? MakeAction(State state, Combat combat, Card card, int tier, bool isUpgraded)
		=> tier < 2 ? null : new ADrawCard { count = isUpgraded ? 2 : 1 };
}

internal sealed class OverdriveCrystalKnowledgeHandler : ICrystalKnowledgeHandler
{
	public bool IsEnabled(State state)
		=> state.characters.Any(c => c.deckType is { } deck && CrystalKnowledgeManager.ShardCharacters.Contains(deck));
	
	public CardAction? MakeAction(State state, Combat combat, Card card, int tier, bool isUpgraded)
		=> tier < 2 ? null : new AStatus { targetPlayer = true, status = Status.overdrive, statusAmount = isUpgraded ? 2 : 1 };
}

internal sealed class PowerdriveCrystalKnowledgeHandler : ICrystalKnowledgeHandler
{
	public bool IsEnabled(State state)
		=> state.characters.Any(c => c.deckType is { } deck && CrystalKnowledgeManager.ShardCharacters.Contains(deck));
	
	public CardAction? MakeAction(State state, Combat combat, Card card, int tier, bool isUpgraded)
		=> tier < 4 || !isUpgraded ? null : new AStatus { targetPlayer = true, status = Status.powerdrive, statusAmount = 1 };
}

internal sealed class EnergyCrystalKnowledgeHandler : ICrystalKnowledgeHandler
{
	public CardAction? MakeAction(State state, Combat combat, Card card, int tier, bool isUpgraded)
		=> tier < 4 ? null : new AEnergy { changeAmount = isUpgraded ? 2 : 1 };
}

internal sealed class ShardCrystalKnowledgeHandler : ICrystalKnowledgeHandler
{
	public bool IsEnabled(State state)
		=> state.characters.Any(c => c.deckType is { } deck && CrystalKnowledgeManager.ShardCharacters.Contains(deck));
	
	public CardAction MakeAction(State state, Combat combat, Card card, int tier, bool isUpgraded)
		=> new AStatus { targetPlayer = true, status = Status.shard, statusAmount = isUpgraded ? 2 : 1 };
}

internal sealed class MaxShardCrystalKnowledgeHandler : ICrystalKnowledgeHandler
{
	public bool IsEnabled(State state)
		=> state.characters.Any(c => c.deckType is { } deck && CrystalKnowledgeManager.ShardCharacters.Contains(deck));
	
	public CardAction? MakeAction(State state, Combat combat, Card card, int tier, bool isUpgraded)
		=> tier < 2 ? null : new AStatus { targetPlayer = true, status = Status.maxShard, statusAmount = isUpgraded ? 2 : 1 };
}