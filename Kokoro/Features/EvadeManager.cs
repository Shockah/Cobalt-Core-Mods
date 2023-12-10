using FSPRO;
using Shockah.Shared;
using System.Linq;

namespace Shockah.Kokoro;

public sealed class EvadeManager : HookManager<IEvadeHook>
{
	internal EvadeManager() : base()
	{
		Register(VanillaEvadeHook.Instance, 0);
		Register(VanillaDebugEvadeHook.Instance, int.MaxValue);
		Register(VanillaTrashAnchorEvadeHook.Instance, 1000);
	}

	public bool IsEvadePossible(State state, Combat combat, EvadeHookContext context)
		=> GetHandlingHook(state, combat, context) is not null;

	public IEvadeHook? GetHandlingHook(State state, Combat combat, EvadeHookContext context = EvadeHookContext.Action)
	{
		foreach (var hook in GetHooksWithProxies(state.EnumerateAllArtifacts()))
		{
			var hookResult = hook.IsEvadePossible(state, combat, context);
			if (hookResult == false)
				return null;
			else if (hookResult == true)
				return hook;
		}
		return null;
	}
}

public sealed class VanillaEvadeHook : IEvadeHook
{
	public static VanillaEvadeHook Instance { get; private set; } = new();

	private VanillaEvadeHook() { }

	public bool? IsEvadePossible(State state, Combat combat, EvadeHookContext context)
		=> state.ship.Get(Status.evade) > 0 ? true : null;

	public void PayForEvade(State state, Combat combat, int direction)
		=> state.ship.Add(Status.evade, -1);
}

public sealed class VanillaDebugEvadeHook : IEvadeHook
{
	public static VanillaDebugEvadeHook Instance { get; private set; } = new();

	private VanillaDebugEvadeHook() { }

	public bool? IsEvadePossible(State state, Combat combat, EvadeHookContext context)
		=> FeatureFlags.Debug && Input.shift ? true : null;
}

public sealed class VanillaTrashAnchorEvadeHook : IEvadeHook
{
	public static VanillaTrashAnchorEvadeHook Instance { get; private set; } = new();

	private VanillaTrashAnchorEvadeHook() { }

	public bool? IsEvadePossible(State state, Combat combat, EvadeHookContext context)
	{
		if (context != EvadeHookContext.Action)
			return null;
		if (!combat.hand.Any(c => c is TrashAnchor))
			return null;

		Audio.Play(Event.Status_PowerDown);
		state.ship.shake += 1.0;
		return false;
	}

	public void PayForEvade(State state, Combat combat, int direction)
		=> state.ship.Add(Status.evade, -1);
}