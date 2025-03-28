using HarmonyLib;
using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Shockah.Shared;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.ITemporaryUpgradesApi TemporaryUpgrades { get; } = new TemporaryUpgradesApi();
		
		public sealed class TemporaryUpgradesApi : IKokoroApi.IV2.ITemporaryUpgradesApi
		{
			public ICardTraitEntry CardTrait
				=> TemporaryUpgradesManager.Trait;

			public Tooltip UpgradeTooltip
				=> new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::TemporaryUpgrade")
				{
					Icon = TemporaryUpgradesManager.UpgradeIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "TemporaryUpgrade", "name", "upgrade"]),
					Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "TemporaryUpgrade", "description", "upgrade"])
				};
			
			public Tooltip DowngradeTooltip
				=> new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::TemporaryUpgrade")
				{
					Icon = TemporaryUpgradesManager.DowngradeIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "TemporaryUpgrade", "name", "downgrade"]),
					Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "TemporaryUpgrade", "description", "downgrade"])
				};
			
			public Tooltip SidegradeTooltip
				=> new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::TemporaryUpgrade")
				{
					Icon = TemporaryUpgradesManager.SidegradeIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "TemporaryUpgrade", "name", "sidegrade"]),
					Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "TemporaryUpgrade", "description", "sidegrade"])
				};

			public Upgrade GetPermanentUpgrade(Card card)
				=> GetPermanentUpgrade(MG.inst.g.state, card);

			public Upgrade GetPermanentUpgrade(State state, Card card)
				=> ModEntry.Instance.Helper.ModData.GetOptionalModData<Upgrade>(card, "NonTemporaryUpgrade") ?? card.upgrade;

			public Upgrade? GetTemporaryUpgrade(Card card)
				=> GetTemporaryUpgrade(MG.inst.g.state, card);

			public Upgrade? GetTemporaryUpgrade(State state, Card card)
				=> ModEntry.Instance.Helper.ModData.ContainsModData(card, "NonTemporaryUpgrade") ? card.upgrade : null;

			public void SetPermanentUpgrade(Card card, Upgrade upgrade)
				=> SetPermanentUpgrade(MG.inst.g.state, card, upgrade);
			
			public void SetPermanentUpgrade(State state, Card card, Upgrade upgrade)
			{
				if (ModEntry.Instance.Helper.ModData.ContainsModData(card, "NonTemporaryUpgrade"))
					ModEntry.Instance.Helper.ModData.SetOptionalModData<Upgrade>(card, "NonTemporaryUpgrade", upgrade);
				else
					card.upgrade = upgrade;
			}

			public void SetTemporaryUpgrade(Card card, Upgrade? upgrade)
				=> SetTemporaryUpgrade(MG.inst.g.state, card, upgrade);

			public void SetTemporaryUpgrade(State state, Card card, Upgrade? upgrade)
			{
				var oldTemporaryUpgrade = GetTemporaryUpgrade(card);
				var oldUpgrade = card.upgrade;
				
				if (upgrade is { } nonNullUpgrade)
				{
					if (!ModEntry.Instance.Helper.ModData.ContainsModData(card, "NonTemporaryUpgrade"))
						ModEntry.Instance.Helper.ModData.SetOptionalModData<Upgrade>(card, "NonTemporaryUpgrade", card.upgrade);
					card.upgrade = nonNullUpgrade;
				}
				else if (ModEntry.Instance.Helper.ModData.TryGetModData<Upgrade>(card, "NonTemporaryUpgrade", out var nonTemporaryUpgrade))
				{
					card.upgrade = nonTemporaryUpgrade;
					ModEntry.Instance.Helper.ModData.RemoveModData(card, "NonTemporaryUpgrade");
				}

				var newTemporaryUpgrade = GetTemporaryUpgrade(card);
				var newUpgrade = card.upgrade;
				
				var args = ModEntry.Instance.ArgsPool.Get<OnTemporaryUpgradeArgs>();
				try
				{
					args.State = state;
					args.Card = card;
					args.OldTemporaryUpgrade = oldTemporaryUpgrade;
					args.NewTemporaryUpgrade = newTemporaryUpgrade;
					args.OldUpgrade = oldUpgrade;
					args.NewUpgrade = newUpgrade;

					foreach (var hook in TemporaryUpgradesManager.Instance.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
						hook.OnTemporaryUpgrade(args);
				}
				finally
				{
					ModEntry.Instance.ArgsPool.Return(args);
				}
			}

			public IKokoroApi.IV2.ITemporaryUpgradesApi.ISetTemporaryUpgradeAction? AsSetTemporaryUpgradeAction(CardAction action)
				=> action as IKokoroApi.IV2.ITemporaryUpgradesApi.ISetTemporaryUpgradeAction;

			public IKokoroApi.IV2.ITemporaryUpgradesApi.ISetTemporaryUpgradeAction MakeSetTemporaryUpgradeAction(int cardId, Upgrade? upgrade)
				=> new TemporaryUpgradesManager.SetTemporaryUpgradeAction { CardId = cardId, Upgrade = upgrade };

			public IKokoroApi.IV2.ITemporaryUpgradesApi.IChooseTemporaryUpgradeAction? AsChooseTemporaryUpgradeAction(CardAction action)
				=> action as IKokoroApi.IV2.ITemporaryUpgradesApi.IChooseTemporaryUpgradeAction;

			public IKokoroApi.IV2.ITemporaryUpgradesApi.IChooseTemporaryUpgradeAction MakeChooseTemporaryUpgradeAction(int cardId)
				=> new TemporaryUpgradesManager.ChooseTemporaryUpgradeAction { CardId = cardId };

			public IKokoroApi.IV2.ITemporaryUpgradesApi.ICardUpgrade ModifyCardUpgrade(CardUpgrade route)
				=> new TemporaryUpgradesManager.CardUpgradeWrapper(Mutil.DeepCopy(route));

			public void RegisterHook(IKokoroApi.IV2.ITemporaryUpgradesApi.IHook hook, double priority = 0)
				=> TemporaryUpgradesManager.Instance.Register(hook, priority);

			public void UnregisterHook(IKokoroApi.IV2.ITemporaryUpgradesApi.IHook hook)
				=> TemporaryUpgradesManager.Instance.Unregister(hook);
			
			internal sealed class OnTemporaryUpgradeArgs : IKokoroApi.IV2.ITemporaryUpgradesApi.IHook.IOnTemporaryUpgradeArgs
			{
				public State State { get; internal set; } = null!;
				public Card Card { get; internal set; } = null!;
				public Upgrade? OldTemporaryUpgrade { get; internal set; }
				public Upgrade? NewTemporaryUpgrade { get; internal set; }
				public Upgrade OldUpgrade { get; internal set; }
				public Upgrade NewUpgrade { get; internal set; }
			}
		}
	}
}

