using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal sealed class Crits : HookManager<Crits.IHook>, IRegisterable
{
	public interface IHook
	{
		void OnCrit(OnCritArgs args) { }
		
		public readonly struct OnCritArgs
		{
			public required State State { get; init; }
			public required Combat Combat { get; init; }
			public required Ship Ship { get; init; }
			public required Part Part { get; init; }
			public required int LocalX { get; init; }
			public required PDamMod DamageModifier { get; init; }
			public required AAttack Attack { get; init; }
		}
	}
	
	internal static readonly Crits Instance = new();

	private static AAttack? AttackContext;

	private Crits() : base(ModEntry.Instance.Package.Manifest.UniqueName)
	{
	}

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.NormalDamage)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_NormalDamage_Prefix))
		);
	}

	private static void AAttack_Begin_Prefix(AAttack __instance)
		=> AttackContext = __instance;

	private static void AAttack_Begin_Finalizer()
		=> AttackContext = null;

	private static void Ship_NormalDamage_Prefix(Ship __instance, State s, Combat c, int? maybeWorldGridX)
	{
		if (AttackContext is null)
			return;
		if (AttackContext.targetPlayer)
			return;
		if (__instance == s.ship)
			return;
		if (maybeWorldGridX is not { } worldGridX)
			return;
		if (__instance.GetPartAtWorldX(worldGridX) is not { } part || part.type == PType.empty)
			return;

		var damageModifier = part.GetDamageModifier();
		if (damageModifier is not (PDamMod.brittle or PDamMod.weak))
			return;

		var args = new IHook.OnCritArgs
		{
			State = s,
			Combat = c,
			Ship = __instance,
			Part = part,
			LocalX = worldGridX - __instance.x,
			DamageModifier = damageModifier,
			Attack = AttackContext,
		};
		foreach (var hook in Instance)
			hook.OnCrit(args);
	}
}