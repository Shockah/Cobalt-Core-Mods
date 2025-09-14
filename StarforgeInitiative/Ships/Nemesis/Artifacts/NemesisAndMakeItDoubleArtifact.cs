using System.Linq;
using System.Reflection;
using FSPRO;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class NemesisAndMakeItDoubleArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("NemesisAndMakeItDouble", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.EventOnly],
				unremovable = true,
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Nemesis/Artifact/AndMakeItDouble.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Nemesis", "artifact", "AndMakeItDouble", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Nemesis", "artifact", "AndMakeItDouble", "description"]).Localize
		});
	}

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);
		combat.QueueImmediate(new ActivateCannonsAction { artifactPulse = Key() });
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		combat.QueueImmediate(new DeactivateCannonsAction { artifactPulse = Key() });
	}

	private sealed class DeactivateCannonsAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0.3;
			
			if (s.ship.key != NemesisShip.ShipEntry.UniqueName)
			{
				timer = 0;
				return;
			}

			var parts = s.ship.parts
				.Where(p => p.type == PType.cannon)
				.Where(p => p.skin == NemesisShip.LeftCannonEntry.UniqueName || p.skin == NemesisShip.MidCannonEntry.UniqueName || p.skin == NemesisShip.RightCannonEntry.UniqueName)
				.ToList();

			foreach (var part in parts)
				part.active = false;
			if (parts.Count != 0)
				parts[^1].active = true;
			Audio.Play(Event.TogglePart);
		}
	}

	private sealed class ActivateCannonsAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0.3;

			if (s.ship.key != NemesisShip.ShipEntry.UniqueName)
			{
				timer = 0;
				return;
			}

			var parts = s.ship.parts
				.Where(p => p.type == PType.cannon)
				.Where(p => p.skin == NemesisShip.LeftCannonEntry.UniqueName || p.skin == NemesisShip.MidCannonEntry.UniqueName || p.skin == NemesisShip.RightCannonEntry.UniqueName)
				.ToList();

			foreach (var part in parts)
				part.active = true;
			Audio.Play(Event.TogglePart);
		}
	}
}