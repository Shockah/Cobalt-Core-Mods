using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal static class LimitedExt
{
	public static void ResetLimitedUses(this Card card)
		=> ModEntry.Instance.Helper.ModData.RemoveModData(card, "LimitedUses");

	public static int GetLimitedUses(this Card card)
		=> ModEntry.Instance.Helper.ModData.TryGetModData(card, "LimitedUses", out int value) ? value : Limited.GetDefaultLimitedUses(card.Key(), card.upgrade);

	public static void SetLimitedUses(this Card card, int value)
		=> ModEntry.Instance.Helper.ModData.SetModData(card, "LimitedUses", Math.Max(value, 1));
}

internal sealed class Limited : IRegisterable
{
	internal static ICardTraitEntry Trait = null!;

	private static readonly Dictionary<string, Dictionary<Upgrade, int>> DefaultLimitedUses = [];
	private static readonly Dictionary<int, Spr> LimitedIcons = [];

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Trait = helper.Content.Cards.RegisterTrait("Limited", new()
		{
			Icon = (state, card) => ObtainIcon(card?.GetLimitedUses() ?? 2),
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "Limited", "name"]).Localize,
			Tooltips = (state, card) => [
				new GlossaryTooltip($"cardtrait.{MethodBase.GetCurrentMethod()!.DeclaringType!.Namespace!}::Limited")
				{
					Icon = ObtainIcon(card?.GetLimitedUses() ?? 2),
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "Limited", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "Limited", "description", state.route is Combat ? "stateful" : "stateless"], new { Count = card?.GetLimitedUses() ?? 2 }),
				}
			]
		});

		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnCombatEnd), (State state) =>
		{
			foreach (var card in state.deck)
				card.ResetLimitedUses();
		}, 0);

		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card, State state) =>
		{
			if (!helper.Content.Cards.IsCardTraitActive(state, card, Trait))
				return;
			card.SetLimitedUses(card.GetLimitedUses() - 1);
		}, 0);

		helper.Content.Cards.OnGetFinalDynamicCardTraitOverrides += OnGetFinalDynamicCardTraitOverrides;
	}

	[EventPriority(double.MaxValue)]
	private static void OnGetFinalDynamicCardTraitOverrides(object? sender, GetFinalDynamicCardTraitOverridesEventArgs args)
	{
		if (!args.TraitStates[Trait].IsActive)
			return;
		if (args.Card.GetLimitedUses() > 1)
			return;
		args.SetOverride(ModEntry.Instance.Helper.Content.Cards.ExhaustCardTrait, true);
	}

	public static int GetDefaultLimitedUses(string key, Upgrade upgrade)
	{
		if (!DefaultLimitedUses.TryGetValue(key, out var perUpgrade))
			return 2;
		if (!perUpgrade.TryGetValue(upgrade, out var value))
			return 2;
		return value;
	}

	public static void SetDefaultLimitedUses(string key, int value)
	{
		SetDefaultLimitedUses(key, Upgrade.None, value);
		SetDefaultLimitedUses(key, Upgrade.A, value);
		SetDefaultLimitedUses(key, Upgrade.B, value);
	}

	public static void SetDefaultLimitedUses(string key, Upgrade upgrade, int value)
	{
		if (!DefaultLimitedUses.TryGetValue(key, out var perUpgrade))
		{
			perUpgrade = [];
			DefaultLimitedUses[key] = perUpgrade;
		}
		perUpgrade[upgrade] = value;
	}

	internal static Spr ObtainIcon(int amount)
	{
		amount = Math.Clamp(amount, 0, 10);
		if (LimitedIcons.TryGetValue(amount, out var icon))
			return icon;

		icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite($"Limited{amount}", () =>
		{
			var exhaustIcon = SpriteLoader.Get(StableSpr.icons_exhaust)!;
			return TextureUtils.CreateTexture(exhaustIcon.Width, exhaustIcon.Height, () =>
			{
				Draw.Sprite(exhaustIcon, 0, 0);

				var text = amount > 9 ? "+" : amount.ToString();
				var textRect = Draw.Text(text, 0, 0, outline: Colors.black, dontDraw: true, dontSubstituteLocFont: true);
				Draw.Text(text, exhaustIcon.Width - textRect.w, exhaustIcon.Height - textRect.h - 1, color: Colors.white, outline: Colors.black, dontSubstituteLocFont: true);
			});
		}).Sprite;

		LimitedIcons[amount] = icon;
		return icon;
	}
}

internal sealed class LimitedUsesVariableHint : AVariableHint
{
	public LimitedUsesVariableHint()
	{
		this.hand = true;
	}

	public override Icon? GetIcon(State s)
		=> new() { path = Limited.ObtainIcon(10) };

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip("action.xHintLimitedUses.desc")
			{
				Description = ModEntry.Instance.Localizations.Localize(["x", "LimitedUses"])
			}
		];
}