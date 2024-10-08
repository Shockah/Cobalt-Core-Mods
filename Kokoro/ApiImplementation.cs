using CobaltCoreModding.Definitions.ModManifests;
using Nanoray.Pintail;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Shockah.Kokoro;

public sealed partial class ApiImplementation(
	IManifest manifest
) : IKokoroApi
{
	private static ModEntry Instance => ModEntry.Instance;

	private static readonly Dictionary<Type, ConditionalWeakTable<object, object?>> ProxyCache = [];

	public TimeSpan TotalGameTime
		=> TimeSpan.FromSeconds(MG.inst.g?.time ?? 0);

	public IEnumerable<Card> GetCardsEverywhere(State state, bool hand = true, bool drawPile = true, bool discardPile = true, bool exhaustPile = true)
	{
		if (drawPile)
			foreach (var card in state.deck)
				yield return card;
		if (state.route is Combat combat)
		{
			if (hand)
				foreach (var card in combat.hand)
					yield return card;
			if (discardPile)
				foreach (var card in combat.discard)
					yield return card;
			if (exhaustPile)
				foreach (var card in combat.exhausted)
					yield return card;
		}
	}

	public bool TryProxy<T>(object @object, [MaybeNullWhen(false)] out T proxy) where T : class
	{
		if (!typeof(T).IsInterface)
		{
			proxy = null;
			return false;
		}
		if (!ProxyCache.TryGetValue(typeof(T), out var table))
		{
			table = [];
			ProxyCache[typeof(T)] = table;
		}
		if (table.TryGetValue(@object, out var rawProxy))
		{
			proxy = (T)rawProxy!;
			return rawProxy is not null;
		}

		var newNullableProxy = Instance.Helper.Utilities.ProxyManager.TryProxy<string, T>(@object, "Unknown", Instance.Name, out var newProxy) ? newProxy : null;
		table.AddOrUpdate(@object, newNullableProxy);
		proxy = newNullableProxy;
		return newNullableProxy is not null;
	}

	public IKokoroApi.IActionApi Actions { get; } = new ActionApiImplementation();

	public sealed partial class ActionApiImplementation : IKokoroApi.IActionApi;
	
	public sealed partial class TemporaryUpgradesApi : IKokoroApi.ITemporaryUpgradesApi;
}