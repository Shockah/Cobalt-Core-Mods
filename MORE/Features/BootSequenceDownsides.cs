using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.MORE;

internal sealed class BootSequenceDownsides : IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		HibernationHangoverCard.Register(package, helper);

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Events), nameof(Events.BootSequenceDownside)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Events_BootSequenceDownside_Postfix)))
		);
	}

	private static void Events_BootSequenceDownside_Postfix(ref List<Choice> __result)
		=> __result.Add(new Choice
		{
			label = ModEntry.Instance.Localizations.Localize(["event", "BootSequenceDownside", "choices", "HibernationHangover"]),
			key = "BootSequence",
			actions = [new AAddCard { card = new HibernationHangoverCard() }],
		});

	private sealed class HibernationHangoverCard : Card, IRegisterable
	{
		internal static ICardEntry Entry = null!;

		public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
		{
			Entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
			{
				CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					deck = Deck.trash,
					rarity = Rarity.common,
					dontOffer = true
				},
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "BootSequenceDownside", "card", "HibernationHangover", "name"]).Localize
			});
		}

		public override CardData GetData(State state)
			=> new()
			{
				cost = 0,
				exhaust = true,
				description = ModEntry.Instance.Localizations.Localize(["event", "BootSequenceDownside", "card", "HibernationHangover", "description"]),
				art = StableSpr.cards_ColorlessTrash,
			};

		public override List<CardAction> GetActions(State s, Combat c)
			=> [new Action()];

		private sealed class Action : CardAction
		{
			public override List<Tooltip> GetTooltips(State s)
				=> (MG.inst.g.state ?? DB.fakeState).characters
					.Select(character => character.deckType is not null && StatusMeta.deckToMissingStatus.TryGetValue(character.deckType.Value, out var missingStatus) ? missingStatus : (Status?)null)
					.Where(s => s is not null)
					.SelectMany(s => StatusMeta.GetTooltips(s!.Value, 1))
					.ToList();

			public override void Begin(G g, State s, Combat c)
			{
				base.Begin(g, s, c);
				timer = 0;

				var status = s.characters
					.Select(character => character.deckType is not null && StatusMeta.deckToMissingStatus.TryGetValue(character.deckType.Value, out var missingStatus) ? missingStatus : (Status?)null)
					.Where(s => s is not null)
					.Select(s => s!.Value)
					.Shuffle(s.rngActions)
					.FirstOrNull();

				if (status is null)
					return;

				c.QueueImmediate(new AStatus { targetPlayer = true, status = status.Value, statusAmount = 1 });
			}
		}
	}
}
