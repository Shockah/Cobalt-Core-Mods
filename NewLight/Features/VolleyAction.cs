using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.NewLight;

internal sealed class VolleyAction : AAttack, IRegisterable
{
	private static readonly Dictionary<(int direction, bool isShort), ISpriteEntry> Icons = [];

	public int VolleyDirection;
	
	[JsonProperty]
	private bool VolleyDone;

	private static AAttack? AttackContext;

	public override Icon? GetIcon(State s)
	{
		var icon = Icons[(Math.Sign(VolleyDirection), Math.Abs(VolleyDirection) == 1)];
		if (base.GetIcon(s) is { } result)
			return result with { path = icon.Sprite };
		return new(icon.Sprite, damage, Colors.redd);
	}

	public override List<Tooltip> GetTooltips(State s)
	{
		var results = base.GetTooltips(s);
		
		var isShort = Math.Abs(VolleyDirection) == 1;
		var shortOrLongString = isShort ? "Short" : "Long";
		var directionString = VolleyDirection < 0 ? "Left" : "Right";
		
		for (var i = 0; i < results.Count; i++)
		{
			if (results[i] is not TTGlossary { key: "action.attack.name" })
				continue;
			
			var icon = Icons[(Math.Sign(VolleyDirection), isShort)];
			results[i] = new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::Volley{directionString}{shortOrLongString}")
			{
				Icon = icon.Sprite,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["Action", "Volley", "Name", directionString, shortOrLongString]),
				Description = ModEntry.Instance.Localizations.Localize(["Action", "Volley", "Description", directionString], new { Damage = damage, Range = Math.Abs(VolleyDirection) }),
			};
			break;
		}
		
		return results;
	}

	public override void Begin(G g, State s, Combat c)
	{
		if (VolleyDone || VolleyDirection == 0)
		{
			var timer = this.timer;
			base.Begin(g, s, c);
			this.timer = timer;
			return;
		}

		var attackCount = Math.Min(Math.Abs(VolleyDirection) + 1, damage);
		var splitDamage = damage / attackCount;
		var leftoverDamage = damage % attackCount;
		
		c.QueueImmediate(
			Enumerable.Range(0, attackCount)
				.Select(i =>
				{
					var attack = Mutil.DeepCopy(this);
					attack.VolleyDone = true;
					attack.damage = splitDamage + ((attackCount - i - 1) < leftoverDamage ? 1 : 0);
					attack.VolleyDirection = i * Math.Sign(VolleyDirection);
					attack.timer /= attackCount;
					return attack;
				})
		);
		
		timer = 0;
	}

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		for (var direction = -1; direction <= 1; direction += 2)
		{
			var directionString = direction < 0 ? "Left" : "Right";
			Icons[(direction, true)] = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Actions/Volley{directionString}Short.png"));
			Icons[(direction, false)] = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Actions/Volley{directionString}Long.png"));
		}
		
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
		if (__instance is not VolleyAction volley)
			return;
		if (!volley.VolleyDone)
			return;
		if (__result is not { } result)
			return;

		__result = result + volley.VolleyDirection;
	}

	private static void AAttack_Begin_Prefix(AAttack __instance)
		=> AttackContext = __instance;

	private static void AAttack_Begin_Finalizer()
		=> AttackContext = null;

	private static void Part_GetDamageModifier_Postfix(ref PDamMod __result)
	{
		if (AttackContext is not VolleyAction)
			return;
		if (__result is PDamMod.weak or PDamMod.brittle)
			__result = PDamMod.none;
	}
}