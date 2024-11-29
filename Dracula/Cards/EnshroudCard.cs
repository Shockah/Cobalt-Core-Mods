using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal static class EnshroudExt
{
	public static PDamMod? GetDamageModifierBeforeEnshroud(this Part self)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<PDamMod>(self, "DamageModifierBeforeEnshroud");

	public static void SetDamageModifierBeforeEnshroud(this Part self, PDamMod? value)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "DamageModifierBeforeEnshroud", value);

	public static PDamMod? GetDamageModifierOverrideWhileActiveBeforeEnshroud(this Part self)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<PDamMod>(self, "DamageModifierOverrideWhileActiveBeforeEnshroud");

	public static void SetDamageModifierOverrideWhileActiveBeforeEnshroud(this Part self, PDamMod? value)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "DamageModifierOverrideWhileActiveBeforeEnshroud", value);
}

internal sealed class EnshroudCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Enshroud", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.rare,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_CloudSave,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Enshroud", "name"]).Localize
		});

		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnTurnStart), (State state, Combat combat) =>
		{
			if (!combat.isPlayerTurn)
				return;

			List<Ship> ships = [state.ship, combat.otherShip];
			foreach (var ship in ships)
			{
				foreach (var part in ship.parts)
				{
					if (part.damageModifier == PDamMod.armor && part.GetDamageModifierBeforeEnshroud() is { } damageModifierBeforeEnshroud)
					{
						part.damageModifier = damageModifierBeforeEnshroud;
						part.SetDamageModifierBeforeEnshroud(null);
					}
					if (part.damageModifierOverrideWhileActive == PDamMod.armor && part.GetDamageModifierOverrideWhileActiveBeforeEnshroud() is { } damageModifierOverrideWhileActiveBeforeEnshroud)
					{
						part.damageModifierOverrideWhileActive = damageModifierOverrideWhileActiveBeforeEnshroud;
						part.SetDamageModifierOverrideWhileActiveBeforeEnshroud(null);
					}
				}
			}
		});

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(HARDMODE), nameof(HARDMODE.OnTurnStart)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(HARDMODE_OnTurnStart_Postfix))
		);
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade switch
			{
				Upgrade.A => 1,
				Upgrade.B => 3,
				_ => 2
			},
			singleUse = upgrade == Upgrade.B,
			description = ModEntry.Instance.Localizations.Localize(["card", "Enshroud", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new UpgradeArmor(),
				new AEndTurn(),
			],
			_ => [
				new EnshroudAction { TargetPlayer = true },
			]
		};

	private static void HARDMODE_OnTurnStart_Prefix(HARDMODE __instance, Combat combat, ref int __state)
		=> __state = combat.cardActions.Count;

	private static void HARDMODE_OnTurnStart_Postfix(HARDMODE __instance, Combat combat, ref int __state)
	{
		if (__instance.difficulty < 1 || combat.turn != 1)
			return;

		for (var i = combat.cardActions.Count - __state - 1; i >= 0; i--)
			combat.cardActions[i].timer = 0;
		combat.cardActions.Insert(combat.cardActions.Count - __state, new UpgradeArmor { Reapply = true });
	}

	private sealed class EnshroudAction : CardAction
	{
		public required bool TargetPlayer;

		public override List<Tooltip> GetTooltips(State s)
			=> [new TTGlossary("parttrait.armor")];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			var ship = TargetPlayer ? s.ship : c.otherShip;
			c.QueueImmediate(
				ship.parts
					.Select((part, i) => (Part: part, X: i))
					.Where(e => e.Part.type != PType.empty)
					.Select(e => new EnshroudPartAction { TargetPlayer = TargetPlayer, LocalX = e.X })
			);
		}
	}

	private sealed class EnshroudPartAction : CardAction
	{
		public required bool TargetPlayer;
		public required int LocalX;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			var ship = TargetPlayer ? s.ship : c.otherShip;
			if (ship.GetPartAtLocalX(LocalX) is not { } part)
				return;

			if (part.damageModifier != PDamMod.armor)
			{
				part.SetDamageModifierBeforeEnshroud(part.damageModifier);
				c.QueueImmediate(new AArmor
				{
					targetPlayer = TargetPlayer,
					worldX = LocalX + ship.x
				});
			}
			if (part.damageModifierOverrideWhileActive is not null && part.damageModifierOverrideWhileActive != PDamMod.armor)
			{
				part.SetDamageModifierOverrideWhileActiveBeforeEnshroud(part.damageModifierOverrideWhileActive);
				c.QueueImmediate(new AArmor
				{
					targetPlayer = TargetPlayer,
					worldX = LocalX + ship.x,
					justTheActiveOverride = true
				});
			}
		}
	}

	private sealed class UpgradeArmor : CardAction
	{
		public bool Reapply;

		public override List<Tooltip> GetTooltips(State s)
			=> [
				new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::{typeof(EnshroudCard).Name}::{GetType().Name}")
				{
					Icon = StableSpr.icons_armor,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["card", "Enshroud", "UpgradeCockpit", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["card", "Enshroud", "UpgradeCockpit", "description"])
				},
				new TTGlossary("parttrait.brittle"),
				new TTGlossary("parttrait.weak"),
				new TTGlossary("parttrait.armor"),
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			bool zeroTimer;
			if (Reapply)
				zeroTimer = ApplyUpgrade(s.ship);
			else
				zeroTimer = IncreaseUpgrade(s.ship);

			if (zeroTimer)
				timer = 0;
		}

		private static bool IncreaseUpgrade(Ship ship)
		{
			var level = ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(ship, "CockpitArmorUpgradeLevel") + 1;
			ModEntry.Instance.Helper.ModData.SetModData(ship, "CockpitArmorUpgradeLevel", level);
			ApplyUpgrade(ship, 1);
			return true;
		}

		private static bool ApplyUpgrade(Ship ship)
			=> ApplyUpgrade(ship, ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(ship, "CockpitArmorUpgradeLevel"));

		private static bool ApplyUpgrade(Ship ship, int level)
		{
			if (level <= 0)
				return false;

			foreach (var part in ship.parts)
			{
				if (part.type != PType.cockpit)
					continue;

				for (var i = 0; i < level; i++)
					part.damageModifier = ModifyDamageModifier(part.damageModifier);

				static PDamMod ModifyDamageModifier(PDamMod mod)
					=> mod switch
					{
						PDamMod.brittle => PDamMod.weak,
						PDamMod.weak => PDamMod.none,
						PDamMod.none => PDamMod.armor,
						_ => mod
					};
			}

			return true;
		}
	}
}
