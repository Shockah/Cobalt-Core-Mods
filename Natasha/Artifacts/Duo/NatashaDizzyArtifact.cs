using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.Natasha;

internal sealed class NatashaDizzyArtifact : Artifact, IRegisterable
{
	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;
	
	[JsonProperty]
	private bool MitosisTransferred;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		
		ActiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/Dizzy.png"));
		InactiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DizzyInactive.png"));
		
		helper.Content.Artifacts.RegisterArtifact("NatashaDizzy", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = ActiveSprite.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Dizzy", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Dizzy", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.NatashaDeck.Deck, Deck.dizzy]);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Update)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_Update_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_Update_Postfix))
		);
	}

	public override Spr GetSprite()
		=> MitosisTransferred ? InactiveSprite.Sprite : ActiveSprite.Sprite;

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. StatusMeta.GetTooltips(Status.mitosis, 2),
			new TTGlossary("status.shieldAlt"),
		];

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);

		if (combat.otherShip.Get(Status.shield) <= 0)
		{
			MitosisTransferred = true;
			combat.QueueImmediate(new AStatus { targetPlayer = true, status = Status.mitosis, statusAmount = 1, artifactPulse = Key() });
		}
		else
		{
			MitosisTransferred = false;
			combat.QueueImmediate(new AStatus { targetPlayer = false, status = Status.mitosis, statusAmount = 2, artifactPulse = Key() });
		}
	}

	private static void Combat_Update_Prefix(Combat __instance, out int __state)
		=> __state = __instance.otherShip.Get(Status.shield);

	private static void Combat_Update_Postfix(Combat __instance, G g, in int __state)
	{
		if (__state <= 0)
			return;
		if (__instance.otherShip.Get(Status.shield) > 0)
			return;
		if (g.state.EnumerateAllArtifacts().OfType<NatashaDizzyArtifact>().FirstOrDefault() is not { } artifact)
			return;
		if (artifact.MitosisTransferred)
			return;
		if (__instance.cardActions.Any(a => a is TransferMitosisAction))
			return;
		
		__instance.QueueImmediate(new TransferMitosisAction());
	}

	private sealed class TransferMitosisAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			
			if (g.state.EnumerateAllArtifacts().OfType<NatashaDizzyArtifact>().FirstOrDefault() is not { } artifact)
				return;
			if (artifact.MitosisTransferred)
				return;

			artifact.MitosisTransferred = true;
			c.QueueImmediate([
				new AStatus { targetPlayer = false, status = Status.mitosis, statusAmount = -2, artifactPulse = Key(), timer = 0 },
				new AStatus { targetPlayer = true, status = Status.mitosis, statusAmount = 1 },
			]);
		}
	}
}