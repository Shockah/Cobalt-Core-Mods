using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DrakeMaxArtifact : DuoArtifact
{
	internal static ExternalSprite WormSprite { get; private set; } = null!;
	internal static ExternalStatus WormStatus { get; private set; } = null!;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetAllTooltips)),
			postfix: new HarmonyMethod(GetType(), nameof(Card_GetAllTooltips_Postfix))
		);
	}

	protected internal override void RegisterArt(ISpriteRegistry registry, string namePrefix)
	{
		base.RegisterArt(registry, namePrefix);
		WormSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Icon.Worm",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Icons", "Worm.png"))
		);
	}

	protected internal override void RegisterStatuses(IStatusRegistry registry, string namePrefix)
	{
		base.RegisterStatuses(registry, namePrefix);
		WormStatus = new(
			$"{namePrefix}.Worm",
			isGood: false,
			mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF009900)),
			borderColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF879900)),
			WormSprite,
			affectedByTimestop: true
		);
		WormStatus.AddLocalisation(I18n.WormStatusName, I18n.WormStatusDescription);
		registry.RegisterStatus(WormStatus);
	}

	protected internal override void RegisterCards(ICardRegistry registry, string namePrefix)
	{
		base.RegisterCards(registry, namePrefix);
		ExternalCard card = new(
			$"{namePrefix}.DrakeMaxArtifactCard",
			typeof(DrakeMaxArtifactCard),
			ExternalSprite.GetRaw((int)StableSpr.cards_hacker),
			ExternalDeck.GetRaw((int)Deck.ephemeral)
		);
		card.AddLocalisation(I18n.DrakeMaxArtifactCardName);
		registry.RegisterCard(card);
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? new();
		tooltips.Add(new TTCard { card = new DrakeMaxArtifactCard() });
		return tooltips;
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		Pulse();
		combat.Queue(new AAddCard
		{
			card = new DrakeMaxArtifactCard(),
			destination = CardDestination.Deck
		});
		combat.Queue(new AAddCard
		{
			card = new WormFood { temporaryOverride = true },
			destination = CardDestination.Deck
		});
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		int worm = combat.otherShip.Get((Status)WormStatus.Id!.Value);
		if (worm <= 0)
			return;

		var partXWithIntent = Enumerable.Range(0, combat.otherShip.parts.Count)
			.Where(x => combat.otherShip.parts[x].intent is not null)
			.Select(x => x + combat.otherShip.x)
			.ToList();

		for (int i = 0; i < worm; i++)
		{
			if (partXWithIntent.Count == 0)
				break;
			int partX = partXWithIntent[state.rngActions.NextInt() % partXWithIntent.Count];
			combat.QueueImmediate(new AStunPart { worldX = partX });
		}

		combat.otherShip.Add((Status)WormStatus.Id!.Value, -1);
	}

	private static void Card_GetAllTooltips_Postfix(State s, ref IEnumerable<Tooltip> __result)
	{
		var result = __result;
		IEnumerable<Tooltip> ModifyResult()
		{
			foreach (var tooltip in result)
			{
				if (tooltip is TTGlossary glossary && glossary.key == $"status.{WormStatus.Id!.Value}" && (glossary.vals is null || glossary.vals.Length == 0 || Equals(glossary.vals[0], "<c=boldPink>0</c>")))
					glossary.vals = new object[] { "<c=boldPink>1</c>" };
				yield return tooltip;
			}
		}
		__result = ModifyResult();
	}
}

[CardMeta(dontOffer = true)]
internal sealed class DrakeMaxArtifactCard : Card
{
	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
			temporary = true,
			retain = true,
			description = I18n.DrakeMaxArtifactCardDescription
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		var cards = c.hand.OfType<WormFood>().ToList();

		List<CardAction> actions = new();
		foreach (var card in cards)
			actions.Add(new AExhaustOtherCard { uuid = card.uuid });
		actions.Add(new AStatus
		{
			status = (Status)DrakeMaxArtifact.WormStatus.Id!.Value,
			statusAmount = cards.Count,
			targetPlayer = false
		});
		return actions;
	}
}