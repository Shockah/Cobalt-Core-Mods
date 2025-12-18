using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal static class EcholocationExt
{
	public static int? GetEcholocationReturnPosition(this Combat self)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<int>(self, "EcholocationReturnPosition");

	public static void SetEcholocationReturnPosition(this Combat self, int? value)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "EcholocationReturnPosition", value);
}

internal sealed class EcholocationCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Echolocation", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true,
				unreleased = true,
			},
			Art = StableSpr.cards_ScootRight,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Dracula", "Echolocation", "name"]).Localize
		});

		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnTurnEnd), (State state, Combat combat) =>
		{
			if (combat.GetEcholocationReturnPosition() is not { } echolocationReturnPosition)
				return;
			combat.SetEcholocationReturnPosition(null);
			combat.QueueImmediate(new AMove
			{
				targetPlayer = true,
				dir = echolocationReturnPosition - state.ship.x,
				ignoreFlipped = true,
				ignoreHermes = true,
				isTeleport = true
			});
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.A ? 0 : 1,
			retain = upgrade == Upgrade.B,
			description = ModEntry.Instance.Localizations.Localize(["card", "Dracula", "Echolocation", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		var maxCheck = Math.Abs(c.otherShip.x - s.ship.x) + s.ship.parts.Count + c.otherShip.parts.Count;

		int? nullableAlignment = null;
		nullableAlignment ??= FindClosestAlignment(inactiveCannons: false, nonArmored: false);
		nullableAlignment ??= FindClosestAlignment(inactiveCannons: false, nonArmored: true);
		nullableAlignment ??= FindClosestAlignment(inactiveCannons: true, nonArmored: false);
		nullableAlignment ??= FindClosestAlignment(inactiveCannons: true, nonArmored: true);

		List<CardAction> actions = [];
		if (nullableAlignment is { } alignment)
			actions.Add(new AEcholocationMove
			{
				Dir = alignment,
				Return = upgrade == Upgrade.B,
				omitFromTooltips = s == DB.fakeState,
			});
		actions.Add(
			new ATooltipAction
			{
				Tooltips = [
					new TTGlossary("parttrait.brittle"),
					new TTGlossary("parttrait.weak"),
				]
			}
		);
		return actions;

		int? FindClosestAlignment(bool inactiveCannons, bool nonArmored)
		{
			for (var i = 0; i < maxCheck; i++)
			{
				for (var partIndex = 0; partIndex < s.ship.parts.Count; partIndex++)
				{
					if (s.ship.parts[partIndex].type != PType.cannon)
						continue;
					if (!inactiveCannons && !s.ship.parts[partIndex].active)
						continue;
					if (i != 0 && c.otherShip.GetPartAtWorldX(s.ship.x + partIndex - i) is { } enemyPartLeft)
					{
						if (enemyPartLeft.damageModifier is PDamMod.weak or PDamMod.brittle)
							return -i;
						if (nonArmored && enemyPartLeft.damageModifier == PDamMod.none)
							return -i;
					}
					if (c.otherShip.GetPartAtWorldX(s.ship.x + partIndex + i) is { } enemyPartRight)
					{
						if (enemyPartRight.damageModifier is PDamMod.weak or PDamMod.brittle)
							return i;
						if (nonArmored && enemyPartRight.damageModifier == PDamMod.none)
							return i;
					}
				}
			}
			return null;
		}
	}

	public sealed class AEcholocationMove : CardAction
	{
		public required int Dir;
		public required bool Return;

		public override List<Tooltip> GetTooltips(State s)
			=> new AMove
			{
				targetPlayer = true,
				dir = Dir,
				ignoreFlipped = true,
				ignoreHermes = true
			}.GetTooltips(s);

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			if (Return)
				c.SetEcholocationReturnPosition(s.ship.x);
			
			c.QueueImmediate(new AMove
			{
				targetPlayer = true,
				dir = Dir,
				ignoreFlipped = true,
				ignoreHermes = true
			});
		}
	}
}
