using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using FSPRO;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;

namespace Shockah.Destiny;

internal sealed class Enchanted : IRegisterable
{
	internal static ICardTraitEntry EnchantedTrait { get; private set; } = null!;
	
	private static ISpriteEntry[] EnchantedOf2Icons { get; set; } = null!;
	private static ISpriteEntry[] EnchantedOf3Icons { get; set; } = null!;
	private static ISpriteEntry[] EnchantedOf2Art { get; set; } = null!;
	private static ISpriteEntry[] EnchantedOf3Art { get; set; } = null!;
	private static ISpriteEntry[] EnchantedOf2Split1to2Art { get; set; } = null!;
	private static ISpriteEntry[] EnchantedOf2Split2to1Art { get; set; } = null!;

	private static readonly Color PaidGateColor = new("51A7F8");
	private static readonly Color NextGateColor = new("51A7F8");
	private static readonly Color FutureGateColor = new("0A1F53");
	
	private static readonly Dictionary<TransactionWholePaymentResultDictionaryKey, ISpriteEntry> CostOutlineSprites = [];
	private static readonly Dictionary<string, Dictionary<Upgrade, Dictionary<int, IKokoroApi.IV2.IActionCostsApi.ICost>>> EnchantLevelCosts = [];
	private static readonly Dictionary<string, Dictionary<Upgrade, int>> MaxEnchantLevels = [];
	
	private static readonly Pool<OnEnchantArgs> OnEnchantArgsPool = new(() => new());
	private static readonly Pool<AfterEnchantArgs> AfterEnchantArgsPool = new(() => new());

