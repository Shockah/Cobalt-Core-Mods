using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.CatExpansion;

internal sealed class PersonalDataArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("PersonalData", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.catartifact,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifact/PersonalData.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "PersonalData", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "PersonalData", "description"]).Localize
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ACardSelect), nameof(ACardSelect.BeginWithRoute)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardSelect_BeginWithRoute_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.GetCardList)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Postfix))
		);
	}

	public override void OnReceiveArtifact(State state)
	{
		base.OnReceiveArtifact(state);
		
		state.GetCurrentQueue().InsertRange(
			0,
			state.characters
				.Where(character => character.deckType is not null)
				.Select(character => character.deckType!.Value)
				.Select(deckType =>
				{
					var route = new ACardSelect { browseSource = CardBrowse.Source.Deck, browseAction = new UpgradeCardBrowseAction(), filterUpgrade = Upgrade.None };
					ModEntry.Instance.Helper.ModData.SetModData(route, "FilterDeck", deckType);
					return route;
				})
		);
	}
	
	private static void ACardSelect_BeginWithRoute_Postfix(ACardSelect __instance, ref Route? __result)
	{
		if (__result is not CardBrowse route)
			return;

		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<Deck>(__instance, "FilterDeck") is { } filterDeck)
			ModEntry.Instance.Helper.ModData.SetModData(route, "FilterDeck", filterDeck);
	}

	private static void CardBrowse_GetCardList_Postfix(CardBrowse __instance, ref List<Card> __result)
	{
		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<Deck>(__instance, "FilterDeck") is not { } filterDeck)
			return;

		for (var i = __result.Count - 1; i >= 0; i--)
			if (__result[i].GetMeta().deck != filterDeck)
				__result.RemoveAt(i);
	}

	private sealed class UpgradeCardBrowseAction : CardAction
	{
		public override string GetCardSelectText(State s)
			=> ModEntry.Instance.Localizations.Localize(["artifact", "PersonalData", "browseTitle"]);

		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			var baseResult = base.BeginWithRoute(g, s, c);
			if (selectedCard is null)
			{
				timer = 0;
				return baseResult;
			}

			var copy = Mutil.DeepCopy(selectedCard);
			copy.drawAnim = 1;
			return new CardUpgrade { cardCopy = copy };
		}
	}
}