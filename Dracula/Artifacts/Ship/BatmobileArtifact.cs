using FSPRO;
using HarmonyLib;
using Newtonsoft.Json;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class BatmobileArtifact : Artifact, IDraculaArtifact
{
	[JsonProperty]
	private bool WasBelow75 = false;

	[JsonProperty]
	private bool WasBelow50 = false;

	[JsonProperty]
	private bool WasBelow25 = false;

	[JsonProperty]
	private bool InCombat = false;

	public static void Register(IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("Batmobile", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				unremovable = true,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Ship/Batmobile.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ship", "Batmobile", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ship", "Batmobile", "description"]).Localize
		});

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Artifact), nameof(GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Artifact_GetTooltips_Postfix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Update)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_Update_Postfix))
		);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> StatusMeta.GetTooltips(Status.maxShield, 1)
			.Concat(StatusMeta.GetTooltips(Status.evade, 1))
			.Concat(StatusMeta.GetTooltips(Status.shield, 1))
			.Append(new TTGlossary("parttrait.weak"))
			.ToList();

	public override void OnCombatStart(State state, Combat combat)
		=> this.InCombat = true;

	public override void OnCombatEnd(State state)
		=> this.InCombat = false;

	public override int ModifyBaseDamage(int baseDamage, Card? card, State state, Combat? combat, bool fromPlayer)
		=> fromPlayer && state.ship.hull == 1 ? 1 : 0;

	private static void Artifact_GetTooltips_Postfix(Artifact __instance, ref List<Tooltip> __result)
	{
		if (__instance is not BatmobileArtifact)
			return;

		var textTooltip = __result.OfType<TTText>().FirstOrDefault(t => t.text.StartsWith("<c=artifact>"));
		if (textTooltip is null)
			return;

		if (MG.inst.g?.state is not { } state || state.route is not Combat)
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

				if (isBelow25)
					__instance.QueueImmediate(new ARemovePartArmorMod
					{
						TargetPlayer = true,
						WorldX = worldX,
						canRunAfterKill = true,
						artifactPulse = artifact.Key()
					});
				else
					__instance.QueueImmediate(new AWeaken
					{
						targetPlayer = true,
						worldX = worldX,
						canRunAfterKill = true,
						artifactPulse = artifact.Key()
					});
			}
			artifact.WasBelow25 = isBelow25;
		}
	}

	public sealed class ARemovePartArmorMod : CardAction
	{
		public int WorldX { get; init; }

		public bool TargetPlayer { get; init; }

		public bool JustTheActiveOverride { get; init; }

		public override void Begin(G g, State s, Combat c)
		{
			var partAtWorldX = (TargetPlayer ? s.ship : c.otherShip).GetPartAtWorldX(WorldX);
			if (partAtWorldX is null)
			{
				timer = 0;
				return;
			}

			timer *= 0.5;
			bool isGood;
			if (JustTheActiveOverride)
			{
				if (partAtWorldX.damageModifierOverrideWhileActive == PDamMod.none)
				{
					timer = 0;
					return;
				}
				isGood = partAtWorldX.damageModifierOverrideWhileActive != PDamMod.armor;
				partAtWorldX.damageModifierOverrideWhileActive = PDamMod.none;
			}
			else
			{
				if (partAtWorldX.damageModifier == PDamMod.none)
				{
					timer = 0;
					return;
				}
				isGood = partAtWorldX.damageModifier != PDamMod.armor;
				partAtWorldX.damageModifier = PDamMod.none;
			}
			Audio.Play(isGood ? Event.Status_PowerUp : Event.Status_PowerDown);
		}
	}
}