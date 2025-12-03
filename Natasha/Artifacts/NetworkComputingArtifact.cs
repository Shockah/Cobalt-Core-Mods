using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class NetworkComputingArtifact : Artifact, IRegisterable, IKokoroApi.IV2.ILimitedApi.IHook
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("NetworkComputing", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.NatashaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/NetworkComputing.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "NetworkComputing", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "NetworkComputing", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [.. (ModEntry.Instance.KokoroApi.Limited.Trait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? [])];

	public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
	{
		base.OnPlayerPlayCard(energyCost, deck, card, state, combat, handPosition, handCount);

		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, ModEntry.Instance.KokoroApi.Limited.Trait))
			return;

		if (ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(card, "PlayedViaNetworkComputing"))
		{
			ModEntry.Instance.Helper.ModData.RemoveModData(card, "PlayedViaNetworkComputing");
			return;
		}

		combat.Queue(new Action { CardId = card.uuid, OriginalArtifactKey = Key() });
	}

	public override void OnQueueEmptyDuringPlayerTurn(State state, Combat combat)
	{
		base.OnQueueEmptyDuringPlayerTurn(state, combat);

		foreach (var card in combat.hand)
			ModEntry.Instance.Helper.ModData.RemoveModData(card, "PlayedViaNetworkComputing");
	}

	public bool ModifyLimitedUses(IKokoroApi.IV2.ILimitedApi.IHook.IModifyLimitedUsesArgs args)
	{
		args.Uses--;
		return false;
	}

	private sealed class Action : CardAction
	{
		public required int CardId;
		public required string OriginalArtifactKey;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			if (s.FindCard(CardId) is not { } card || c.exhausted.Contains(card))
				return;

			ModEntry.Instance.Helper.ModData.SetModData(card, "PlayedViaNetworkComputing", true);
			var action = ModEntry.Instance.KokoroApi.PlayCardsFromAnywhere.MakeAction(CardId).AsCardAction;
			action.artifactPulse = OriginalArtifactKey;
			c.QueueImmediate(action);
		}
	}
}