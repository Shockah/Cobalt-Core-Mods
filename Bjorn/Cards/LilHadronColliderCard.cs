using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Shockah.Bjorn;

public sealed class LilHadronColliderCard : Card, IRegisterable
{
	private static readonly Stack<Card> CardGetDataWithOverridesStack = [];
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BjornDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/LilHadronCollider.png"), StableSpr.cards_HandCannon).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "LilHadronCollider", "name"]).Localize,
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(GetDataWithOverrides)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetDataWithOverrides_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetDataWithOverrides_Finalizer))
		);
	}

	private int GetDamage(State state)
	{
		if (ModEntry.Instance.Helper.Content.Cards.IsCurrentlyCreatingCardTraitStates(state, this))
			return 0;
		if (CardGetDataWithOverridesStack.Count > 1 || (CardGetDataWithOverridesStack.TryPeek(out var stackCard) && stackCard != this))
			return 0;
		if (state.route is not Combat combat)
			return 0;

		var analyzableCount = combat.hand.Count(card => card != this && card.IsAnalyzable(state, combat));
		return GetDmg(state, upgrade.Switch(
			none: () => analyzableCount * 2,
			a: () => analyzableCount * 2,
			b: () => analyzableCount * 3
		));
	}

	public override CardData GetData(State state)
	{
		var description = ModEntry.Instance.Localizations.Localize(["card", "LilHadronCollider", "description", upgrade.ToString(), state.route is Combat ? "stateful" : "stateless"], new { Damage = GetDamage(state) });
		return upgrade.Switch<CardData>(
			none: () => new() { cost = 3, exhaust = true, description = description },
			a: () => new() { cost = 3, exhaust = true, retain = true, buoyant = true, description = description },
			b: () => new() { cost = 3, description = description }
		);
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [
				new AnalyzableVariableHint { CardId = uuid },
				new AnalyzeHandAction { CardId = uuid },
				new AAttack { damage = GetDamage(s), xHint = 2 }
			],
			a: () => [
				new AnalyzableVariableHint { CardId = uuid },
				new AnalyzeHandAction { CardId = uuid },
				new AAttack { damage = GetDamage(s), xHint = 2 }
			],
			b: () => [
				new AnalyzableVariableHint { CardId = uuid },
				new AnalyzeHandAction { CardId = uuid },
				new AAttack { damage = GetDamage(s), xHint = 3 }
			]
		);

	private static void Card_GetDataWithOverrides_Prefix(Card __instance)
		=> CardGetDataWithOverridesStack.Push(__instance);

	private static void Card_GetDataWithOverrides_Finalizer()
		=> CardGetDataWithOverridesStack.TryPop(out _);

	private sealed class AnalyzeHandAction : CardAction
	{
		public required int CardId;

		public override List<Tooltip> GetTooltips(State s)
			=> AnalyzeManager.GetAnalyzeTooltips(s);

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			foreach (var card in c.hand)
				if (card.uuid != CardId)
					ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, card, AnalyzeManager.AnalyzedTrait, true, permanent: false);
		}
	}
}