using FSPRO;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class BatmobileArtifact : Artifact, IRegisterable
{
	private static ISpriteEntry Sprite100 = null!;
	private static ISpriteEntry Sprite75 = null!;
	private static ISpriteEntry Sprite50 = null!;
	private static ISpriteEntry Sprite25 = null!;
	private static ISpriteEntry Sprite1 = null!;

	[JsonProperty]
	private bool WasBelow75;

	[JsonProperty]
	private bool WasBelow50;

	[JsonProperty]
	private bool WasBelow25;

	[JsonProperty]
	private bool InCombat;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Sprite100 = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Ship/Batmobile100.png"));
		Sprite75 = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Ship/Batmobile75.png"));
		Sprite50 = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Ship/Batmobile50.png"));
		Sprite25 = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Ship/Batmobile25.png"));
		Sprite1 = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Ship/Batmobile1.png"));

		helper.Content.Artifacts.RegisterArtifact("Batmobile", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				unremovable = true,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Ship/Batmobile.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ship", "Batmobile", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ship", "Batmobile", "description"]).Localize
		});

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Artifact), nameof(GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Artifact_GetTooltips_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Update)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_Update_Postfix))
		);
	}

	public override Spr GetSprite()
	{
		if (!InCombat || MG.inst.g?.state is not { } state)
			return base.GetSprite();
		if (state.ship.hull == 1)
			return Sprite1.Sprite;
		return ((1.0 * state.ship.hull / state.ship.hullMax) switch
		{
			<= 0.25 => Sprite25,
			<= 0.5 => Sprite50,
			<= 0.75 => Sprite75,
			_ => Sprite100
		}).Sprite;
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. StatusMeta.GetTooltips(Status.maxShield, 1),
			.. StatusMeta.GetTooltips(Status.evade, 1),
			.. StatusMeta.GetTooltips(Status.shield, 1),
			new TTGlossary("parttrait.weak"),
		];

	public override void OnCombatStart(State state, Combat combat)
		=> this.InCombat = true;

	public override void OnCombatEnd(State state)
	{
		this.InCombat = false;
		if (MG.inst.g is { } g)
			this.UnapplyWingArmor(g);
	}

	public override int ModifyBaseDamage(int baseDamage, Card? card, State state, Combat? combat, bool fromPlayer)
		=> fromPlayer && state.ship.hull == 1 ? 1 : 0;

	private static void Artifact_GetTooltips_Postfix(Artifact __instance, ref List<Tooltip> __result)
	{
		if (__instance is not BatmobileArtifact)
			return;

		var textTooltip = __result.OfType<TTText>().FirstOrDefault(t => t.text.StartsWith("<c=artifact>"));
		if (textTooltip is null)
			return;

		if (MG.inst.g?.state is not { } state || state.IsOutsideRun())
			return;
		textTooltip.text = DB.Join(
			"<c=artifact>{0}</c>\n".FF(__instance.GetLocName()),
			ModEntry.Instance.Localizations.Localize(["artifact", "ship", "Batmobile", "combatDescription"], new
			{
				Hull75 = (int)(state.ship.hullMax * 0.75),
				Hull50 = (int)(state.ship.hullMax * 0.5),
				Hull25 = (int)(state.ship.hullMax * 0.25),
			})
		);
	}

	private static void Combat_Update_Postfix(Combat __instance, G g)
	{
		var artifact = g.state.EnumerateAllArtifacts().OfType<BatmobileArtifact>().FirstOrDefault();
		if (artifact is null)
			return;

		var newState = 1.0 * g.state.ship.hull / g.state.ship.hullMax;

		var isBelow75 = artifact.InCombat && newState <= 0.75;
		var isBelow50 = artifact.InCombat && newState <= 0.5;
		var isBelow25 = artifact.InCombat && newState <= 0.25;

		if (isBelow75 != artifact.WasBelow75)
		{
			__instance.QueueImmediate(new AStatus
			{
				targetPlayer = true,
				status = Status.maxShield,
				statusAmount = isBelow75 ? 1 : -1,
				canRunAfterKill = true,
				artifactPulse = artifact.Key()
			});
			artifact.WasBelow75 = isBelow75;
		}

		if (isBelow50 != artifact.WasBelow50)
		{
			if (isBelow50)
			{
				__instance.QueueImmediate(new AStatus
				{
					targetPlayer = true,
					status = Status.shield,
					statusAmount = 1,
					artifactPulse = artifact.Key()
				});
				__instance.QueueImmediate(new AStatus
				{
					targetPlayer = true,
					status = Status.evade,
					statusAmount = 1,
					artifactPulse = artifact.Key()
				});
			}
			artifact.WasBelow50 = isBelow50;
		}

		if (isBelow25 != artifact.WasBelow25)
		{
			for (var i = g.state.ship.parts.Count - 1; i >= 0; i--)
			{
				var worldX = g.state.ship.x + i;
				if (g.state.ship.parts[i].type != PType.wing)
					continue;
				__instance.QueueImmediate(new ABatmobileArmor
				{
					TargetPlayer = true,
					Weaken = !isBelow25,
					WorldX = worldX,
					canRunAfterKill = true,
					artifactPulse = artifact.Key()
				});
			}
			artifact.WasBelow25 = isBelow25;
		}
	}

	private void UnapplyWingArmor(G g)
	{
		for (var i = g.state.ship.parts.Count - 1; i >= 0; i--)
		{
			var worldX = g.state.ship.x + i;
			if (g.state.ship.parts[i].type != PType.wing)
				continue;
			new ABatmobileArmor
			{
				TargetPlayer = true,
				Weaken = true,
				WorldX = worldX,
				canRunAfterKill = true,
				artifactPulse = Key()
			}.Begin(g, g.state, g.state.route as Combat ?? DB.fakeCombat);
		}
		WasBelow25 = false;
	}

	public sealed class ABatmobileArmor : CardAction
	{
		public int WorldX { get; init; }
		public bool TargetPlayer { get; init; }
		public bool Weaken { get; init; }

		public override void Begin(G g, State s, Combat c)
		{
			var newDamageModifier = Weaken ? PDamMod.weak : PDamMod.none;
			var partAtWorldX = (TargetPlayer ? s.ship : c.otherShip).GetPartAtWorldX(WorldX);
			if (partAtWorldX is null || partAtWorldX.damageModifier == newDamageModifier)
			{
				timer = 0;
				return;
			}

			var isGood = partAtWorldX.damageModifier == PDamMod.weak;
			partAtWorldX.damageModifier = newDamageModifier;
			if (s.ship.key == ModEntry.Instance.Ship.UniqueName)
				partAtWorldX.skin = Weaken ? ModEntry.Instance.ShipWing.UniqueName : ModEntry.Instance.ShipArmoredWing.UniqueName;
			Audio.Play(isGood ? Event.Status_PowerUp : Event.Status_PowerDown);
		}
	}
}