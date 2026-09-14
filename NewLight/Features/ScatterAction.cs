using System;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.NewLight;

internal sealed class ScatterAction : AAttack, IRegisterable
{
	private static ISpriteEntry Icon = null!;

	public int Direction;
	
	[JsonProperty]
	private bool Scattered;

	public override Icon? GetIcon(State s)
	{
		if (base.GetIcon(s) is { } icon)
			return icon with { path = Icon.Sprite };
		return new(Icon.Sprite, damage, Colors.redd);
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

		if (Direction == 0)
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
			leftAttack.Direction = -1;
			leftAttack.timer *= 0.25;
			
			rightAttack.damage = splitDamage;
			rightAttack.Direction = 1;
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
			sideAttack.Direction = Math.Sign(Direction);
			sideAttack.timer *= 0.25;
		
			c.QueueImmediate([sideAttack, midAttack]);
		}
		
		timer = 0;
	}

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Icon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Actions/Scatter.png"));
		
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

		__result = result + scatterAction.Direction;
	}
}