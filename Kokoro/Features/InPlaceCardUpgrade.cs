using HarmonyLib;
using Newtonsoft.Json;
using Nickel;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.IInPlaceCardUpgradeApi InPlaceCardUpgrade { get; } = new InPlaceCardUpgradeApi();
		
		public sealed class InPlaceCardUpgradeApi : IKokoroApi.IV2.IInPlaceCardUpgradeApi
		{
			public IKokoroApi.IV2.IInPlaceCardUpgradeApi.ICardUpgrade ModifyCardUpgrade(CardUpgrade route)
				=> new InPlaceCardUpgradeManager.CardUpgradeWrapper(Mutil.DeepCopy(route));
			
			internal sealed class InPlaceCardUpgradeStrategyApplyInPlaceCardUpgradeArgs : IKokoroApi.IV2.IInPlaceCardUpgradeApi.IInPlaceCardUpgradeStrategy.IApplyInPlaceCardUpgradeArgs
			{
				public State State { get; internal set; } = null!;
				public CardUpgrade Route { get; internal set; } = null!;
				public Card TargetCard { get; internal set; } = null!;
				public Card TemplateCard { get; internal set; } = null!;
			}
		}
	}
}

internal static class InPlaceCardUpgradeManager
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

		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<IKokoroApi.IV2.IInPlaceCardUpgradeApi.IInPlaceCardUpgradeStrategy>(__instance, "InPlaceCardUpgradeStrategy") is { } strategy)
			ModEntry.Instance.ArgsPool.Do<ApiImplementation.V2Api.InPlaceCardUpgradeApi.InPlaceCardUpgradeStrategyApplyInPlaceCardUpgradeArgs>(args =>
			{
				args.State = g.state;
				args.Route = __instance;
				args.TargetCard = card;
				args.TemplateCard = newCard;

				strategy.ApplyInPlaceCardUpgrade(args);
			});
		else
			card.upgrade = newCard.upgrade;

		return false;
	}
	
	internal sealed class CardUpgradeWrapper(CardUpgrade route) : IKokoroApi.IV2.IInPlaceCardUpgradeApi.ICardUpgrade
	{
		[JsonIgnore]
		public CardUpgrade AsRoute
			=> route;

		public bool IsInPlace
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(route, "IsInPlace");
			set => ModEntry.Instance.Helper.ModData.SetModData(route, "IsInPlace", value);
		}
		
		public IKokoroApi.IV2.IInPlaceCardUpgradeApi.IInPlaceCardUpgradeStrategy? InPlaceCardUpgradeStrategy
		{
			get => ModEntry.Instance.Helper.ModData.GetOptionalModData<IKokoroApi.IV2.IInPlaceCardUpgradeApi.IInPlaceCardUpgradeStrategy>(route, "InPlaceCardUpgradeStrategy");
			set => ModEntry.Instance.Helper.ModData.SetOptionalModData(route, "InPlaceCardUpgradeStrategy", value);
		}

		public IKokoroApi.IV2.IInPlaceCardUpgradeApi.ICardUpgrade SetIsInPlace(bool value)
		{
			this.IsInPlace = value;
			return this;
		}

		public IKokoroApi.IV2.IInPlaceCardUpgradeApi.ICardUpgrade SetInPlaceCardUpgradeStrategy(IKokoroApi.IV2.IInPlaceCardUpgradeApi.IInPlaceCardUpgradeStrategy? value)
		{
			this.InPlaceCardUpgradeStrategy = value;
			return this;
		}
	}
}