using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class JohnsonTyArtifact : Artifact, IRegisterable
{
	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;

	[JsonProperty]
	public bool TriggeredThisTurn { get; set; }

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		if (ModEntry.Instance.TyAndSashaApi is not { } tyApi)
			return;

		ActiveSprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/JohnsonTy.png"));
		InactiveSprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/JohnsonTyInactive.png"));

		helper.Content.Artifacts.RegisterArtifact("JohnsonTy", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = ActiveSprite.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "JohnsonTy", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "JohnsonTy", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.JohnsonDeck.Deck, tyApi.TyDeck]);
	}

	public override Spr GetSprite()
		=> (TriggeredThisTurn ? InactiveSprite : ActiveSprite).Sprite;

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			.. ModEntry.Instance.TyAndSashaApi!.WildTrait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? [],
			ModEntry.Instance.KokoroApi.TemporaryUpgrades.UpgradeTooltip
		];

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (combat.isPlayerTurn)
			TriggeredThisTurn = false;
	}

	public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
	{
		base.OnPlayerPlayCard(energyCost, deck, card, state, combat, handPosition, handCount);
		if (TriggeredThisTurn)
			return;
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, ModEntry.Instance.TyAndSashaApi!.WildTrait))
			return;

		TriggeredThisTurn = true;
		if (card.upgrade != Upgrade.None)
			return;

		var action = ModEntry.Instance.KokoroApi.TemporaryUpgrades.MakeChooseTemporaryUpgradeAction(card.uuid).AsCardAction;
		action.artifactPulse = Key();
		combat.Queue(action);
	}
}