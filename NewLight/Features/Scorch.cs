using System;
using System.Reflection;
using FSPRO;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class Scorch : IRegisterable
{
	public static CustomPartTraits.ITraitEntry Trait { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/PartTraits/Scorch.png"));
		
		Trait = CustomPartTraits.RegisterTrait(package.Manifest, "Scorch", new()
		{
			Icon = _ => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["PartTrait", "Scorch", "Name"]).Localize,
			Tooltips = args =>
			[
				new GlossaryTooltip($"parttrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Scorch")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.parttrait,
					Title = ModEntry.Instance.Localizations.Localize(["PartTrait", "Scorch", "TooltipTitle"], new { Amount = Math.Max(args.Part.Scorch, 1) }),
					Description = ModEntry.Instance.Localizations.Localize(["PartTrait", "Scorch", "Description"], new { Amount = Math.Max(args.Part.Scorch, 1) }),
				}
			],
		});
		
		CustomPartTraits.Instance.Register(new CustomPartTraitsHook(), 0);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnBeginTurn)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_OnBeginTurn_Postfix))
		);
	}

	private static void Ship_OnBeginTurn_Postfix(Ship __instance, Combat c)
	{
		var totalScorch = 0;
		
		foreach (var part in __instance.parts)
		{
			var scorch = part.Scorch;
			if (scorch <= 0)
				continue;
			
			totalScorch += 1;
			part.Scorch = scorch - 1;
		}

		if (totalScorch <= 0)
			return;
		c.QueueImmediate(new AStatus { targetPlayer = __instance.isPlayerShip, status = Status.heat, statusAmount = totalScorch });
	}

	private sealed class CustomPartTraitsHook : CustomPartTraits.IHook
	{
		public void ModifyCustomPartTraits(CustomPartTraits.IHook.ModifyCustomPartTraitsArgs args)
		{
			if (args.Part.Scorch <= 0)
				return;
			args.Traits.Add(Trait);
		}
	}

	public sealed class ApplyScorchAction : CardAction
	{
		public required bool TargetPlayer;
		public required int LocalX;
		public required int Amount;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			var target = TargetPlayer ? s.ship : c.otherShip;

			if (target.GetPartAtLocalX(LocalX) is not { } part || part.type == PType.empty)
			{
				timer = 0;
				return;
			}

			part.Scorch += Amount;
			if (part.Scorch < 3)
			{
				Audio.Play(Event.Status_PowerDown);
				return;
			}

			part.Scorch = 0;
			timer = 0;
			c.QueueImmediate([
				new AHurt { targetPlayer = TargetPlayer, hurtAmount = 1 },
				new ApplyScorchAction { TargetPlayer = TargetPlayer, LocalX = LocalX - 1, Amount = 2 },
				new ApplyScorchAction { TargetPlayer = TargetPlayer, LocalX = LocalX + 1, Amount = 2 },
			]);
		}
	}
}

file static class ScorchExt
{
	extension(Part part)
	{
		public int Scorch
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(part, "Scorch");
			set => ModEntry.Instance.Helper.ModData.SetModData(part, "Scorch", value);
		}
	}
}