internal sealed class TemporaryUpgradesManager : HookManager<IKokoroApi.IV2.ITemporaryUpgradesApi.IHook>
{
	internal static readonly TemporaryUpgradesManager Instance = new();

	internal static ICardTraitEntry Trait { get; private set; } = null!;
	internal static ISpriteEntry UpgradeIcon { get; private set; } = null!;
	internal static ISpriteEntry DowngradeIcon { get; private set; } = null!;
	internal static ISpriteEntry SidegradeIcon { get; private set; } = null!;

	private TemporaryUpgradesManager() : base(ModEntry.Instance.Package.Manifest.UniqueName)
	{
	}
	
	internal static void Setup(IHarmony harmony)
	{
		UpgradeIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/TemporaryUpgrade.png"));
		DowngradeIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/TemporaryDowngrade.png"));
		SidegradeIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/TemporarySidegrade.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("TemporaryUpgrade", new()
		{
			Icon = (state, card) =>
			{
				if (card is null)
					return UpgradeIcon.Sprite;
				
				var permUpgrade = ModEntry.Instance.Api.V2.TemporaryUpgrades.GetPermanentUpgrade(state, card);
				var tempUpgrade = ModEntry.Instance.Api.V2.TemporaryUpgrades.GetTemporaryUpgrade(state, card);
				
				if (permUpgrade == tempUpgrade)
					return UpgradeIcon.Sprite;
				if (permUpgrade == Upgrade.None)
					return UpgradeIcon.Sprite;
				if (tempUpgrade == Upgrade.None)
					return DowngradeIcon.Sprite;
				return SidegradeIcon.Sprite;
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "TemporaryUpgrade", "name"]).Localize,
			Tooltips = (state, card) =>
			{
				if (card is null)
					return [ModEntry.Instance.Api.V2.TemporaryUpgrades.UpgradeTooltip];
				
				var permUpgrade = ModEntry.Instance.Api.V2.TemporaryUpgrades.GetPermanentUpgrade(state, card);
				var tempUpgrade = ModEntry.Instance.Api.V2.TemporaryUpgrades.GetTemporaryUpgrade(state, card);
				
				if (permUpgrade == tempUpgrade)
					return [ModEntry.Instance.Api.V2.TemporaryUpgrades.UpgradeTooltip];
				if (permUpgrade == Upgrade.None)
					return [ModEntry.Instance.Api.V2.TemporaryUpgrades.UpgradeTooltip];
				if (tempUpgrade == Upgrade.None)
					return [ModEntry.Instance.Api.V2.TemporaryUpgrades.DowngradeTooltip];
				return [ModEntry.Instance.Api.V2.TemporaryUpgrades.SidegradeTooltip];
			}
		});

