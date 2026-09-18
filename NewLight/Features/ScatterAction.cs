using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.NewLight;

internal sealed class ScatterAction : AAttack, IRegisterable
{
	private static readonly Dictionary<int, ISpriteEntry> Icons = [];

	public int ScatterDirection;
	
	[JsonProperty]
	private bool Scattered;

	public override Icon? GetIcon(State s)
	{
		var icon = Icons[Math.Sign(ScatterDirection)];
		if (base.GetIcon(s) is { } result)
			return result with { path = icon.Sprite };
		return new(icon.Sprite, damage, Colors.redd);
	}

	public override List<Tooltip> GetTooltips(State s)
	{
		var results = base.GetTooltips(s);
		
		var directionString = ScatterDirection switch
		{
			< 0 => "Left",
			> 0 => "Right",
			_ => "Centered"
		};
		
		for (var i = 0; i < results.Count; i++)
		{
			if (results[i] is not TTGlossary { key: "action.attack.name" })
				continue;
			
			var icon = Icons[Math.Sign(ScatterDirection)];
			results[i] = new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::Scatter{directionString}")
			{
				Icon = icon.Sprite,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["Action", "Scatter", "Name", directionString]),
				Description = ModEntry.Instance.Localizations.Localize(["Action", "Scatter", "Description", directionString], new { Damage = damage }),
			};
			break;
		}
		
		return results;
	}

	public override void Begin(G g, State s, Combat c)
	{
		if (Scattered)
		{
			var timer = this.timer;
			base.Begin(g, s, c);
			this.timer = timer;
			return;
		}

		if (ScatterDirection == 0)
		{
			var midAttack = Mutil.DeepCopy(this);
			midAttack.Scattered = true;
			var leftAttack = Mutil.DeepCopy(midAttack);
			var rightAttack = Mutil.DeepCopy(midAttack);

			var splitDamage = damage / 3;
			var leftoverDamage = damage % 3;

			midAttack.damage = splitDamage + leftoverDamage;
			midAttack.timer *= 0.5;
			
			leftAttack.damage = splitDamage;
			leftAttack.ScatterDirection = -1;
			leftAttack.timer *= 0.25;
			
			rightAttack.damage = splitDamage;
			rightAttack.ScatterDirection = 1;
			rightAttack.timer *= 0.25;
		
			c.QueueImmediate([leftAttack, rightAttack, midAttack]);
		}
		else
		{
			var midAttack = Mutil.DeepCopy(this);
			midAttack.Scattered = true;
			var sideAttack = Mutil.DeepCopy(midAttack);
			
			var splitDamage = damage / 2;
			var leftoverDamage = damage % 2;
			
			midAttack.damage = splitDamage + leftoverDamage;
			midAttack.timer *= 0.75;
			
			sideAttack.damage = splitDamage;
			sideAttack.ScatterDirection = Math.Sign(ScatterDirection);
			sideAttack.timer *= 0.25;
		
			c.QueueImmediate([sideAttack, midAttack]);
		}
		
		timer = 0;
	}

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		for (var direction = -1; direction <= 1; direction++)
		{
			var directionString = direction switch
			{
				< 0 => "Left",
				> 0 => "Right",
				_ => "Centered"
			};
			Icons[direction] = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Actions/Scatter{directionString}.png"));
		}
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(GetFromX)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_GetFromX_Postfix))
		);
	}

	private static void AAttack_GetFromX_Postfix(AAttack __instance, ref int? __result)
	{
		if (__instance is not ScatterAction scatterAction)
			return;
		if (!scatterAction.Scattered)
			return;
		if (__result is not { } result)
			return;

		__result = result + scatterAction.ScatterDirection;
	}
}