using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class DarkWebDataArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("DarkWebData", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.NatashaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/DarkWebData.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "DarkWebData", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "DarkWebData", "description"]).Localize
		});

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ArtifactReward), nameof(ArtifactReward.GetBlockedArtifacts)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactReward_GetBlockedArtifacts_Postfix))
		);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> [.. (ModEntry.Instance.KokoroApi.Limited.Trait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? [])];

	public override void OnReceiveArtifact(State state)
	{
		base.OnReceiveArtifact(state);
		state.GetCurrentQueue().QueueImmediate(ModEntry.Instance.KokoroApi.Limited.ModifyCardSelect(new ACardSelect
		{
			browseAction = new CardSelectAction(),
			browseSource = CardBrowse.Source.Deck,
			filterExhaust = true,
			filterTemporary = false,
		}).SetFilterLimited(false).AsCardAction);
	}

	private static void ArtifactReward_GetBlockedArtifacts_Postfix(State s, ref HashSet<Type> __result)
	{
		if (!s.GetAllCards().Any(card => card.GetMeta().deck != Deck.trash && ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, ModEntry.Instance.Helper.Content.Cards.ExhaustCardTrait)))
			__result.Add(typeof(DarkWebDataArtifact));
	}

	private sealed class CardSelectAction : CardAction
	{
		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			if (selectedCard is null)
				return null;

			ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, selectedCard, ModEntry.Instance.Helper.Content.Cards.ExhaustCardTrait, false, permanent: true);
			ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, selectedCard, ModEntry.Instance.KokoroApi.Limited.Trait, true, permanent: true);

			return new CustomShowCards
			{
				messageKey = $"{ModEntry.Instance.Package.Manifest.UniqueName}::{nameof(DarkWebDataArtifact)}::ShowCards",
				Message = ModEntry.Instance.Localizations.Localize(["artifact", "DarkWebData", "ui", "done"]),
				cardIds = [selectedCard.uuid]
			};
		}

		public override string? GetCardSelectText(State s)
			=> ModEntry.Instance.Localizations.Localize(["artifact", "DarkWebData", "ui", "title"]);
	}

	private sealed class CustomShowCards : ShowCards
	{
		public required string Message;

		public override void Render(G g)
		{
			DB.currentLocale.strings[messageKey] = Message;
			base.Render(g);
		}
	}
}