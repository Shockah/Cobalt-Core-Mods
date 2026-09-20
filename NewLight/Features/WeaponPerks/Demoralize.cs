using System.Collections.Generic;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class DemoralizeWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var activeIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/DemoralizeActive.png"));
		var inactiveIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/DemoralizeInactive.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Demoralize", new()
		{
			Icon = (state, card) => card is not null && (MG.inst.g.state ?? state).route is Combat combat && combat.TriggeredDemoralizeCards.Contains(card.uuid) ? inactiveIcon.Sprite : activeIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "Demoralize", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Demoralize")
				{
					Icon = activeIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Demoralize", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Demoralize", "Description"]),
				}
			]
		});

		LegendaryWeaponCard.WeaponPerkConditions[Trait.UniqueName] = weapon => weapon is not WeaponCard.ICannotCrit;
		LegendaryWeaponCard.WeaponPerkElementAssignments[Trait.UniqueName] = WeaponElement.Void;
		
		Crits.Instance.Register(new CritHook(), 0);
	}

	private sealed class CritHook : Crits.IHook
	{
		public void OnCrit(Crits.IHook.OnCritArgs args)
		{
			if (args.Ship.isPlayerShip)
				return;
			if (ModEntry.Instance.KokoroApi.ActionInfo.GetSourceCard(args.State, args.Attack) is not { } sourceCard)
				return;
			if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(args.State, sourceCard, Trait))
				return;
			if (args.Combat.TriggeredDemoralizeCards.Contains(sourceCard.uuid))
				return;

			var newTargetLocalX = args.LocalX - 1;
			var newTargetPart = args.Ship.GetPartAtLocalX(newTargetLocalX);
			var newTargetPartDamageModifier = newTargetPart?.GetDamageModifier();
			if (newTargetPart is null || newTargetPart.type == PType.empty || newTargetPartDamageModifier == PDamMod.brittle || (newTargetPartDamageModifier == PDamMod.weak && args.DamageModifier == PDamMod.brittle))
			{
				newTargetLocalX = args.LocalX + 1;
				newTargetPart = args.Ship.GetPartAtLocalX(newTargetLocalX);
				newTargetPartDamageModifier = newTargetPart?.GetDamageModifier();
			}
			if (newTargetPart is null || newTargetPart.type == PType.empty || newTargetPartDamageModifier == PDamMod.brittle || (newTargetPartDamageModifier == PDamMod.weak && args.DamageModifier == PDamMod.brittle))
				return;

			var newTargetWorldX = args.Ship.x + newTargetLocalX;
			args.Combat.TriggeredDemoralizeCards.Add(sourceCard.uuid);
			args.Combat.QueueImmediate(
				args.DamageModifier == PDamMod.brittle
					? new ABrittle { targetPlayer = false, worldX = newTargetWorldX }
					: new AWeaken { targetPlayer = false, worldX = newTargetWorldX }
			);
		}
	}
}

file static class DemoralizeWeaponPerkExt
{
	extension(Combat combat)
	{
		public HashSet<int> TriggeredDemoralizeCards
			=> ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<int>>(combat, "TriggeredDemoralizeCards");
	}
}