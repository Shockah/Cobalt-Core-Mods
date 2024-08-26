using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class ManInTheMiddleCard : Card, IRegisterable, IHasCustomCardTraits
{
	internal record SerializableObjectEntry(
		string UniqueName,
		double Weight
	);
	
	private static readonly UK CancelMitmUK = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();

	internal static readonly Dictionary<string, INatashaApi.ManInTheMiddleStaticObjectEntry> RegisteredObjects = new List<INatashaApi.ManInTheMiddleStaticObjectEntry>()
	{
		new($"{typeof(ModEntry).Namespace!}::Asteroid", _ => new Asteroid()),
		new($"{typeof(ModEntry).Namespace!}::Geode", _ => new Geode(), 0.5),
		new($"{typeof(ModEntry).Namespace!}::Mine", _ => new SpaceMine()),
		new($"{typeof(ModEntry).Namespace!}::BigMine", _ => new SpaceMine { bigMine = true }, 0.5),
		new($"{typeof(ModEntry).Namespace!}::AttackDrone", _ => new AttackDrone()),
		new($"{typeof(ModEntry).Namespace!}::AttackDroneMk2", _ => new AttackDrone { upgraded = true }, 0.5),
		new($"{typeof(ModEntry).Namespace!}::ShieldDrone", _ => new ShieldDrone { targetPlayer = true }),
		new($"{typeof(ModEntry).Namespace!}::EnergyDrone", _ => new EnergyDrone { targetPlayer = true }, 0.25),
		new($"{typeof(ModEntry).Namespace!}::JupiterDrone", _ => new JupiterDrone(), 0.25),
		new($"{typeof(ModEntry).Namespace!}::SportOrb", _ => new Football(), 0.1, (_, w) => w / 2.0)
	}.ToDictionary(e => e.UniqueName, e => e);

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.NatashaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/ManInTheMiddle.png"), StableSpr.cards_goat).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "ManInTheMiddle", "name"]).Localize
		});

		Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.None, 3);
		Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.A, 5);
		Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.B, 3);

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.IsVisible)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_IsVisible_Postfix))
		);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { Limited.Trait };

	public override CardData GetData(State state)
		=> new() { cost = 1, description = ModEntry.Instance.Localizations.Localize(["card", "ManInTheMiddle", "description", upgrade.ToString()]) };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [new Action { Random = true }],
			_ => [new Action { Random = false }]
		};

	private static void Combat_IsVisible_Postfix(Combat __instance, ref bool __result)
	{
		if (__instance.routeOverride is ActionRoute)
			__result = true;
	}
	
	private sealed class Action : CardAction
	{
		public bool Random;

		public override List<Tooltip> GetTooltips(State s)
			=> [
				new TTGlossary("action.spawn"),
				.. new Asteroid().GetTooltips()
			];
		
		public override Route? BeginWithRoute(G g, State s, Combat c)
			=> new ActionRoute { Random = Random };
	}

	private sealed class ActionRoute : Route
	{
		public required bool Random;
		
		public override bool GetShowOverworldPanels()
			=> true;

		public override bool CanBePeeked()
			=> false;

		public override void Render(G g)
		{
			base.Render(g);

			if (g.state.route is not Combat combat)
			{
				g.CloseRoute(this);
				return;
			}

			Draw.Rect(0, 0, MG.inst.PIX_W, MG.inst.PIX_H, Colors.black.fadeAlpha(0.5));

			var keyPrefix = $"{typeof(ModEntry).Namespace!}::{nameof(ManInTheMiddleCard)}";
			for (var i = 0; i < g.state.ship.parts.Count; i++)
			{
				var part = g.state.ship.parts[i];
				if (g.boxes.FirstOrDefault(b => b.key is { } key && key.k == StableUK.part && key.v == i && key.str == "combat_ship_player") is not { } realBox)
					continue;

				g.Push(rect: new Rect(realBox.rect.x - i * 16 + 1, realBox.rect.y, realBox.rect.w, realBox.rect.h));

				combat.otherShip.RenderPartUI(g, combat, part, i, keyPrefix, isPreview: false);

				if (g.boxes.FirstOrDefault(b => b.key is { } key && key.k == StableUK.part && key.v == i && key.str == keyPrefix) is { } box)
				{
					var partIndex = i;
					box.onMouseDown = new MouseDownHandler(() => OnPartSelected(g, partIndex));
					if (box.IsHover())
					{
						if (!Input.gamepadIsActiveInput)
							MouseUtil.DrawGamepadCursor(box);
						part.hilight = true;
					}
				}

				g.Pop();
			}

			SharedArt.ButtonText(
				g,
				new Vec(MG.inst.PIX_W - 69, MG.inst.PIX_H - 31),
				CancelMitmUK,
				ModEntry.Instance.Localizations.Localize(["card", "RemoteExecution", "ui", "cancel"]),
				onMouseDown: new MouseDownHandler(() => g.CloseRoute(this))
			);
		}

		private void OnPartSelected(G g, int partIndex)
		{
			if (g.state.route is not Combat combat)
			{
				g.CloseRoute(this);
				return;
			}
			
			var @object = Random ? CreateNextObject(g.state, combat) : new Asteroid();
			combat.QueueImmediate(new ASpawn { fromX = partIndex, thing = @object });
			g.CloseRoute(this);
		}
		
		private static StuffBase CreateNextObject(State state, Combat combat)
		{
			var queue = ObtainObjectQueue(combat);

			var weighted = new WeightedRandom<SerializableObjectEntry>();
			foreach (var serializableEntry in queue)
				weighted.Add(new(serializableEntry.Weight, serializableEntry));

			while (true)
			{
				if (weighted.Items.Count == 0)
					return new Asteroid();

				var chosenSerializableEntry = weighted.Next(state.rngActions, consume: true);
				if (!RegisteredObjects.TryGetValue(chosenSerializableEntry.UniqueName, out var chosenEntry))
					continue;

				queue.Remove(chosenSerializableEntry);
				queue.Add(chosenSerializableEntry with { Weight = chosenEntry.WeightProvider?.Invoke(state, chosenSerializableEntry.Weight) ?? chosenSerializableEntry.Weight });
				return chosenEntry.Factory(state);
			}
		}

		private static List<SerializableObjectEntry> ObtainObjectQueue(Combat combat)
		{
			var queue = ModEntry.Instance.Helper.ModData.ObtainModData<List<SerializableObjectEntry>>(combat, $"{nameof(ManInTheMiddleCard)}::ObjectQueue");

			for (var i = queue.Count - 1; i >= 0; i--)
				if (!RegisteredObjects.ContainsKey(queue[i].UniqueName))
					queue.RemoveAt(i);

			if (queue.Count == 0)
				queue.AddRange(RegisteredObjects.Values.Select(e => new SerializableObjectEntry(e.UniqueName, e.InitialWeight)));

			return queue;
		}
	}
}
