using HarmonyLib;
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
	private static readonly Dictionary<int, Spr> Icons = [];

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Trait = helper.Content.Cards.RegisterTrait("Limited", new()
		{
			Icon = (state, card) => ObtainIcon(card?.GetLimitedUses() ?? 10),
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "Limited", "name"]).Localize,
			Tooltips = (state, card) =>
			{
				string description;
				if (card is null)
					description = ModEntry.Instance.Localizations.Localize(["cardTrait", "Limited", "description", "withoutCard"]);
				else if (state.route is Combat)
					description = ModEntry.Instance.Localizations.Localize(["cardTrait", "Limited", "description", "stateful"], new { Count = card.GetLimitedUses() });
				else
					description = ModEntry.Instance.Localizations.Localize(["cardTrait", "Limited", "description", "outOfCombat"], new { Count = GetDefaultLimitedUses(card.Key(), card.upgrade) });

				return [
					new GlossaryTooltip($"cardtrait.{MethodBase.GetCurrentMethod()!.DeclaringType!.Namespace!}::Limited")
					{
						Icon = ObtainIcon(card?.GetLimitedUses() ?? 10),
						TitleColor = Colors.cardtrait,
						Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "Limited", "name"]),
						Description = description,
					},
					new TTGlossary("cardtrait.exhaust"),
				];
			}
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

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
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
		if (Icons.TryGetValue(amount, out var icon))
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

		Icons[amount] = icon;
		return icon;
	}

	private static bool Card_RenderAction_Prefix(G g, CardAction action, bool dontDraw, ref int __result)
	{
		if (action is not ChangeLimitedUsesAction usesAction)
			return true;

		var position = g.Push(rect: new()).rect.xy;
		int initialX = (int)position.x;

		if (!dontDraw)
			Draw.Sprite(ObtainIcon(10), position.x, position.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
		position.x += 10;

		if (usesAction.Mode == AStatusMode.Set)
		{
			position.x += 1;
			if (!dontDraw)
				Draw.Text("=", position.x, position.y, dontSubstituteLocFont: true);
			position.x += 6;
		}

		if (!dontDraw)
			BigNumbers.Render(usesAction.Amount, position.x, position.y, usesAction.disabled ? Colors.disabledText : Colors.textMain);
		position.x += usesAction.Amount.ToString().Length * 6;

		__result = (int)position.x - initialX;
		g.Pop();

		return false;
	}
}

internal sealed class LimitedUsesVariableHint : AVariableHint
{
	public required int CardId;

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
				Description = ModEntry.Instance.Localizations.Localize(["x", "LimitedUses", s.route is Combat ? "stateful" : "stateless"], new { Count = s.FindCard(CardId)?.GetLimitedUses() ?? 0 })
			}
		];
}

internal sealed class ChangeLimitedUsesAction : CardAction
{
	public required int CardId;
	public required int Amount;
	public AStatusMode Mode;

	public override Icon? GetIcon(State s)
		=> new(Limited.ObtainIcon(10), Amount, Colors.textMain);

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip($"action.{GetType().Namespace!}::ChangeLimitedUses::{Mode}")
			{
				Icon = Limited.ObtainIcon(10),
				TitleColor = Colors.action,
				Title = Mode switch
				{
					AStatusMode.Add => ModEntry.Instance.Localizations.Localize(["action", "ChangeLimitedUses", "Add", "name", Amount >= 0 ? "positive" : "negative"]),
					_ => ModEntry.Instance.Localizations.Localize(["action", "ChangeLimitedUses", Mode.ToString(), "name"])
				},
				Description = Mode switch
				{
					AStatusMode.Add => ModEntry.Instance.Localizations.Localize(["action", "ChangeLimitedUses", "Add", "description", Amount >= 0 ? "positive" : "negative"], new { Amount = Math.Abs(Amount) }),
					_ => ModEntry.Instance.Localizations.Localize(["action", "ChangeLimitedUses", Mode.ToString(), "description"], new { Amount = Amount })
				}
			}
		];

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);

		if (s.FindCard(CardId) is not { } card)
		{
			timer = 0;
			return;
		}

		var currentAmount = card.GetLimitedUses();
		var newAmount = Mode switch
		{
			AStatusMode.Add => currentAmount + Amount,
			AStatusMode.Mult => currentAmount * Amount,
			_ => Amount,
		};

		card.SetLimitedUses(newAmount);
		if (newAmount <= 0 && !c.exhausted.Contains(card))
		{
			s.RemoveCardFromWhereverItIs(CardId);
			c.SendCardToExhaust(s, card);
		}
	}
}