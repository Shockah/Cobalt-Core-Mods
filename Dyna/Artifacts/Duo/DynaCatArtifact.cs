using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class DynaCatArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;

		helper.Content.Artifacts.RegisterArtifact("DynaCat", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DynaCat.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DynaCat", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DynaCat", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DynaDeck.Deck, Deck.colorless]);

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(CannonColorless), nameof(CannonColorless.GetActions)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CannonColorless_GetActions_Postfix))
		);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> new BlastwaveManager.BlastwaveAction { Source = new(), Damage = 1, WorldX = 0 }.GetTooltips(DB.fakeState);

	public override void OnReceiveArtifact(State state)
	{
		base.OnReceiveArtifact(state);
		state.GetCurrentQueue().QueueImmediate(new AAddCard
		{
			destination = CardDestination.Deck,
			card = new CannonColorless(),
			amount = 2
		});
	}

	private static void CannonColorless_GetActions_Postfix(CannonColorless __instance, State s, ref List<CardAction> __result)
	{
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is DynaCatArtifact) is not { } artifact)
			return;

		foreach (var action in __result)
		{
			if (action is AAttack attack && !attack.IsBlastwave())
			{
				attack.SetBlastwave(
					damage: ModEntry.Instance.Api.GetBlastwaveDamage(__instance, s, 1)
				);
				break;
			}
		}
	}
}