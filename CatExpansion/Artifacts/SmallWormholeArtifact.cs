using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.CatExpansion;

internal sealed class SmallWormholeArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("SmallWormhole", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.catartifact,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifact/SmallWormhole.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "SmallWormhole", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "SmallWormhole", "description"]).Localize
		});
	}

	public override void OnReceiveArtifact(State state)
	{
		base.OnReceiveArtifact(state);
		state.GetCurrentQueue().InsertRange(0, [
			new ArtifactOfferingAction(),
			new CorrespondingExeCardAction(),
		]);
	}

	private sealed class ArtifactOfferingAction : AArtifactOffering
	{
		public override Route BeginWithRoute(G g, State s, Combat c)
		{
			base.BeginWithRoute(g, s, c);
			return new ArtifactReward
			{
				artifacts = GetCustomArtifactOffering(s),
				canSkip = canSkip,
			};
		}

		private static List<Artifact> GetCustomArtifactOffering(State state)
		{
			var blockedArtifactTypes = ArtifactReward.GetBlockedArtifacts(state);
			var nonCrewCharacterDeckTypes = NewRunOptions.allChars
				.Select(deckType => deckType == Deck.colorless ? Deck.catartifact : deckType)
				.Where(deckType => state.characters.All(character => character.deckType != deckType))
				.Where(deckType => deckType == Deck.catartifact || ModEntry.Instance.EssentialsApi.GetExeCardTypeForDeck(deckType) is not null)
				.ToList();

			return DB.artifacts
				.Select(kvp => (Key: kvp.Key, Type: kvp.Value, Meta: DB.artifactMetas[kvp.Key]))
				.Where(e => e.Meta.pools.Contains(ArtifactPool.Common) && !e.Meta.pools.Contains(ArtifactPool.Unreleased) && !e.Meta.pools.Contains(ArtifactPool.EventOnly))
				.Where(e => nonCrewCharacterDeckTypes.Contains(e.Meta.owner))
				.Where(e => !blockedArtifactTypes.Contains(e.Type))
				.Where(e => !(ModEntry.Instance.Helper.Content.Characters.V2.LookupByDeck(e.Meta.owner)?.Configuration.Starters.artifacts.Any(starter => starter.GetType() == e.Type) ?? false))
				.Shuffle(state.rngArtifactOfferings)
				.Take(4)
				.OrderBy(e => NewRunOptions.allChars.IndexOf(e.Meta.owner == Deck.catartifact ? Deck.colorless : e.Meta.owner))
				.Select(e =>
				{
					var artifact = (Artifact)Activator.CreateInstance(e.Type)!;
					ModEntry.Instance.Helper.ModData.SetModData(artifact, "AddedBySmallWormhole", true);
					return artifact;
				})
				.ToList();
		}
	}

	private sealed class CorrespondingExeCardAction : CardAction
	{
		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			var baseResult = base.BeginWithRoute(g, s, c);
			timer = 0;
			
			foreach (var artifact in s.EnumerateAllArtifacts())
			{
				if (!ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(artifact, "AddedBySmallWormhole"))
					continue;
				ModEntry.Instance.Helper.ModData.RemoveModData(artifact, "AddedBySmallWormhole");

				var meta = artifact.GetMeta();
				var exeCardType = meta.owner == Deck.catartifact ? typeof(ColorlessCATSummon) : ModEntry.Instance.EssentialsApi.GetExeCardTypeForDeck(artifact.GetMeta().owner);
				if (exeCardType is null)
					return baseResult;

				var exeCard = (Card)Activator.CreateInstance(exeCardType)!;
				exeCard.drawAnim = 1;
				return new CardUpgrade { cardCopy = exeCard };
			}

			return baseResult;
		}
	}
}