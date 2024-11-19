using HarmonyLib;
using Newtonsoft.Json;
using Nickel;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.ICustomCardUpgradeApi CustomCardUpgrade { get; } = new CustomCardUpgradeApi();
		
		public sealed class CustomCardUpgradeApi : IKokoroApi.IV2.ICustomCardUpgradeApi
		{
			public IKokoroApi.IV2.ICustomCardUpgradeApi.IRoute MakeCustom(CardUpgrade route)
				=> new CustomCardUpgradeManager.RouteWrapper(Mutil.DeepCopy(route));
		}
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
	
	internal sealed class RouteWrapper(CardUpgrade route) : IKokoroApi.IV2.ICustomCardUpgradeApi.IRoute
	{
		[JsonIgnore]
		public CardUpgrade AsRoute
			=> route;

		public bool IsInPlace
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(route, "IsInPlace");
			set => ModEntry.Instance.Helper.ModData.SetModData(route, "IsInPlace", value);
		}

		public IKokoroApi.IV2.ICustomCardUpgradeApi.IRoute SetInPlace(bool value)
		{
			this.IsInPlace = value;
			return this;
		}
	}
}