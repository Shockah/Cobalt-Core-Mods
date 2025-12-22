using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class PurgatoryArtifact : Artifact, IRegisterable
{
	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;

	[JsonProperty]
	public bool TriggeredThisCombat { get; set; }

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ActiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Dracula/Purgatory.png"));
		InactiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Dracula/PurgatoryInactive.png"));

		helper.Content.Artifacts.RegisterArtifact("Purgatory", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DraculaDeck.Deck,
				pools = [ArtifactPool.Boss]
			},
			Sprite = ActiveSprite.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Dracula", "Purgatory", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Dracula", "Purgatory", "description"]).Localize
		});

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.DirectHullDamage)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_DirectHullDamage_Prefix))
		);
	}

	public override Spr GetSprite()
		=> (TriggeredThisCombat ? InactiveSprite : ActiveSprite).Sprite;

	public override List<Tooltip> GetExtraTooltips()
		=> StatusMeta.GetTooltips(Status.perfectShield, 1);

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		TriggeredThisCombat = false;
	}

	private static bool Ship_DirectHullDamage_Prefix(Ship __instance, State s, Combat c, int amt)
	{
		if (!__instance.isPlayerShip)
			return true;
		if (amt < __instance.hull)
			return true;
		if (__instance.Get(Status.perfectShield) > 0)
			return true;

		var artifact = s.EnumerateAllArtifacts().OfType<PurgatoryArtifact>().FirstOrDefault();
		if (artifact is null || artifact.TriggeredThisCombat)
			return true;

		c.QueueImmediate([
			new AStatus
			{
				targetPlayer = true,
				mode = AStatusMode.Set,
				status = Status.perfectShield,
				statusAmount = 1,
				artifactPulse = artifact.Key()
			},
			new ADisablePurgatory()
		]);
		return false;
	}

	public sealed class ADisablePurgatory : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			var artifact = s.EnumerateAllArtifacts().OfType<PurgatoryArtifact>().FirstOrDefault();
			if (artifact is not null)
				artifact.TriggeredThisCombat = true;
		}
	}
}