using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DrakeMaxArtifact : DuoArtifact
{
	private static ExternalSprite StatusSprite = null!;
	internal static ExternalStatus Status = null!;
	
	protected internal override void ApplyPatches(IHarmony harmony)
	{
		base.ApplyPatches(harmony);

		var hook = new Hook();
		Instance.KokoroApi.StatusLogic.RegisterHook(hook);
		Instance.KokoroApi.StatusRendering.RegisterHook(hook);
	}
	
	protected internal override void RegisterArt(ISpriteRegistry registry, string namePrefix, DuoArtifactDefinition definition)
	{
		base.RegisterArt(registry, namePrefix, definition);
		StatusSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Status.Worm",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Icons", "WormStatus.png"))
		);
	}
	
	protected internal override void RegisterStatuses(IStatusRegistry registry, string namePrefix, DuoArtifactDefinition definition)
	{
		base.RegisterStatuses(registry, namePrefix, definition);
		Status = new(
			$"{typeof(ModEntry).Namespace}.Status.Worm",
			isGood: false,
			mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF009900)),
			borderColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF879900)),
			StatusSprite,
			affectedByTimestop: false
		);
		Status.AddLocalisation(I18n.WormStatusName, I18n.WormStatusStatefulDescription);
		registry.RegisterStatus(Status);
	}
	
	protected internal override void RegisterCards(ICardRegistry registry, string namePrefix, DuoArtifactDefinition definition)
	{
		base.RegisterCards(registry, namePrefix, definition);
		ExternalCard card = new(
			$"{namePrefix}.DrakeMaxArtifactCard",
			typeof(DrakeMaxArtifactCard),
			ExternalSprite.GetRaw((int)StableSpr.cards_hacker),
			Instance.Database.DuoArtifactDeck
		);
		card.AddLocalisation(I18n.DrakeMaxArtifactCardName);
		registry.RegisterCard(card);
	}

	public override List<Tooltip> GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? [];
		tooltips.Add(new TTCard { card = new DrakeMaxArtifactCard() });
		return tooltips;
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		combat.Queue([
			new AAddCard
			{
				card = new DrakeMaxArtifactCard(),
				destination = CardDestination.Deck,
				artifactPulse = Key(),
			},
			new AAddCard
			{
				card = new WormFood { temporaryOverride = true },
				destination = CardDestination.Deck,
			},
		]);
	}

	private sealed class Hook : IKokoroApi.IV2.IStatusLogicApi.IHook, IKokoroApi.IV2.IStatusRenderingApi.IHook
	{
		public IReadOnlyList<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
		{
			foreach (var tooltip in args.Tooltips)
			{
				if (tooltip is TTGlossary glossary && glossary.key == $"status.{Status.Id!.Value}" && (glossary.vals is null || glossary.vals.Length == 0 || Equals(glossary.vals[0], "<c=boldPink>0</c>")))
					glossary.vals = ["<c=boldPink>1</c>"];
			}
			return args.Tooltips;
		}

		public void OnStatusTurnTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IOnStatusTurnTriggerArgs args)
		{
			if (args.Status != (Status)Status.Id!.Value || args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
				return;

			var otherShip = args.Ship.isPlayerShip ? args.Combat.otherShip : args.State.ship;
			var wormAmount = otherShip.Get((Status)Status.Id!.Value);
			if (wormAmount <= 0)
				return;

			if (!otherShip.isPlayerShip)
			{
				var partXsWithIntent = Enumerable.Range(0, otherShip.parts.Count)
					.Where(x => otherShip.parts[x].intent is not null)
					.Select(x => x + otherShip.x)
					.ToList();

				foreach (var partXWithIntent in partXsWithIntent.Shuffle(args.State.rngActions).Take(wormAmount))
					args.Combat.Queue(new AStunPart { worldX = partXWithIntent });
			}

			args.Combat.Queue(new AStatus
			{
				targetPlayer = otherShip.isPlayerShip,
				status = (Status)Status.Id!.Value,
				statusAmount = -1
			});
		}
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
			exhaust = true,
			description = I18n.DrakeMaxArtifactCardDescription,
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		var cards = s.deck
			.Concat(c.discard)
			.Concat(c.hand)
			.OfType<WormFood>()
			.ToList();

		return [
			.. cards.Select(card => new AExhaustWherever { uuid = card.uuid }),
			new AStatus
			{
				status = (Status)DrakeMaxArtifact.Status.Id!.Value,
				statusAmount = cards.Count,
				targetPlayer = false,
			}
		];
	}
}