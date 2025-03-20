using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.StarforgeInitiative.Actions;

namespace Shockah.StarforgeInitiative;

internal sealed class BreadnaughtGrandmaShopEventChanges : IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Events), nameof(Events.GrandmaShop)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Events_GrandmaShop_Postfix))
		);
	}

	private static void Events_GrandmaShop_Postfix(ref List<Choice> __result)
	{
		__result.Insert(1, new Choice
		{
			label = ModEntry.Instance.Localizations.Localize(["ship", "Breadnaught", "event", "GrandmaShop", "Choice-GoodieBag", "title"]),
			key = "GrandmaShop_Rare",
			actions = [
				new AAddCard { card = new BreadnaughtBasicPackageCard(), callItTheDeckNotTheDrawPile = true },
				new TooltipAction { Tooltips = new BreadnaughtBasicPackageCard().GetAllTooltips(MG.inst.g, DB.fakeState).ToList() }
			],
		});
	}
}