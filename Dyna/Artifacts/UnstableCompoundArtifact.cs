using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class UnstableCompoundArtifact : Artifact, IRegisterable, IDynaHook, IHookPriority
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("UnstableCompound", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DynaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/UnstableCompound.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "UnstableCompound", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "UnstableCompound", "description"]).Localize,
		});

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActionsOverridden)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActionsOverridden_Postfix)), priority: Priority.Low)
		);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> new BlastwaveManager.BlastwaveAction { Source = new(), Damage = 1, Range = 1, LocalX = 0 }.GetTooltips(DB.fakeState)
			.Concat(new BlastwaveManager.BlastwaveAction { Source = new(), Damage = 1, Range = 2, LocalX = 0 }.GetTooltips(DB.fakeState))
			.ToList();

	public double HookPriority
		=> -10;

	public void OnBlastwaveTrigger(State state, Combat combat, Ship ship, int worldX, bool hitMidrow)
	{
		if (ship.isPlayerShip)
			return;

		combat.QueueImmediate(new AHurt
		{
			targetPlayer = true,
			hurtShieldsFirst = true,
			hurtAmount = 1,
			artifactPulse = Key()
		});
	}

	private static void Card_GetActionsOverridden_Postfix(State s, ref List<CardAction> __result)
	{
		if (!s.EnumerateAllArtifacts().Any(a => a is UnstableCompoundArtifact))
			return;

		foreach (var action in __result)
			foreach (var wrappedAction in ModEntry.Instance.KokoroApi.WrappedActions.GetWrappedCardActionsRecursively(action))
				if (wrappedAction is AAttack attack && attack.IsBlastwave() && attack.GetBlastwaveRange() <= 1)
					ModEntry.Instance.Helper.ModData.SetModData(attack, "BlastwaveRange", 2);
	}
}