using System.Reflection;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class AgniWaterCoolingArtifact : Artifact, IRegisterable
{
	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;
	
	[JsonProperty]
	internal bool TriggeredThisTurn;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ActiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Agni/Artifact/WaterCooling.png"));
		InactiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Agni/Artifact/WaterCoolingInactive.png"));
		
		helper.Content.Artifacts.RegisterArtifact("AgniWaterCooling", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.Common],
				unremovable = true,
			},
			Sprite = ActiveSprite.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Agni", "artifact", "WaterCooling", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Agni", "artifact", "WaterCooling", "description"]).Localize
		});
	}

	public override Spr GetSprite()
		=> TriggeredThisTurn ? InactiveSprite.Sprite : ActiveSprite.Sprite;

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		TriggeredThisTurn = false;
	}
}