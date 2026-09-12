using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal sealed class PrecisionCardTrait : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;

	private static ISpriteEntry BaseIcon = null!;
	private static readonly Dictionary<int, Spr> Icons = [];
	private static readonly Dictionary<string, Dictionary<Upgrade, int>> PrecisionPerUpgrade = [];
	private static AAttack? AttackContext;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		BaseIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardTraits/Precision.png"));
		
		Trait = helper.Content.Cards.RegisterTrait("Precision", new()
		{
			Icon = (state, card) => ObtainIcon(card is null ? 10 : GetPrecision(state, card)),
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "Precision", "Name"]).Localize,
			Tooltips = (state, card) =>
			{
				string description;
				if (card is null)
					description = ModEntry.Instance.Localizations.Localize(["CardTrait", "Precision", "Description", "WithoutCard"]);
				else
					description = ModEntry.Instance.Localizations.Localize(["CardTrait", "Precision", "Description", "WithCard"], new { Amount = GetPrecision(state, card) });

				return [
					new GlossaryTooltip($"cardtrait.{package.Manifest.UniqueName}::Precision")
					{
						Icon = ObtainIcon(card is null ? 10 : GetPrecision(DB.fakeState, card)),
						TitleColor = Colors.cardtrait,
						Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "Precision", "Name"]),
						Description = description,
					}
				];
			}
		});

		helper.Content.Cards.OnGetDynamicInnateCardTraitOverrides += (_, args) =>
		{
			if (GetPrecision(args.State, args.Card) > 0)
				args.SetOverride(Trait, true);
		};
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.ModifyDamageDueToParts)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_ModifyDamageDueToParts_Prefix))
		);
	}
	
	public static void SetPrecision(string key, int value)
	{
		SetPrecision(key, Upgrade.None, value);
		SetPrecision(key, Upgrade.A, value);
		SetPrecision(key, Upgrade.B, value);
	}
	
	public static void SetPrecision(string key, Upgrade upgrade, int value)
	{
		ref var perUpgrade = ref CollectionsMarshal.GetValueRefOrAddDefault(PrecisionPerUpgrade, key, out var perUpgradeExists);
		if (!perUpgradeExists)
			perUpgrade = [];
		perUpgrade![upgrade] = value;
	}

	public static int GetPrecision(State state, Card card)
		=> PrecisionPerUpgrade.TryGetValue(card.Key(), out var perUpgrade) ? perUpgrade.GetValueOrDefault(card.upgrade) : 0;

	private static Spr ObtainIcon(int amount)
	{
		amount = Math.Clamp(amount, 0, 10);
		if (Icons.TryGetValue(amount, out var icon))
			return icon;

		icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite($"Precision{amount}", () =>
		{
			var baseIcon = SpriteLoader.Get(BaseIcon.Sprite)!;
			return TextureUtils.CreateTexture(new(baseIcon.Width, baseIcon.Height)
			{
				Actions = _ =>
				{
					Draw.Sprite(baseIcon, 0, 0);

					var text = amount > 9 ? "+" : amount.ToString();
					var textRect = Draw.Text(text, 0, 0, outline: Colors.black, dontDraw: true, dontSubstituteLocFont: true);
					Draw.Text(text, baseIcon.Width - textRect.w, baseIcon.Height - textRect.h - 1, color: Colors.white, outline: Colors.black, dontSubstituteLocFont: true);
				},
			});
		}).Sprite;

		Icons[amount] = icon;
		return icon;
	}

	private static void AAttack_Begin_Prefix(AAttack __instance)
		=> AttackContext = __instance;

	private static void AAttack_Begin_Finalizer()
		=> AttackContext = null;

	private static void Ship_ModifyDamageDueToParts_Prefix(Ship __instance, State s, Combat c, ref int incomingDamage, Part part)
	{
		if (AttackContext is null)
			return;
		if (AttackContext.targetPlayer)
			return;
		if (__instance == s.ship)
			return;
		if (part.GetDamageModifier() is not (PDamMod.brittle or PDamMod.weak))
			return;
		if (ModEntry.Instance.KokoroApi.ActionInfo.GetSourceCard(s, AttackContext) is not { } sourceCard)
			return;
		
		var precision = GetPrecision(s, sourceCard);
		if (precision <= 0)
			return;

		incomingDamage += precision;
	}
}