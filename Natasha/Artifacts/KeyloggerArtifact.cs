using FSPRO;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class KeyloggerArtifact : Artifact, IRegisterable, INatashaHook
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("Keylogger", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.NatashaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Keylogger.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Keylogger", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Keylogger", "description"]).Localize
		});

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AEndTurn), nameof(AEndTurn.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AEndTurn_Begin_Prefix))
		);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> [.. (Limited.Trait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? [])];

	private static void AEndTurn_Begin_Prefix(State s, Combat c)
	{
		if (c.energy <= 0)
			return;
		if (c.cardActions.Any(a => a is AEndTurn))
			return;
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is KeyloggerArtifact) is not { } artifact)
			return;
		if (!c.hand.Any(card => ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, Limited.Trait)))
			return;

		c.QueueImmediate(new Action { artifactPulse = artifact.Key() });
	}

	private sealed class Action : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			var limitedCards = c.hand.Where(card => ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, Limited.Trait)).ToList();
			switch (limitedCards.Count)
			{
				case 0:
					timer = 0;
					break;
				case 1:
					HandleCard(limitedCards[0]);
					break;
				default:
					HandleCard(limitedCards[s.rngActions.NextInt() % limitedCards.Count]);
					break;
			}

			void HandleCard(Card card)
			{
				limitedCards[0].SetLimitedUses(limitedCards[0].GetLimitedUses(s) + 1);
				Audio.Play(Event.Status_PowerUp);
			}
		}
	}
}