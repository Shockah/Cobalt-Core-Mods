using HarmonyLib;
using Nickel;
using Shockah.Shared;

namespace Shockah.Bloch;

internal sealed class DrawEachTurnManager
{
	internal static IStatusEntry DrawEachTurnStatus { get; private set; } = null!;

	public DrawEachTurnManager()
	{
		DrawEachTurnStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("DrawEachTurn", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/DrawEachTurn.png")).Sprite,
				color = DB.statuses[Status.drawNextTurn].color,
				border = DB.statuses[Status.drawNextTurn].border,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "DrawEachTurn", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "DrawEachTurn", "description"]).Localize
		});

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.GetDrawCount)),
			prefix: new HarmonyMethod(GetType(), nameof(Combat_DrawCards_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(Combat_DrawCards_Finalizer))
		);
	}

	private static void Combat_DrawCards_Prefix(State s, ref int __state)
	{
		__state = s.ship.baseDraw;
		s.ship.baseDraw += s.ship.Get(DrawEachTurnStatus.Status);
	}

	private static void Combat_DrawCards_Finalizer(State s, ref int __state)
		=> s.ship.baseDraw = __state;
}
