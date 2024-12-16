using System;
using System.Collections.Generic;
using System.Linq;
using FSPRO;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.Destiny;

internal sealed class EnchantedManager
{
	internal static ISpriteEntry[] EnchantedOf2Icons { get; private set; } = null!;
	internal static ISpriteEntry[] EnchantedOf3Icons { get; private set; } = null!;
	internal static ICardTraitEntry EnchantedTrait { get; private set; } = null!;
	
	private static readonly Dictionary<string, Dictionary<Upgrade, IKokoroApi.IV2.IActionCostsApi.ICost[]>> UpgradeCosts = [];
	
	public EnchantedManager()
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

		Spr GetIcon(Card? card)
		{
			var maxEnchantLevel = card is null ? 2 : GetMaxEnchantLevel(card);
			var enchantLevel = Math.Clamp(card is null ? 0 : GetEnchantLevel(card), 0, maxEnchantLevel);
			return maxEnchantLevel == 2 ? EnchantedOf2Icons[enchantLevel].Sprite : EnchantedOf3Icons[enchantLevel].Sprite;
		}
	}

	internal static int GetMaxEnchantLevel(Card card)
	{
		if (!UpgradeCosts.TryGetValue(card.Key(), out var perUpgradeCosts))
			return 0;
		if (!perUpgradeCosts.TryGetValue(card.upgrade, out var costs))
			return 0;
		return costs.Length;
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

	internal static IKokoroApi.IV2.IActionCostsApi.ICost? GetNextEnchantCost(Card card)
	{
		var maxEnchantLevel = GetMaxEnchantLevel(card);
		var enchantLevel = Math.Clamp(GetEnchantLevel(card), 0, maxEnchantLevel);
		if (enchantLevel >= maxEnchantLevel)
			return null;
		
		if (!UpgradeCosts.TryGetValue(card.Key(), out var perUpgradeCosts))
			return null;
		if (!perUpgradeCosts.TryGetValue(card.upgrade, out var costs))
			return null;
		return costs[enchantLevel];
	}

	internal static void SetEnchantCosts(string key, Upgrade upgrade, IEnumerable<IKokoroApi.IV2.IActionCostsApi.ICost> costs)
	{
		if (!UpgradeCosts.TryGetValue(key, out var perUpgradeCosts))
		{
			perUpgradeCosts = [];
			UpgradeCosts[key] = perUpgradeCosts;
		}
		perUpgradeCosts[upgrade] = costs.ToArray();
	}

	internal static bool TryEnchant(State state, Card card)
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
			card.shakeNoAnim = 1.0;
			Audio.Play(Event.ZeroEnergy);
			return false;
		}

		transaction.Pay(environment);
		SetEnchantLevel(card, enchantLevel + 1);
		return true;
	}
}