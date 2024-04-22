using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class MasochismArtifact : Artifact, IRegisterable
{
	[JsonProperty]
	public int Stacks { get; set; } = 0;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("Masochism", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DraculaDeck.Deck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Masochism.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Masochism", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Masochism", "description"]).Localize
		});

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.DirectHullDamage)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_DirectHullDamage_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_DirectHullDamage_Postfix))
		);
	}

	public override int? GetDisplayNumber(State s)
		=> Stacks;

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			new TTCard
			{
				card = new BloodTapCard
				{
					discount = -1,
					exhaustOverride = true,
					temporaryOverride = true
				}
			}
		];

	private static void Ship_DirectHullDamage_Prefix(Ship __instance, ref int __state)
		=> __state = __instance.hull;

	private static void Ship_DirectHullDamage_Postfix(Ship __instance, State s, Combat c, ref int __state)
	{
		var damageTaken = __state - __instance.hull;
		if (damageTaken <= 0)
			return;
		if (!__instance.isPlayerShip)
			return;
		if (!c.isPlayerTurn)
			return;

		var artifact = s.EnumerateAllArtifacts().OfType<MasochismArtifact>().FirstOrDefault();
		if (artifact is null)
			return;

		artifact.Stacks += damageTaken;
		artifact.Pulse();

		while (artifact.Stacks >= 5)
		{
			artifact.Stacks -= 5;
			c.QueueImmediate(new AAddCard
			{
				card = new BloodTapCard
				{
					discount = -1,
					exhaustOverride = true,
					temporaryOverride = true
				},
				destination = CardDestination.Hand
			});
		}
	}
}