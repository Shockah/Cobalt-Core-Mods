using System.Reflection;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class NemesisReactiveShieldArtifact : Artifact, IRegisterable
{
	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;
	
	[JsonProperty]
	private bool TriggeredThisTurn;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ActiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Nemesis/Artifact/ReactiveShield.png"));
		InactiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Nemesis/Artifact/ReactiveShieldInactive.png"));
		
		helper.Content.Artifacts.RegisterArtifact("NemesisReactiveShield", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.Common],
			},
			Sprite = ActiveSprite.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Nemesis", "artifact", "ReactiveShield", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Nemesis", "artifact", "ReactiveShield", "description"]).Localize
		});
	}

	public override Spr GetSprite()
		=> TriggeredThisTurn ? InactiveSprite.Sprite : ActiveSprite.Sprite;

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		TriggeredThisTurn = false;
	}

	public override void OnPlayerTakeNormalDamage(State state, Combat combat, int rawAmount, Part? part)
	{
		base.OnPlayerTakeNormalDamage(state, combat, rawAmount, part);
		if (TriggeredThisTurn)
			return;
		if (part is null)
			return;

		TriggeredThisTurn = true;
		combat.QueueImmediate(new AStatus { targetPlayer = true, status = Status.shield, statusAmount = 1, artifactPulse = Key() });
	}
}