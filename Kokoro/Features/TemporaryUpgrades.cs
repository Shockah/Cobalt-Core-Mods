using HarmonyLib;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	public IKokoroApi.ITemporaryUpgradesApi TemporaryUpgrades { get; } = new TemporaryUpgradesApi();
	
	partial class TemporaryUpgradesApi
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
			=> ModEntry.Instance.Helper.ModData.GetOptionalModData<Upgrade>(card, "NonTemporaryUpgrade") ?? card.upgrade;

		public Upgrade? GetTemporaryUpgrade(Card card)
			=> ModEntry.Instance.Helper.ModData.ContainsModData(card, "NonTemporaryUpgrade") ? card.upgrade : null;
		
		public void SetPermanentUpgrade(Card card, Upgrade upgrade)
		{
			if (ModEntry.Instance.Helper.ModData.ContainsModData(card, "NonTemporaryUpgrade"))
				ModEntry.Instance.Helper.ModData.SetOptionalModData<Upgrade>(card, "NonTemporaryUpgrade", upgrade);
			else
				card.upgrade = upgrade;
		}

		public void SetTemporaryUpgrade(Card card, Upgrade? upgrade)
		{
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
		}

		public CardAction MakeSetTemporaryUpgradeAction(int cardId, Upgrade? upgrade)
			=> new TemporaryUpgradesManager.SetTemporaryUpgradeAction { CardId = cardId, Upgrade = upgrade };

		public CardAction MakeChooseTemporaryUpgradeAction(int cardId)
			=> new TemporaryUpgradesManager.ChooseTemporaryUpgradeAction { CardId = cardId };
	}
}

internal sealed class TemporaryUpgradesManager
{
	internal static ICardTraitEntry Trait { get; private set; } = null!;
	internal static ISpriteEntry UpgradeIcon { get; private set; } = null!;
	internal static ISpriteEntry DowngradeIcon { get; private set; } = null!;
	internal static ISpriteEntry SidegradeIcon { get; private set; } = null!;
	
	internal static void Setup(IHarmony harmony)
	{
		UpgradeIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/TemporaryUpgrade.png"));
		DowngradeIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/TemporaryDowngrade.png"));
		SidegradeIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/TemporarySidegrade.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("TemporaryUpgrade", new()
		{
			Icon = (_, card) =>
			{
				if (card is null)
					return UpgradeIcon.Sprite;
				
				var permUpgrade = ModEntry.Instance.Api.TemporaryUpgrades.GetPermanentUpgrade(card);
				var tempUpgrade = ModEntry.Instance.Api.TemporaryUpgrades.GetTemporaryUpgrade(card);
				
				if (permUpgrade == tempUpgrade)
					return UpgradeIcon.Sprite;
				if (permUpgrade == Upgrade.None)
					return UpgradeIcon.Sprite;
				if (tempUpgrade == Upgrade.None)
					return DowngradeIcon.Sprite;
				return SidegradeIcon.Sprite;
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "TemporaryUpgrade", "name"]).Localize,
			Tooltips = (_, card) =>
			{
				if (card is null)
					return [ModEntry.Instance.Api.TemporaryUpgrades.UpgradeTooltip];
				
				var permUpgrade = ModEntry.Instance.Api.TemporaryUpgrades.GetPermanentUpgrade(card);
				var tempUpgrade = ModEntry.Instance.Api.TemporaryUpgrades.GetTemporaryUpgrade(card);
				
				if (permUpgrade == tempUpgrade)
					return [ModEntry.Instance.Api.TemporaryUpgrades.UpgradeTooltip];
				if (permUpgrade == Upgrade.None)
					return [ModEntry.Instance.Api.TemporaryUpgrades.UpgradeTooltip];
				if (tempUpgrade == Upgrade.None)
					return [ModEntry.Instance.Api.TemporaryUpgrades.DowngradeTooltip];
				return [ModEntry.Instance.Api.TemporaryUpgrades.SidegradeTooltip];
			}
		});

		ModEntry.Instance.Helper.Content.Cards.OnGetDynamicInnateCardTraitOverrides += (_, e) =>
		{
			if (ModEntry.Instance.Api.TemporaryUpgrades.GetTemporaryUpgrade(e.Card) is not null)
				e.SetOverride(Trait, true);
		};

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnCombatEnd), (State state) =>
		{
			state.rewardsQueue.Queue(new RemoveTemporaryUpgradesAction());
		}, 0);

		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.EndRun)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_EndRun_Prefix))
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
			ModEntry.Instance.Api.TemporaryUpgrades.SetTemporaryUpgrade(card, null);
	}

	private static void State_EndRun_Prefix(State __instance)
		=> RemoveTemporaryUpgrades(__instance);

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

	internal sealed class ChooseTemporaryUpgradeAction : CardAction
	{
		public int CardId;

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
			
			ModEntry.Instance.Api.TemporaryUpgrades.SetTemporaryUpgrade(card, card.upgrade);
			return ModEntry.Instance.Api.Actions.MakeCustomCardUpgrade(new CardUpgrade { cardCopy = Mutil.DeepCopy(card) }).SetInPlace(true).AsRoute;
		}
	}

	internal sealed class SetTemporaryUpgradeAction : CardAction
	{
		public int CardId;
		public Upgrade? Upgrade;

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
			
			ModEntry.Instance.Api.TemporaryUpgrades.SetTemporaryUpgrade(card, Upgrade);
		}
	}
}