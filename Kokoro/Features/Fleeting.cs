using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FSPRO;
using HarmonyLib;
using Newtonsoft.Json;
using Nickel;
using Shockah.Shared;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.IFleetingApi Fleeting { get; } = new FleetingApi();
		
		public sealed class FleetingApi : IKokoroApi.IV2.IFleetingApi
		{
			public ICardTraitEntry Trait
				=> FleetingManager.Trait;

			public Spr TopIconLayer
				=> FleetingManager.TopIconLayer.Sprite;

			public Spr BottomIconLayer
				=> FleetingManager.BottomIconLayer.Sprite;

			public Spr CombinedIcon
				=> FleetingManager.CombinedIcon.Sprite;

			public IKokoroApi.IV2.IFleetingApi.ICardSelect ModifyCardSelect(ACardSelect action)
				=> new CardSelectWrapper { Wrapped = action };

			public IKokoroApi.IV2.IFleetingApi.ICardBrowse ModifyCardBrowse(CardBrowse route)
				=> new CardBrowseWrapper { Wrapped = route };

			public void RegisterHook(IKokoroApi.IV2.IFleetingApi.IHook hook, double priority = 0)
				=> FleetingManager.Instance.Register(hook, priority);

			public void UnregisterHook(IKokoroApi.IV2.IFleetingApi.IHook hook)
				=> FleetingManager.Instance.Unregister(hook);
			
			private sealed class CardSelectWrapper : IKokoroApi.IV2.IFleetingApi.ICardSelect
			{
				public required ACardSelect Wrapped { get; init; }

				public bool? FilterFleeting
				{
					get => ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(Wrapped, "FilterFleeting");
					set => ModEntry.Instance.Helper.ModData.SetOptionalModData(Wrapped, "FilterFleeting", value);
				}

				[JsonIgnore]
				public ACardSelect AsCardAction
					=> Wrapped;
				
				public IKokoroApi.IV2.IFleetingApi.ICardSelect SetFilterFleeting(bool? value)
				{
					FilterFleeting = value;
					return this;
				}
			}
			
			private sealed class CardBrowseWrapper : IKokoroApi.IV2.IFleetingApi.ICardBrowse
			{
				public required CardBrowse Wrapped { get; init; }

				public bool? FilterFleeting
				{
					get => ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(Wrapped, "FilterFleeting");
					set => ModEntry.Instance.Helper.ModData.SetOptionalModData(Wrapped, "FilterFleeting", value);
				}

				public CardBrowse AsRoute
					=> Wrapped;
				
				public IKokoroApi.IV2.IFleetingApi.ICardBrowse SetFilterFleeting(bool? value)
				{
					FilterFleeting = value;
					return this;
				}
			}
			
			internal sealed class ShouldExhaustViaFleetingArgs : IKokoroApi.IV2.IFleetingApi.IHook.IShouldExhaustViaFleetingArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public Card Card { get; internal set; } = null!;
			}
			
			internal sealed class BeforeExhaustViaFleetingArgs : IKokoroApi.IV2.IFleetingApi.IHook.IBeforeExhaustViaFleetingArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IReadOnlyList<Card> Cards { get; internal set; } = null!;
			}
			
			internal sealed class OnExhaustViaFleetingArgs : IKokoroApi.IV2.IFleetingApi.IHook.IOnExhaustViaFleetingArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IReadOnlyList<Card> Cards { get; internal set; } = null!;
			}
		}
	}
}

internal sealed class FleetingManager : HookManager<IKokoroApi.IV2.IFleetingApi.IHook>
{
	internal static readonly FleetingManager Instance = new();
	
	internal static ICardTraitEntry Trait = null!;
	internal static ISpriteEntry TopIconLayer = null!;
	internal static ISpriteEntry BottomIconLayer = null!;
	internal static ISpriteEntry CombinedIcon = null!;
	
	private FleetingManager() : base(ModEntry.Instance.Package.Manifest.UniqueName)
	{
	}

	internal static void Setup(IHarmony harmony)
	{
		TopIconLayer = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/FleetingTop.png"));
		BottomIconLayer = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/FleetingBottom.png"));
		CombinedIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Fleeting.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Fleeting", new()
		{
			Icon = (_, _) => CombinedIcon.Sprite,
			Renderer = (_, _, position) =>
			{
				Draw.Sprite(BottomIconLayer.Sprite, position.x, position.y, color: Colors.white.fadeAlpha(0.7));
				Draw.Sprite(TopIconLayer.Sprite, position.x, position.y);
				return true;
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "Fleeting", "name"]).Localize,
			Tooltips = (_, _) => [
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Fleeting")
				{
					Icon = CombinedIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "Fleeting", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "Fleeting", "description"]),
				}
			]
		});
		
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AEndTurn), nameof(AEndTurn.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AEndTurn_Begin_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ACardSelect), nameof(ACardSelect.BeginWithRoute)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardSelect_BeginWithRoute_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.GetCardList)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Postfix))
		);
	}

	private static void AEndTurn_Begin_Prefix(Combat c)
	{
		if (c.cardActions.Any(a => a is AEndTurn))
			return;
		c.QueueImmediate(new ExhaustFleetingAction());
	}
	
	private static void ACardSelect_BeginWithRoute_Postfix(ACardSelect __instance, ref Route? __result)
	{
		if (__result is not CardBrowse route)
			return;
		
		ModEntry.Instance.Helper.ModData.SetOptionalModData(route, "FilterFleeting", ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterFleeting"));
	}

	private static void CardBrowse_GetCardList_Postfix(CardBrowse __instance, G g, ref List<Card> __result)
	{
		var filterFleeting = ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterFleeting");
		if (filterFleeting is null)
			return;

		for (var i = __result.Count - 1; i >= 0; i--)
			if (filterFleeting is not null && ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(g.state, __result[i], Trait) != filterFleeting.Value)
				__result.RemoveAt(i);
	}

	private sealed class ExhaustFleetingAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0.3;
			
			var toExhaust = c.hand
				.Where(card => ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, Trait))
				.Where(card => ModEntry.Instance.ArgsPool.Do<ApiImplementation.V2Api.FleetingApi.ShouldExhaustViaFleetingArgs, bool>(args =>
				{
					args.State = s;
					args.Combat = c;
					args.Card = card;

					foreach (var hook in Instance.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, s.EnumerateAllArtifacts()))
					{
						if (hook.ShouldExhaustViaFleeting(args) is { } result)
							return result;
					}
					return true;
				}))
				.ToList();

			if (toExhaust.Count == 0)
			{
				timer = 0;
				return;
			}

			ModEntry.Instance.ArgsPool.Do<ApiImplementation.V2Api.FleetingApi.BeforeExhaustViaFleetingArgs>(args =>
			{
				args.State = s;
				args.Combat = c;
				args.Cards = toExhaust;

				foreach (var hook in Instance.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, s.EnumerateAllArtifacts()))
					hook.BeforeExhaustViaFleeting(args);
			});
		
			Audio.Play(Event.CardHandling);
			foreach (var card in toExhaust) {
				card.ExhaustFX();
				c.hand.Remove(card);
				c.SendCardToExhaust(s, card);
				timer = 0.3;
			}
		
			ModEntry.Instance.ArgsPool.Do<ApiImplementation.V2Api.FleetingApi.OnExhaustViaFleetingArgs>(args =>
			{
				args.State = s;
				args.Combat = c;
				args.Cards = toExhaust;

				foreach (var hook in Instance.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, s.EnumerateAllArtifacts()))
					hook.OnExhaustViaFleeting(args);
			});
		}
	}
}