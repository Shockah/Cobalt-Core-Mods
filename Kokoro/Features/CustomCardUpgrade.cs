using HarmonyLib;
using Nickel;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class ActionApiImplementation
	{
		public IKokoroApi.IActionApi.ICustomCardUpgrade MakeCustomCardUpgrade(CardUpgrade route)
			=> new CustomCardUpgradeManager.RouteWrapper(route);
	}
}

internal sealed class CustomCardUpgradeManager
{
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardUpgrade), nameof(CardUpgrade.FinallyReallyUpgrade)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(FinallyReallyUpgrade_Prefix))
		);
	}
	
	private static bool FinallyReallyUpgrade_Prefix(CardUpgrade __instance, G g, Card newCard)
	{
		if (!ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(__instance, "IsInPlace"))
			return true;

		var card = g.state.FindCard(newCard.uuid);
		if (card is null)
			return true;

		card.upgrade = newCard.upgrade;
		return false;
	}
	
	internal sealed class RouteWrapper(CardUpgrade route) : IKokoroApi.IActionApi.ICustomCardUpgrade
	{
		public CardUpgrade AsRoute
			=> route;

		public bool IsInPlace
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(route, "IsInPlace");
			set => ModEntry.Instance.Helper.ModData.SetModData(route, "IsInPlace", value);
		}

		public IKokoroApi.IActionApi.ICustomCardUpgrade SetInPlace(bool value)
		{
			this.IsInPlace = value;
			return this;
		}
	}
}