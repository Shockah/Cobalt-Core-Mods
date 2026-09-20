using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class Severed : IRegisterable
{
	public static CustomPartTraits.ITraitEntry Trait { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/PartTraits/Severed.png"));
		
		Trait = CustomPartTraits.RegisterTrait(package.Manifest, "Severed", new()
		{
			Icon = _ => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["PartTrait", "Severed", "Name"]).Localize,
			Tooltips = _ =>
			[
				new GlossaryTooltip($"parttrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Severed")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.parttrait,
					Title = ModEntry.Instance.Localizations.Localize(["PartTrait", "Severed", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["PartTrait", "Severed", "Description"]),
				}
			],
		});
		
		CustomPartTraits.Instance.Register(new CustomPartTraitsHook(), 0);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnAfterTurn)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_OnAfterTurn_Postfix))
		);
	}

	private static void Ship_OnAfterTurn_Postfix(Ship __instance)
	{
		foreach (var part in __instance.parts)
			part.Severed = false;
	}

	private sealed class CustomPartTraitsHook : CustomPartTraits.IHook
	{
		public void ModifyCustomPartTraits(CustomPartTraits.IHook.ModifyCustomPartTraitsArgs args)
		{
			if (!args.Part.Severed)
				return;
			args.Traits.Add(Trait);
		}
	}
}

public static class SeveredExt
{
	extension(Part part)
	{
		public bool Severed
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(part, "Severed");
			set
			{
				if (value)
					ModEntry.Instance.Helper.ModData.SetModData(part, "Severed", true);
				else
					ModEntry.Instance.Helper.ModData.RemoveModData(part, "Severed");
			}
		}
	}
}