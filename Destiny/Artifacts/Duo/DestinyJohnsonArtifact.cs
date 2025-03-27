using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Johnson;
using Shockah.Kokoro;
using Shockah.Shared;

namespace Shockah.Destiny;

internal sealed class DestinyJohnsonArtifact : Artifact, IRegisterable, IKokoroApi.IV2.ITemporaryUpgradesApi.IHook
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		if (ModEntry.Instance.Helper.ModRegistry.GetApi<IJohnsonApi>("Shockah.Johnson") is not { } johnsonApi)
			return;
		
		helper.Content.Artifacts.RegisterArtifact("DestinyJohnson", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/Johnson.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Johnson", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Johnson", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DestinyDeck.Deck, johnsonApi.JohnsonDeck.Deck]);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			ModEntry.Instance.KokoroApi.TemporaryUpgrades.UpgradeTooltip,
			.. Enchanted.EnchantedTrait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? [],
			.. StatusMeta.GetTooltips(Status.shard, (MG.inst.g?.state ?? DB.fakeState).ship.GetMaxShard()),
		];

	public void OnTemporaryUpgrade(IKokoroApi.IV2.ITemporaryUpgradesApi.IHook.IOnTemporaryUpgradeArgs args)
	{
		if (args.State.route is not Combat combat)
			return;
		
		var enchantLevel = Enchanted.GetEnchantLevel(args.Card);
		var maxEnchantLevel = Enchanted.GetMaxEnchantLevel(args.Card.Key(), args.Card.upgrade);
		if (enchantLevel >= maxEnchantLevel)
			return;
		if (Enchanted.GetEnchantLevelCost(args.Card.Key(), args.Card.upgrade, enchantLevel + 1) is not { } cost)
			return;
		if (cost.GetPossibleTransactions(Array.Empty<IKokoroApi.IV2.IActionCostsApi.ICost>(), ModEntry.Instance.KokoroApi.ActionCosts.MakeTransaction()).FirstOrDefault() is not { } transaction)
			return;

		var actions = transaction.Resources
			.Select(kvp => kvp.Key.GetChangeAction(args.State, combat, kvp.Value))
			.WhereNotNull()
			.ToList();

		if (actions.Count != 0)
			actions[0].artifactPulse = Key();
		combat.QueueImmediate(actions);
	}
}