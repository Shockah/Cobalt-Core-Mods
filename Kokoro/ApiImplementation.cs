using CobaltCoreModding.Definitions.ModManifests;
using Nanoray.Pintail;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Shockah.Kokoro;

public sealed partial class ApiImplementation : IKokoroApi
{
	private static ModEntry Instance => ModEntry.Instance;
	private static readonly Dictionary<Type, ConditionalWeakTable<object, object?>> ProxyCache = [];
	private readonly IManifest Manifest;
	private readonly HashSet<(string, string)> LoggedBrokenV1ApiCalls = [];

	public ApiImplementation(IManifest manifest)
	{
		this.Manifest = manifest;
		this.V2 = new V2Api(this);
	}

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

		ref var table = ref CollectionsMarshal.GetValueRefOrAddDefault(ProxyCache, typeof(T), out var tableExists);
		if (!tableExists)
			table = [];
		
		if (table!.TryGetValue(@object, out var rawProxy))
		{
			proxy = (T)rawProxy!;
			return rawProxy is not null;
		}

		var newNullableProxy = Instance.Helper.Utilities.ProxyManager.TryProxy<string, T>(@object, "Unknown", Instance.Name, out var newProxy) ? newProxy : null;
		table.AddOrUpdate(@object, newNullableProxy);
		proxy = newNullableProxy;
		return newNullableProxy is not null;
	}

	public IKokoroApi.IV2 V2 { get; }

	public IKokoroApi.IActionApi Actions { get; } = new ActionApiImplementation();

	public sealed partial class ActionApiImplementation : IKokoroApi.IActionApi;

	public sealed partial class V2Api(ApiImplementation parent) : IKokoroApi.IV2
	{
		public bool TryProxy<T>(object @object, [MaybeNullWhen(false)] out T proxy) where T : class
			=> parent.TryProxy(@object, out proxy);
	}
}