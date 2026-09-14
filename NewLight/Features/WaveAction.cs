using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.NewLight;

internal sealed class WaveAction : AAttack, IRegisterable
{
	private static ISpriteEntry ShortIcon = null!;
	private static ISpriteEntry LongIcon = null!;

	public int Direction;
	
	[JsonProperty]
	private bool Waved;

	private static AAttack? AttackContext;
	
	private Spr LengthBasedIcon
		=> Math.Abs(Direction) <= 1 ? ShortIcon.Sprite : LongIcon.Sprite;

	public override Icon? GetIcon(State s)
	{
		if (base.GetIcon(s) is { } icon)
			return icon with { path = LengthBasedIcon };
		return new(LengthBasedIcon, damage, Colors.redd);
	}

	public override void Begin(G g, State s, Combat c)
	{
		if (Waved || Direction == 0)
		{
			var timer = this.timer;
			base.Begin(g, s, c);
			this.timer = timer;
			return;
		}

		var attackCount = Math.Min(Math.Abs(Direction) + 1, damage);
		var splitDamage = damage / attackCount;
		var leftoverDamage = damage % attackCount;
		
		c.QueueImmediate(
			Enumerable.Range(0, attackCount)
				.Select(i =>
				{
					var attack = Mutil.DeepCopy(this);
					attack.Waved = true;
					attack.damage = splitDamage + ((attackCount - i - 1) < leftoverDamage ? 1 : 0);
					attack.Direction = i * Math.Sign(Direction);
					attack.timer /= attackCount;
					return attack;
				})
		);
		
		timer = 0;
	}

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ShortIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Actions/WaveShort.png"));
		LongIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Actions/WaveLong.png"));
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(GetFromX)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_GetFromX_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Part), nameof(Part.GetDamageModifier)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Part_GetDamageModifier_Postfix))
		);
	}

	private static void AAttack_GetFromX_Postfix(AAttack __instance, ref int? __result)
	{
		if (__instance is not WaveAction waveAction)
			return;
		if (!waveAction.Waved)
			return;
		if (__result is not { } result)
			return;

		__result = result + waveAction.Direction;
	}

	private static void AAttack_Begin_Prefix(AAttack __instance)
		=> AttackContext = __instance;

	private static void AAttack_Begin_Finalizer()
		=> AttackContext = null;

	private static void Part_GetDamageModifier_Postfix(ref PDamMod __result)
	{
		if (AttackContext is not WaveAction)
			return;
		if (__result is PDamMod.weak or PDamMod.brittle)
			__result = PDamMod.none;
	}
}