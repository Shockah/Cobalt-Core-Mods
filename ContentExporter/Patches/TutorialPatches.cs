using System.Reflection;
using HarmonyLib;
using Shockah.Shared;

namespace Shockah.ContentExporter;

internal sealed class TutorialPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Tutorial), nameof(Tutorial.AttachTutorialsByKey)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Tutorial_AttachTutorialsByKey_Postfix))
		);
	}

	private static void Tutorial_AttachTutorialsByKey_Postfix(G g)
	{
		if (Instance.QueuedTasks.Count == 0)
			return;
		Draw.Text(Instance.Localizations.Localize(["progress"], new { Count = Instance.QueuedTasks.Count }), 4, g.mg.PIX_H - 10, color: Colors.textMain, outline: Colors.black);
	}
}