using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class OneLiners : IRegisterable, IWrappedActionHook
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);

		var self = new Step();
		ModEntry.Instance.KokoroApi.Actions.RegisterWrappedActionHook(self, 0);
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not OneLinerAction oneLinerAction)
			return true;

		var selfDisabled = oneLinerAction.disabled;

		var position = g.Push(rect: new()).rect.xy;
		int initialX = (int)position.x;

		var isFirst = true;
		foreach (var wrappedAction in oneLinerAction.Actions)
		{
			if (isFirst)
				isFirst = false;
			else
				position.x += oneLinerAction.Spacing;

			var oldActionDisabled = wrappedAction.disabled;
			wrappedAction.disabled = wrappedAction.disabled || selfDisabled;

			g.Push(rect: new(position.x - initialX, 0));
			position.x += Card.RenderAction(g, state, wrappedAction, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
			g.Pop();

			wrappedAction.disabled = oldActionDisabled;
		}

		__result = (int)position.x - initialX;
		g.Pop();

		return false;
	}

	public List<CardAction>? GetWrappedCardActions(CardAction action)
		=> action is OneLinerAction oneLinerAction ? oneLinerAction.Actions : null;
}

internal sealed class OneLinerAction : CardAction
{
	public required List<CardAction> Actions;
	public int Spacing = 3;

	public override List<Tooltip> GetTooltips(State s)
		=> Actions.SelectMany(a => a.GetTooltips(s)).ToList();

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;
		c.QueueImmediate(Actions);
	}
}