		ModEntry.Instance.Helper.Content.Cards.OnGetDynamicInnateCardTraitOverrides += (_, e) =>
		{
			if (ModEntry.Instance.Api.V2.TemporaryUpgrades.GetTemporaryUpgrade(e.State, e.Card) is not null)
				e.SetOverride(Trait, true);
		};

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnCombatEnd), (State state) =>
		{
			state.rewardsQueue.Queue(new RemoveTemporaryUpgradesAction());
		});

		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.EndRun)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_EndRun_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardUpgrade), nameof(CardUpgrade.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardUpgrade_Render_Prefix))
		);
	}

	private static void RemoveTemporaryUpgrades(State state)
	{
		IEnumerable<Card> cards = [
			.. state.deck,
			.. (state.route as Combat)?.hand ?? [],
			.. (state.route as Combat)?.discard ?? [],
			.. (state.route as Combat)?.exhausted ?? [],
		];
		
		foreach (var card in cards)
			ModEntry.Instance.Api.V2.TemporaryUpgrades.SetTemporaryUpgrade(state, card, null);
	}

	private static void State_EndRun_Prefix(State __instance)
		=> RemoveTemporaryUpgrades(__instance);
	
	private static void CardUpgrade_Render_Prefix(CardUpgrade __instance, G g)
	{
		if (__instance.upgradeCards is not null)
			return;
		if (!ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(__instance, "IsTemporaryUpgrade"))
			return;
		
		var state = __instance.isCodex ? DB.fakeState : g.state;
		__instance.upgradeCards = __instance.cardCopy.GetMeta().upgradesTo
			.Where(upgrade => state.IsGenerallyAllowedUpgradePath(upgrade))
			.Select(upgrade =>
			{
				var card = Mutil.DeepCopy(__instance.cardCopy);
				ModEntry.Instance.Helper.ModData.SetModData(card, "NonTemporaryUpgrade", card.upgrade);
				card.upgrade = upgrade;
				card.isForeground = false;
				card.hoverAnim = 0;
				return card;
			})
			.ToList();
	}

	private sealed class RemoveTemporaryUpgradesAction : CardAction
	{
		public override List<Tooltip> GetTooltips(State s)
			=> [.. Trait.Configuration.Tooltips?.Invoke(s, null) ?? []];
		
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			RemoveTemporaryUpgrades(s);
		}
	}

	internal sealed class ChooseTemporaryUpgradeAction : CardAction, IKokoroApi.IV2.ITemporaryUpgradesApi.IChooseTemporaryUpgradeAction
	{
		public required int CardId { get; set; }

		[JsonIgnore]
		public CardAction AsCardAction
			=> this;

		public override List<Tooltip> GetTooltips(State s)
			=> [.. Trait.Configuration.Tooltips?.Invoke(s, null) ?? []];

		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			var baseResult = base.BeginWithRoute(g, s, c);
			if (s.FindCard(this.CardId) is not { upgrade: Upgrade.None } card)
			{
				timer = 0;
				return baseResult;
			}

			var route = new CardUpgrade { cardCopy = Mutil.DeepCopy(card) };
			route = ModEntry.Instance.Api.V2.InPlaceCardUpgrade.ModifyCardUpgrade(route).SetIsInPlace(true).AsRoute;
			route = ModEntry.Instance.Api.V2.TemporaryUpgrades.ModifyCardUpgrade(route).SetIsTemporaryUpgrade(true).AsRoute;
			return route;
		}
		
		public IKokoroApi.IV2.ITemporaryUpgradesApi.IChooseTemporaryUpgradeAction SetCardId(int value)
		{
			this.CardId = value;
			return this;
		}
	}

	internal sealed class SetTemporaryUpgradeAction : CardAction, IKokoroApi.IV2.ITemporaryUpgradesApi.ISetTemporaryUpgradeAction
	{
		public required int CardId { get; set; }
		public required Upgrade? Upgrade { get; set; }

		[JsonIgnore]
		public CardAction AsCardAction
			=> this;

		public override List<Tooltip> GetTooltips(State s)
			=> [.. Trait.Configuration.Tooltips?.Invoke(s, null) ?? []];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (s.FindCard(CardId) is not { } card)
			{
				timer = 0;
				return;
			}
			
			ModEntry.Instance.Api.V2.TemporaryUpgrades.SetTemporaryUpgrade(s, card, Upgrade);
		}
		
		public IKokoroApi.IV2.ITemporaryUpgradesApi.ISetTemporaryUpgradeAction SetCardId(int value)
		{
			this.CardId = value;
			return this;
		}

		public IKokoroApi.IV2.ITemporaryUpgradesApi.ISetTemporaryUpgradeAction SetUpgrade(Upgrade? value)
		{
			this.Upgrade = value;
			return this;
		}
	}
	
	internal sealed class CardUpgradeWrapper(CardUpgrade route) : IKokoroApi.IV2.ITemporaryUpgradesApi.ICardUpgrade
	{
		[JsonIgnore]
		public CardUpgrade AsRoute
			=> route;

		public bool IsTemporaryUpgrade
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(route, "IsTemporaryUpgrade");
			set => ModEntry.Instance.Helper.ModData.SetModData(route, "IsTemporaryUpgrade", value);
		}

		public IKokoroApi.IV2.ITemporaryUpgradesApi.ICardUpgrade SetIsTemporaryUpgrade(bool value)
		{
			this.IsTemporaryUpgrade = value;
			return new CardUpgradeWrapper(ModEntry.Instance.Api.V2.InPlaceCardUpgrade.ModifyCardUpgrade(route).SetInPlaceCardUpgradeStrategy(new InPlaceCardUpgradeStrategy()).AsRoute);
		}

		private sealed class InPlaceCardUpgradeStrategy : IKokoroApi.IV2.IInPlaceCardUpgradeApi.IInPlaceCardUpgradeStrategy
		{
			public void ApplyInPlaceCardUpgrade(IKokoroApi.IV2.IInPlaceCardUpgradeApi.IInPlaceCardUpgradeStrategy.IApplyInPlaceCardUpgradeArgs args)
			{
				var upgrade = ModEntry.Instance.Api.V2.TemporaryUpgrades.GetTemporaryUpgrade(args.State, args.TemplateCard);
				ModEntry.Instance.Api.V2.TemporaryUpgrades.SetTemporaryUpgrade(args.State, args.TargetCard, upgrade);
			}
		}
	}
}