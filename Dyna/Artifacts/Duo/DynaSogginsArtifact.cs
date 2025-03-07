using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class DynaSogginsArtifact : Artifact, IRegisterable, ISmugHook
{
	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;

	[JsonProperty]
	private bool TriggeredThisTurn;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		if (ModEntry.Instance.SogginsApi is not { } sogginsApi)
			return;

		ActiveSprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DynaSoggins.png"));
		InactiveSprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DynaSogginsInactive.png"));

		helper.Content.Artifacts.RegisterArtifact("DynaSoggins", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = ActiveSprite.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DynaSoggins", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DynaSoggins", "description"]).Localize,
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DynaDeck.Deck, sogginsApi.SogginsVanillaDeck]);
	}

	public override Spr GetSprite()
		=> (TriggeredThisTurn ? InactiveSprite : ActiveSprite).Sprite;

	public override List<Tooltip> GetExtraTooltips()
		=> [
			ModEntry.Instance.SogginsApi!.GetSmugTooltip(),
			.. new ConcussionCharge().GetTooltips(MG.inst.g?.state ?? DB.fakeState),
			.. new ShatterCharge().GetTooltips(MG.inst.g?.state ?? DB.fakeState),
		];

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (combat.isPlayerTurn)
			TriggeredThisTurn = false;
	}

	public void OnCardBotchedBySmug(State state, Combat combat, Card card)
	{
		if (TriggeredThisTurn)
			return;

		TriggeredThisTurn = true;
		combat.Queue(new FireChargeAction
		{
			Charge = state.rngActions.NextInt() % 2 == 0 ? new ConcussionCharge() : new ShatterCharge(),
			artifactPulse = Key()
		});
	}
}