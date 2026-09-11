using System;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class QuickdrawWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/Quickdraw.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Quickdraw", new()
		{
			Icon = (_, _) => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "Quickdraw", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Quickdraw")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Quickdraw", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Quickdraw", "Description"]),
				}
			]
		});
		
		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnTurnStart), (Combat combat) =>
		{
			combat.PlayedAnythingThisTurn = false;
		});
		
		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Combat combat) =>
		{
			combat.PlayedAnythingThisTurn = true;
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetCurrentCost)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetCurrentCost_Postfix))
		);
	}

	private static void Card_GetCurrentCost_Postfix(Card __instance, State s, ref int __result)
	{
		if (s.route is not Combat combat)
			return;
		if (combat.PlayedAnythingThisTurn)
			return;
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, __instance, Trait))
			return;
		
		__result = Math.Max(__result - 1, 0);
	}
}

file static class QuickdrawWeaponPerkExt
{
	extension(Combat combat)
	{
		public bool PlayedAnythingThisTurn
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(combat, "PlayedAnythingThisTurn");
			set => ModEntry.Instance.Helper.ModData.SetModData(combat, "PlayedAnythingThisTurn", value);
		}
	}
}