using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FSPRO;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;
using MGColor = Microsoft.Xna.Framework.Color;

namespace Shockah.Destiny;

internal sealed class EnchantedManager : IRegisterable
{
	internal static ISpriteEntry[] EnchantedOf2Icons { get; private set; } = null!;
	internal static ISpriteEntry[] EnchantedOf3Icons { get; private set; } = null!;
	internal static ICardTraitEntry EnchantedTrait { get; private set; } = null!;

	private static readonly Color PaidGateColor = new("122537");
	private static readonly Color NextGateColor = new("51A7F8");
	private static readonly Color FutureGateColor = new("0A1F53");
	
	private static readonly (int X, int Y, int PerpendicularX1, int PerpendicularY1, int PerpendicularX2, int PerpendicularY2)[] VectorNeighbors = [
		(1, 0, 0, -1, 0, 1),
		(-1, 0, 0, -1, 0, 1),
		(0, 1, -1, 0, 1, 0),
		(0, -1, -1, 0, 1, 0),
	];
	private static readonly (int X, int Y)[] VectorCorners = [(1, 1), (1, -1), (-1, -1), (-1, 1)];
	
	private static readonly Dictionary<TransactionWholePaymentResultDictionaryKey, ISpriteEntry> CostOutlineSprites = [];

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		EnchantedOf2Icons = Enumerable.Range(0, 2)
			.Select(i => ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/Traits/Enchanted{i + 1}of2.png")))
			.ToArray();
		
		EnchantedOf3Icons = Enumerable.Range(0, 3)
			.Select(i => ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/Traits/Enchanted{i + 1}of3.png")))
			.ToArray();
		
		EnchantedTrait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Enchanted", new()
		{
			Icon = (_, card) => GetIcon(card),
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "Enchanted"]).Localize,
			Tooltips = (_, card) => [
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Enchanted")
				{
					Icon = GetIcon(card),
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "Spontaneous", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "Spontaneous", "description"]),
				}
			]
		});

		ModEntry.Instance.Helper.Content.Cards.OnGetDynamicInnateCardTraitOverrides += (_, e) =>
		{
			if (GetMaxEnchantLevel(e.Card) > 0)
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
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.OnMouseDownRight)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_OnMouseDownRight_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.OnInputPhase)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_OnInputPhase_Postfix))
		);

		Spr GetIcon(Card? card)
		{
			var maxEnchantLevel = card is null ? 2 : GetMaxEnchantLevel(card);
			var enchantLevel = Math.Clamp(card is null ? 0 : GetEnchantLevel(card), 0, maxEnchantLevel);
			return maxEnchantLevel == 2 ? EnchantedOf3Icons[enchantLevel].Sprite : EnchantedOf2Icons[enchantLevel].Sprite;
		}
	}

	internal static int GetMaxEnchantLevel(Card card)
		=> card.GetActions(DB.fakeState, DB.fakeCombat).OfType<EnchantGateAction>().Select(a => (int?)a.Level).Max() ?? 0;

	internal static int GetEnchantLevel(Card card)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(card, "EnchantLevel");

	internal static void SetEnchantLevel(Card card, int level)
	{
		if (level <= 0)
			ModEntry.Instance.Helper.ModData.RemoveModData(card, "EnchantLevel");
		else
			ModEntry.Instance.Helper.ModData.SetModData(card, "EnchantLevel", level);
	}

	internal static List<IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult>? GetEnchantLevelPayment(Card card, int level)
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
		if (level > GetMaxEnchantLevel(card))
			return;
		
		var enchantLevelPayments = ModEntry.Instance.Helper.ModData.ObtainModData<Dictionary<int, List<IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult>>>(card, "EnchantLevelPayments");
		enchantLevelPayments[level] = payment.ToList();
	}

	internal static void ClearEnchantLevelPayments(Card card)
		=> ModEntry.Instance.Helper.ModData.RemoveModData(card, "EnchantLevelPayments");

	internal static IKokoroApi.IV2.IActionCostsApi.ICost? GetNextEnchantCost(Card card)
	{
		var gates = card.GetActions(DB.fakeState, DB.fakeCombat).OfType<EnchantGateAction>().ToList();
		var maxEnchantLevel = gates.Select(a => (int?)a.Level).Max() ?? 0;
		var enchantLevel = Math.Clamp(GetEnchantLevel(card), 0, maxEnchantLevel);
		if (enchantLevel >= maxEnchantLevel)
			return null;

		return gates.FirstOrDefault(a => a.Level == enchantLevel + 1)?.Cost
		       ?? ModEntry.Instance.KokoroApi.ActionCosts.MakeCombinedCost([]);
	}

	internal static bool TryEnchant(State state, Card card, bool fromUserInteraction = true)
	{
		var maxEnchantLevel = GetMaxEnchantLevel(card);
		var enchantLevel = Math.Clamp(GetEnchantLevel(card), 0, maxEnchantLevel);

		if (enchantLevel >= maxEnchantLevel)
			return false;
		if (GetNextEnchantCost(card) is not { } cost)
			return false;
		
		var environment = ModEntry.Instance.KokoroApi.ActionCosts.MakeStatePaymentEnvironment(state, state.route as Combat ?? DB.fakeCombat);
		var transaction = ModEntry.Instance.KokoroApi.ActionCosts.GetBestTransaction(cost, environment);
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

		if (fromUserInteraction)
			card.flipAnim = 1;
		
		return true;
	}

	private static int RenderGate(G g, State state, EnchantGateAction action, bool dontDraw)
	{
		var card = state.FindCard(action.CardId) ?? g.state.FindCard(action.CardId);
		var enchantLevel = card is null ? 0 : GetEnchantLevel(card);
		var gateColor = (action.Level - enchantLevel) switch
		{
			1 => NextGateColor,
			<= 0 => PaidGateColor,
			_ => FutureGateColor,
		};
		
		var environment = ModEntry.Instance.KokoroApi.ActionCosts.MakeMockPaymentEnvironment(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatePaymentEnvironment(state, state.route as Combat ?? DB.fakeCombat));

		var gatesBefore = card
			?.GetActions(state, state.route as Combat ?? DB.fakeCombat)
			.OfType<EnchantGateAction>()
			.Where(a => a.Level < action.Level && a.Level > enchantLevel);

		foreach (var gateBefore in gatesBefore ?? [])
		{
			var transactionBefore = ModEntry.Instance.KokoroApi.ActionCosts.GetBestTransaction(gateBefore.Cost, environment);
			transactionBefore.Pay(environment);
		}
		
		var transaction = ModEntry.Instance.KokoroApi.ActionCosts.GetBestTransaction(action.Cost, environment);

		if (enchantLevel >= action.Level)
		{
			if (card is not null && GetEnchantLevelPayment(card, enchantLevel + 1) is { } payment)
				foreach (var singlePayment in payment)
					environment.SetAvailableResource(singlePayment.Payment.Resource, environment.GetAvailableResource(singlePayment.Payment.Resource) + singlePayment.Paid);
			else
				foreach (var (resource, amount) in transaction.Resources)
					environment.SetAvailableResource(resource, amount);
		}
		
		var transactionPaymentResult = transaction.TestPayment(environment);
		var transactionPaymentResultKey = TransactionWholePaymentResultDictionaryKey.From(transactionPaymentResult);

		const int iconLeftMargin = 2;
		
		var position = g.Push(rect: new(x: iconLeftMargin)).rect.xy;
		var initialX = (int)position.x;

		if (!dontDraw && CostOutlineSprites.TryGetValue(transactionPaymentResultKey, out var costOutlineSprite))
		{
			Draw.Rect(position.x - iconLeftMargin, position.y + 4, 53, 1, gateColor);
			Draw.Sprite(costOutlineSprite.Sprite, position.x - 1, position.y - 1, color: gateColor);
		}
		
		action.Cost.Render(g, ref position, enchantLevel >= action.Level, dontDraw, transactionPaymentResult);
		var costWidth = (int)position.x - initialX;

		if (!CostOutlineSprites.ContainsKey(transactionPaymentResultKey))
		{
			var baseTexture = TextureUtils.CreateTexture(costWidth, 10, () =>
			{
				var position = Vec.Zero;
				action.Cost.Render(g, ref position, false, false, transactionPaymentResult);
			});
			var baseTextureData = new MGColor[baseTexture.Width * baseTexture.Height];
			baseTexture.GetData(baseTextureData);
			
			var outlineTexture = new Texture2D(g.mg.GraphicsDevice, baseTexture.Width + 2, baseTexture.Height + 2);
			var outlineTextureData = new MGColor[outlineTexture.Width * outlineTexture.Height];

			for (var y = 0; y < outlineTexture.Height; y++)
				for (var x = 0; x < outlineTexture.Width; x++)
					outlineTextureData[x + y * outlineTexture.Width] = ShouldContainOutline(x, y) ? MGColor.White : MGColor.Transparent;
			
			outlineTexture.SetData(outlineTextureData);
			CostOutlineSprites[transactionPaymentResultKey] = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(() => outlineTexture);
			
			bool ShouldContainOutline(int outlineX, int outlineY)
			{
				var baseX = outlineX - 1;
				var baseY = outlineY - 1;
				
				if (IsNonZeroAlphaPixel(baseX, baseY))
					return false;
				
				foreach (var neighbor in VectorNeighbors)
					if (IsNonZeroAlphaPixel(baseX + neighbor.X, baseY + neighbor.Y))
						return true;

				foreach (var neighbor in VectorNeighbors)
				{
					if (IsNonZeroAlphaPixel(baseX + neighbor.X, baseY + neighbor.Y))
						continue;
					if (!IsNonZeroAlphaPixel(baseX + neighbor.X + neighbor.PerpendicularX1, baseY + neighbor.Y + neighbor.PerpendicularY1))
						continue;
					if (!IsNonZeroAlphaPixel(baseX + neighbor.X + neighbor.PerpendicularX2, baseY + neighbor.Y + neighbor.PerpendicularY2))
						continue;
					return true;
				}

				foreach (var corner in VectorCorners)
				{
					if (!IsNonZeroAlphaPixel(baseX + corner.X, baseY + corner.Y))
						continue;
					if (!IsNonZeroAlphaPixel(baseX + corner.X * 2, baseY + corner.Y))
						continue;
					if (!IsNonZeroAlphaPixel(baseX + corner.X, baseY + corner.Y * 2))
						continue;
					if (IsNonZeroAlphaPixel(baseX + corner.X * 2, baseY))
						continue;
					if (IsNonZeroAlphaPixel(baseX, baseY + corner.Y * 2))
						continue;
					return true;
				}

				return false;
				
				MGColor? GetBaseTextureColor(int baseX, int baseY)
				{
					if (baseX < 0 || baseY < 0 || baseX >= baseTexture.Width || baseY >= baseTexture.Height)
						return null;
					return baseTextureData[baseX + baseY * baseTexture.Width];
				}

				bool IsNonZeroAlphaColor(MGColor? color)
					=> color is { A: > 0 };

				bool IsNonZeroAlphaPixel(int baseX, int baseY)
					=> IsNonZeroAlphaColor(GetBaseTextureColor(baseX, baseY));
			}
		}
		
		g.Pop();
		return 53;
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is EnchantedAction enchantedAction)
		{
			var oldDisabled = enchantedAction.Action.disabled;
			var renderAsDisabled = enchantedAction.disabled || (state != DB.fakeState && state.FindCard(enchantedAction.CardId) is { } card && GetEnchantLevel(card) < enchantedAction.Level);
			enchantedAction.Action.disabled = renderAsDisabled;
			
			__result = Card.RenderAction(g, state, enchantedAction.Action, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
			enchantedAction.Action.disabled = oldDisabled;
			return false;
		}
		
		if (action is EnchantGateAction gateAction)
		{
			__result += RenderGate(g, state, gateAction, dontDraw);
			return false;
		}

		return true;
	}

	private static void Combat_OnMouseDownRight_Postfix(Combat __instance, G g, Box b)
	{
		if (__instance.TryGetHandCardFromBox(b) is not { } card)
			return;
		
		var maxEnchantLevel = GetMaxEnchantLevel(card);
		var enchantLevel = Math.Clamp(GetEnchantLevel(card), 0, maxEnchantLevel);
		if (enchantLevel >= maxEnchantLevel)
			return;

		TryEnchant(g.state, card);
	}

	private static void Combat_OnInputPhase_Postfix(Combat __instance, G g, Box b)
	{
		if (b.key != Input.currentGpKey)
			return;
		if (__instance.TryGetHandCardFromBox(b) is not { } card)
			return;
		if (!Input.GetGpDown(Btn.B))
			return;
		
		var maxEnchantLevel = GetMaxEnchantLevel(card);
		var enchantLevel = Math.Clamp(GetEnchantLevel(card), 0, maxEnchantLevel);
		if (enchantLevel >= maxEnchantLevel)
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
}

internal sealed class EnchantGateAction : CardAction
{
	public required int CardId;
	public required int Level;
	public required IKokoroApi.IV2.IActionCostsApi.ICost Cost;
	
	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;
	}
}

internal sealed class EnchantedAction : CardAction
{
	public required int CardId;
	public required int Level;
	public required CardAction Action;

	public override List<Tooltip> GetTooltips(State s)
		=> Action.GetTooltips(s);

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		if (s.FindCard(CardId) is not { } card)
			return;
		if (EnchantedManager.GetEnchantLevel(card) < Level)
			return;
		c.QueueImmediate(Action);
	}
}