	private static Card? CardRendered;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		EnchantedOf2Icons = Enumerable.Range(0, 2)
			.Select(i => ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/Traits/Enchanted{i + 1}of2.png")))
			.ToArray();
		
		EnchantedOf3Icons = Enumerable.Range(0, 3)
			.Select(i => ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/Traits/Enchanted{i + 1}of3.png")))
			.ToArray();
		
		EnchantedOf2Art = Enumerable.Range(0, 2)
			.Select(i => ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/Cards/Enchanted{i + 1}of2.png")))
			.ToArray();
		
		EnchantedOf3Art = Enumerable.Range(0, 3)
			.Select(i => ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/Cards/Enchanted{i + 1}of3.png")))
			.ToArray();
		
		EnchantedOf2Split1to2Art = Enumerable.Range(0, 2)
			.Select(i => ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/Cards/Enchanted{i + 1}of2Split1to2.png")))
			.ToArray();
		
		EnchantedOf2Split2to1Art = Enumerable.Range(0, 2)
			.Select(i => ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/Cards/Enchanted{i + 1}of2Split2to1.png")))
			.ToArray();
		
		EnchantedTrait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Enchanted", new()
		{
			Icon = (_, card) => GetIcon(card),
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "Enchanted", "name"]).Localize,
			Tooltips = (state, card) => [
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Enchanted")
				{
					Icon = GetIcon(card),
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "Enchanted", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(
						["cardTrait", "Enchanted", "description", PlatformIcons.GetPlatform() == Platform.MouseKeyboard ? "m&k" : "controller"], 
						new
						{
							Button = PlatformIcons.GetPlatform() switch
							{
								Platform.NX => Loc.T("controller.nx.b"),
								Platform.PS => Loc.T("controller.ps.circle"),
								_ => Loc.T("controller.xbox.b"),
							},
						}
					),
				},
				.. StatusMeta.GetTooltips(Status.shard, state.ship.GetMaxShard()),
			]
		});

		ModEntry.Instance.Helper.Content.Cards.OnGetDynamicInnateCardTraitOverrides += (_, e) =>
		{
			if (GetMaxEnchantLevel(e.Card.Key(), e.Card.upgrade) > 0)
				e.SetOverride(EnchantedTrait, true);
		};
		
		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnCombatEnd), (State state) =>
		{
			IEnumerable<Card> cards = [
				.. state.deck,
				.. (state.route as Combat)?.hand ?? [],
				.. (state.route as Combat)?.discard ?? [],
				.. (state.route as Combat)?.exhausted ?? [],
			];

			foreach (var card in cards)
			{
				SetEnchantLevel(card, 0);
				ClearEnchantLevelPayments(card);
			}
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.MakeAllActionIcons)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_MakeAllActionIcons_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_MakeAllActionIcons_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.OnMouseDownRight)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_OnMouseDownRight_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.OnInputPhase)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_OnInputPhase_Prefix))
		);

		Spr GetIcon(Card? card)
		{
			var maxEnchantLevel = card is null ? 2 : GetMaxEnchantLevel(card.Key(), card.upgrade);
			var enchantLevel = Math.Clamp(card is null ? 0 : GetEnchantLevel(card), 0, maxEnchantLevel);
			return maxEnchantLevel == 2 ? EnchantedOf3Icons[enchantLevel].Sprite : EnchantedOf2Icons[enchantLevel].Sprite;
		}
	}

	internal static Spr? GetCardArt(Card? card, Spr? defaultArt = null, int[]? split = null)
	{
		var maxEnchantLevel = card is null ? 0 : GetMaxEnchantLevel(card.Key(), card.upgrade);
		if (maxEnchantLevel is <= 0 or > 2)
			return defaultArt;

		var sprites = maxEnchantLevel switch
		{
			1 => split switch
			{
				[1, 2] => EnchantedOf2Split1to2Art,
				[2, 1] => EnchantedOf2Split2to1Art,
				_ => EnchantedOf2Art,
			},
			2 => EnchantedOf3Art,
			_ => throw new ArgumentOutOfRangeException()
		};
		
		var enchantLevel = Math.Clamp(card is null ? 0 : GetEnchantLevel(card), 0, maxEnchantLevel);
		return sprites[Math.Min(enchantLevel, sprites.Length - 1)].Sprite;
	}

	internal static int GetMaxEnchantLevel(string cardKey, Upgrade upgrade)
	{
		if (!MaxEnchantLevels.TryGetValue(cardKey, out var specificCardMaxEnchantLevels))
			return 0;
		return specificCardMaxEnchantLevels.GetValueOrDefault(upgrade);
	}

	private static void UpdateMaxEnchantLevel(string cardKey, Upgrade upgrade, int? maxLevel = null)
	{
		var actualMaxLevel = maxLevel ?? EnchantLevelCosts.GetValueOrDefault(cardKey)?.GetValueOrDefault(upgrade)?.Keys.Append(0).Max() ?? 0;

		if (actualMaxLevel <= 0)
		{
			if (!MaxEnchantLevels.TryGetValue(cardKey, out var specificCardMaxEnchantLevels))
				return;

			specificCardMaxEnchantLevels.Remove(upgrade);
			if (specificCardMaxEnchantLevels.Count != 0)
				return;

			MaxEnchantLevels.Remove(cardKey);
		}
		else
		{
			ref var specificCardMaxEnchantLevels = ref CollectionsMarshal.GetValueRefOrAddDefault(MaxEnchantLevels, cardKey, out var specificCardMaxEnchantLevelsExists);
			if (!specificCardMaxEnchantLevelsExists)
				specificCardMaxEnchantLevels = [];
			specificCardMaxEnchantLevels![upgrade] = actualMaxLevel;
		}
	}

	internal static int GetEnchantLevel(Card card)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(card, "EnchantLevel");

	internal static void SetEnchantLevel(Card card, int level)
	{
		if (level <= 0)
			ModEntry.Instance.Helper.ModData.RemoveModData(card, "EnchantLevel");
		else
			ModEntry.Instance.Helper.ModData.SetModData(card, "EnchantLevel", level);
	}

	internal static IKokoroApi.IV2.IActionCostsApi.ICost? GetNextEnchantLevelCost(Card card)
		=> GetEnchantLevelCost(card.Key(), card.upgrade, GetEnchantLevel(card) + 1);

	internal static IKokoroApi.IV2.IActionCostsApi.ICost? GetEnchantLevelCost(string cardKey, Upgrade upgrade, int level)
	{
		if (!EnchantLevelCosts.TryGetValue(cardKey, out var specificCardEnchantLevelCosts))
			return null;
		if (!specificCardEnchantLevelCosts.TryGetValue(upgrade, out var specificUpgradeEnchantLevelCosts))
			return null;
		return specificUpgradeEnchantLevelCosts.GetValueOrDefault(level);
	}

	internal static void SetEnchantLevelCost(string cardKey, int level, IKokoroApi.IV2.IActionCostsApi.ICost? cost)
	{
		SetEnchantLevelCost(cardKey, Upgrade.None, level, cost);
		SetEnchantLevelCost(cardKey, Upgrade.A, level, cost);
		SetEnchantLevelCost(cardKey, Upgrade.B, level, cost);
	}

	internal static void SetEnchantLevelCost(string cardKey, Upgrade upgrade, int level, IKokoroApi.IV2.IActionCostsApi.ICost? cost)
	{
		if (cost is null)
		{
			try
			{
				if (!EnchantLevelCosts.TryGetValue(cardKey, out var specificCardEnchantLevelCosts))
					return;
				if (!specificCardEnchantLevelCosts.TryGetValue(upgrade, out var specificUpgradeEnchantLevelCosts))
					return;

				specificUpgradeEnchantLevelCosts.Remove(level);
				if (specificUpgradeEnchantLevelCosts.Count != 0)
					return;

				specificCardEnchantLevelCosts.Remove(upgrade);
				if (specificCardEnchantLevelCosts.Count != 0)
					return;
			
				EnchantLevelCosts.Remove(cardKey);
			}
			finally
			{
				UpdateMaxEnchantLevel(cardKey, upgrade);
			}
		}
		else
		{
			ref var specificCardEnchantLevelCosts = ref CollectionsMarshal.GetValueRefOrAddDefault(EnchantLevelCosts, cardKey, out var specificCardEnchantLevelCostsExists);
			if (!specificCardEnchantLevelCostsExists)
				specificCardEnchantLevelCosts = [];

			ref var specificUpgradeEnchantLevelCosts = ref CollectionsMarshal.GetValueRefOrAddDefault(specificCardEnchantLevelCosts!, upgrade, out var specificUpgradeEnchantLevelCostsExists);
			if (!specificUpgradeEnchantLevelCostsExists)
				specificUpgradeEnchantLevelCosts = [];
			specificUpgradeEnchantLevelCosts![level] = cost;
			UpdateMaxEnchantLevel(cardKey, upgrade, Math.Max(GetMaxEnchantLevel(cardKey, upgrade), level));
		}
	}

	internal static IReadOnlyList<IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult>? GetEnchantLevelPayment(Card card, int level)
	{
		if (level < 0)
			return null;
		if (!ModEntry.Instance.Helper.ModData.TryGetModData<Dictionary<int, List<IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult>>>(card, "EnchantLevelPayments", out var enchantLevelPayments))
			return null;
		return enchantLevelPayments.GetValueOrDefault(level);
	}

	internal static void SetEnchantLevelPayment(Card card, int level, IReadOnlyList<IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult> payment)
	{
		if (level < 0)
			return;
		if (level > GetMaxEnchantLevel(card.Key(), card.upgrade))
			return;
		
		var enchantLevelPayments = ModEntry.Instance.Helper.ModData.ObtainModData<Dictionary<int, List<IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult>>>(card, "EnchantLevelPayments");
		enchantLevelPayments[level] = payment.ToList();
	}

	internal static void ClearEnchantLevelPayments(Card card)
		=> ModEntry.Instance.Helper.ModData.RemoveModData(card, "EnchantLevelPayments");

	internal static bool TryEnchant(State state, Card card, bool fromUserInteraction = true)
	{
		if (state.route is not Combat combat)
			return false;
		
		var maxEnchantLevel = GetMaxEnchantLevel(card.Key(), card.upgrade);
		var enchantLevel = Math.Clamp(GetEnchantLevel(card), 0, maxEnchantLevel);

		var handled = OnEnchantArgsPool.Do(args =>
		{
			args.State = state;
			args.Combat = combat;
			args.Card = card;
			args.FromUserInteraction = fromUserInteraction;
			args.EnchantLevel = enchantLevel;
			args.MaxEnchantLevel = maxEnchantLevel;

			foreach (var hook in ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
			{
				var result = hook.OnEnchant(args);
				if (result is not null)
					return result.Value;
			}
			return false;
		});

		if (handled)
		{
			// forcing `Artifact.OnQueueEmptyDuringPlayerTurn` to be called
			combat.QueueImmediate(new ADummyAction());
			return true;
		}
		
		if (enchantLevel >= maxEnchantLevel)
			return false;
		if (fromUserInteraction && state.CharacterIsMissing(card.GetMeta().deck))
			return false;
		if (GetNextEnchantLevelCost(card) is not { } actionCost)
			return false;
		
		var modifiedActionCost = ModEntry.Instance.KokoroApi.ActionCosts.ModifyActionCost(Mutil.DeepCopy(actionCost), state, state.route as Combat ?? DB.fakeCombat, card, null);
		var environment = ModEntry.Instance.KokoroApi.ActionCosts.MakeStatePaymentEnvironment(state, combat, card);
		var transaction = ModEntry.Instance.KokoroApi.ActionCosts.GetBestTransaction(modifiedActionCost, environment);
		var transactionPaymentResult = transaction.TestPayment(environment);

		if (transactionPaymentResult.UnpaidResources.Count != 0)
		{
			if (fromUserInteraction)
			{
				card.shakeNoAnim = 1.0;
				Audio.Play(Event.ZeroEnergy);
			}
			return false;
		}

		transactionPaymentResult = transaction.Pay(environment);
		SetEnchantLevel(card, enchantLevel + 1);
		SetEnchantLevelPayment(card, enchantLevel + 1, transactionPaymentResult.Payments);
		
		foreach (var action in card.GetActionsOverridden(state, combat))
			if (action is IDestinyApi.IImbueAction imbueAction && imbueAction.Level == enchantLevel + 1)
				imbueAction.ImbueCard(state, card);

		if (fromUserInteraction)
		{
			card.flipAnim = 1;
			Audio.Play(Event.Status_PowerUp);
		}
		
		AfterEnchantArgsPool.Do(args =>
		{
			args.State = state;
			args.Combat = combat;
			args.Card = card;
			args.FromUserInteraction = fromUserInteraction;
			args.EnchantLevel = enchantLevel;
			args.MaxEnchantLevel = maxEnchantLevel;

			foreach (var hook in ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
				hook.AfterEnchant(args);
		});
		
		// forcing `Artifact.OnQueueEmptyDuringPlayerTurn` to be called
		combat.QueueImmediate([
			new ADelayNonSkippable(),
			new ADummyAction(),
		]);
		return true;
	}

	private static int RenderGate(G g, State state, Card card, EnchantGateAction action, bool dontDraw)
	{
		if (GetEnchantLevelCost(card.Key(), card.upgrade, action.Level) is not { } actionCost)
			return 0;
		var modifiedActionCost = ModEntry.Instance.KokoroApi.ActionCosts.ModifyActionCost(Mutil.DeepCopy(actionCost), state, state.route as Combat ?? DB.fakeCombat, card, action);
		
		var enchantLevel = GetEnchantLevel(card);
		var gateColor = (action.Level - enchantLevel) switch
		{
			1 => NextGateColor,
			<= 0 => PaidGateColor,
			_ => FutureGateColor,
		};

		const int iconLeftMargin = 2;
		
		var position = g.Push(rect: new(x: iconLeftMargin)).rect.xy;
		var initialX = (int)position.x;
		
		// if (!dontDraw)
		// 	Draw.Rect(position.x - iconLeftMargin, position.y + 4, 53, 1, gateColor);
		
		var environment = ModEntry.Instance.KokoroApi.ActionCosts.MakeMockPaymentEnvironment(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatePaymentEnvironment(state, state.route as Combat ?? DB.fakeCombat, card));

		var previousUnpaidEnchantLevelCosts = EnchantLevelCosts.GetValueOrDefault(card.Key())?.GetValueOrDefault(card.upgrade)
			?.OrderBy(kvp => kvp.Key)
			.Where(kvp => kvp.Key < action.Level && kvp.Key > enchantLevel) ?? [];

		foreach (var (_, previousUnpaidEnchantLevelCost) in previousUnpaidEnchantLevelCosts)
		{
			var modifiedUnpaidActionCost = ModEntry.Instance.KokoroApi.ActionCosts.ModifyActionCost(Mutil.DeepCopy(previousUnpaidEnchantLevelCost), state, state.route as Combat ?? DB.fakeCombat, card, action);
			var transactionBefore = ModEntry.Instance.KokoroApi.ActionCosts.GetBestTransaction(modifiedUnpaidActionCost, environment);
			transactionBefore.Pay(environment);
		}

		if (enchantLevel < action.Level)
		{
			var transaction = ModEntry.Instance.KokoroApi.ActionCosts.GetBestTransaction(modifiedActionCost, environment);
			var transactionPaymentResult = transaction.TestPayment(environment);
			var transactionPaymentResultKey = TransactionWholePaymentResultDictionaryKey.From(transactionPaymentResult);
			
			if (!dontDraw && CostOutlineSprites.TryGetValue(transactionPaymentResultKey, out var costOutlineSprite))
				Draw.Sprite(costOutlineSprite.Sprite, position.x - 1, position.y - 1, color: gateColor);
			
			modifiedActionCost.Render(g, ref position, false, dontDraw, transactionPaymentResult);
			var costWidth = (int)position.x - initialX;

			if (!CostOutlineSprites.ContainsKey(transactionPaymentResultKey))
			{
				var baseTexture = TextureUtils.CreateTexture(new(costWidth, 10)
				{
					Actions = () =>
					{
						var position = Vec.Zero;
						modifiedActionCost.Render(g, ref position, false, false, transactionPaymentResult);
					},
				});
				CostOutlineSprites[transactionPaymentResultKey] = TextureOutlines.CreateOutlineSprite(baseTexture, true, true, true);
			}
		}
		
		g.Pop();
		return 53;
	}

	private static void Card_MakeAllActionIcons_Prefix(Card __instance)
		=> CardRendered = __instance;

	private static void Card_MakeAllActionIcons_Finalizer()
		=> CardRendered = null;

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is EnchantedAction enchantedAction)
		{
			var oldDisabled = enchantedAction.Action.disabled;
			var renderAsDisabled = enchantedAction.disabled || (state != DB.fakeState && state.FindCard(enchantedAction.CardId) is { } card2 && GetEnchantLevel(card2) < enchantedAction.Level);
			enchantedAction.Action.disabled = renderAsDisabled;
			
			__result = Card.RenderAction(g, state, enchantedAction.Action, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
			enchantedAction.Action.disabled = oldDisabled;
			return false;
		}
		
		if (action is EnchantGateAction gateAction)
		{
			if (CardRendered is null)
				return true;
			
			__result += RenderGate(g, state, CardRendered, gateAction, dontDraw);
			return false;
		}

		return true;
	}

	private static void Combat_OnMouseDownRight_Postfix(Combat __instance, G g, Box b)
	{
		if (__instance.TryGetHandCardFromBox(b) is not { } card)
			return;
		TryEnchant(g.state, card);
	}

	private static void Combat_OnInputPhase_Prefix(Combat __instance, G g, Box b)
	{
		if (b.key != Input.currentGpKey)
			return;
		if (__instance.TryGetHandCardFromBox(b) is not { } card)
			return;
		
		var maxEnchantLevel = GetMaxEnchantLevel(card.Key(), card.upgrade);
		var enchantLevel = Math.Clamp(GetEnchantLevel(card), 0, maxEnchantLevel);
		if (enchantLevel >= maxEnchantLevel)
			return;
		
		if (!Input.GetGpDown(Btn.B))
			return;

		TryEnchant(g.state, card);
	}

	private sealed class TransactionWholePaymentResultDictionaryKey
	{
		public required List<TransactionPaymentResultDictionaryKey> Payments { get; init; }

		public override bool Equals(object? obj)
			=> obj is TransactionWholePaymentResultDictionaryKey key && Payments.SequenceEqual(key.Payments);

		public override int GetHashCode()
		{
			var result = 0;
			foreach (var payment in Payments)
				result = result * 31 + payment.GetHashCode();
			return result;
		}
	
		public static TransactionWholePaymentResultDictionaryKey From(IKokoroApi.IV2.IActionCostsApi.IWholeTransactionPaymentResult result)
			=> new() { Payments = result.Payments.Select(TransactionPaymentResultDictionaryKey.From).ToList() };
	}

	private sealed class TransactionPaymentResultDictionaryKey
	{
		public required string ResourceKey { get; init; }
		public required int Paid { get; init; }
		public required int Unpaid { get; init; }

		public override bool Equals(object? obj)
			=> obj is TransactionPaymentResultDictionaryKey key && Equals(ResourceKey, key.ResourceKey) && Paid == key.Paid && Unpaid == key.Unpaid;

		public override int GetHashCode()
			=> HashCode.Combine(ResourceKey, Paid, Unpaid);
	
		public static TransactionPaymentResultDictionaryKey From(IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult result)
			=> new() { ResourceKey = result.Payment.Resource.ResourceKey, Paid = result.Paid, Unpaid = result.Unpaid };
	}

	private sealed class OnEnchantArgs : IDestinyApi.IHook.IOnEnchantArgs
	{
		public State State { get; set; } = null!;
		public Combat Combat { get; set; } = null!;
		public Card Card { get; set; } = null!;
		public bool FromUserInteraction { get; set; }
		public int EnchantLevel { get; set; }
		public int MaxEnchantLevel { get; set; }
	}

	private sealed class AfterEnchantArgs : IDestinyApi.IHook.IAfterEnchantArgs
	{
		public State State { get; set; } = null!;
		public Combat Combat { get; set; } = null!;
		public Card Card { get; set; } = null!;
		public bool FromUserInteraction { get; set; }
		public int EnchantLevel { get; set; }
		public int MaxEnchantLevel { get; set; }
	}
}

internal sealed class EnchantGateAction : CardAction, IDestinyApi.IEnchantGateAction
{
	public required int Level { get; set; }

	public CardAction AsCardAction
		=> this;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;
	}
	
	public IDestinyApi.IEnchantGateAction SetLevel(int value)
	{
		this.Level = value;
		return this;
	}
}

internal sealed class EnchantedAction : CardAction, IDestinyApi.IEnchantedAction
{
	public required int CardId { get; set; }
	public required int Level { get; set; }
	public required CardAction Action { get; set; }

	public CardAction AsCardAction
		=> this;

	public override List<Tooltip> GetTooltips(State s)
		=> Action.GetTooltips(s);

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		if (s.FindCard(CardId) is not { } card)
			return;
		if (Enchanted.GetEnchantLevel(card) < Level)
			return;
		c.QueueImmediate(Action);
	}
	
	public IDestinyApi.IEnchantedAction SetCardId(int value)
	{
		this.CardId = value;
		return this;
	}

	public IDestinyApi.IEnchantedAction SetLevel(int value)
	{
		this.Level = value;
		return this;
	}

	public IDestinyApi.IEnchantedAction SetAction(CardAction value)
	{
		this.Action = value;
		return this;
	}
}