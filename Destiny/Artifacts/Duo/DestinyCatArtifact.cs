using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FSPRO;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.Destiny;

internal sealed class DestinyCatArtifact : Artifact, IRegisterable
{
	[JsonProperty]
	private int TurnCounter;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		
		helper.Content.Artifacts.RegisterArtifact("DestinyCat", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/Cat.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Cat", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Cat", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DestinyDeck.Deck, Deck.colorless]);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> Explosive.ExplosiveTrait.Configuration.Tooltips?.Invoke(DB.fakeState, null)?.ToList();

	public override int? GetDisplayNumber(State s)
		=> TurnCounter;

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		TurnCounter++;

		if (TurnCounter < 8)
			return;

		TurnCounter = 0;
		combat.QueueImmediate(new Action { artifactPulse = Key() });
	}

	private sealed class Action : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			
			var didAnything = false;
			var cards = s.deck.Concat(c.hand).Concat(c.discard).Concat(c.exhausted);
			
			foreach (var card in cards)
			{
				if (card is not CannonColorless)
					continue;
				if (ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, Explosive.ExplosiveTrait))
					continue;
				ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, card, Explosive.ExplosiveTrait, true, false);
				didAnything = true;
			}
			
			if (didAnything)
				Audio.Play(Event.Status_PowerUp);
		}
	}
}