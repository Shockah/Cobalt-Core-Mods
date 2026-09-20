using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Shared;
using ILMatches = Nanoray.Shrike.Harmony.ILMatches;

namespace Shockah.NewLight;

internal sealed class CustomPartTraits : HookManager<CustomPartTraits.IHook>, IRegisterable
{
	public readonly struct TraitConfiguration
	{
		public required SingleLocalizationProvider Name { get; init; }
		public required Func<IconArgs, Spr?> Icon { get; init; }
		public Func<RendererArgs, bool>? Renderer { get; init; }
		public Func<TooltipsArgs, IEnumerable<Tooltip>>? Tooltips { get; init; }

		public readonly struct IconArgs
		{
			public required State State { get; init; }
			public required Combat Combat { get; init; }
			public required Ship Ship { get; init; }
			public required Part Part { get; init; }
			public required int LocalX { get; init; }
		}

		public readonly struct RendererArgs
		{
			public required State State { get; init; }
			public required Combat Combat { get; init; }
			public required Ship Ship { get; init; }
			public required Part Part { get; init; }
			public required int LocalX { get; init; }
			public required Vec Position { get; init; }
			public required Color DefaultIconColor { get; init; }
		}

		public readonly struct TooltipsArgs
		{
			public required State State { get; init; }
			public required Combat Combat { get; init; }
			public required Ship Ship { get; init; }
			public required Part Part { get; init; }
			public required int LocalX { get; init; }
		}
	}

	public interface ITraitEntry : IModOwned
	{
		TraitConfiguration Configuration { get; }
	}

	public interface IHook
	{
		void ModifyCustomPartTraits(ModifyCustomPartTraitsArgs args) { }
		
		public readonly struct ModifyCustomPartTraitsArgs
		{
			public required State State { get; init; }
			public required Combat Combat { get; init; }
			public required Ship Ship { get; init; }
			public required Part Part { get; init; }
			public required int LocalX { get; init; }
			public required List<ITraitEntry> Traits { get; init; }
		}
	}

	internal static readonly CustomPartTraits Instance = new();
	
	private static readonly Dictionary<string, ITraitEntry> UniqueNameToTraitEntry = [];
	private static readonly List<ITraitEntry> ScratchTraitList = [];

	private CustomPartTraits() : base(ModEntry.Instance.Package.Manifest.UniqueName)
	{
	}
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.RenderPartUI)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_RenderPartUI_Transpiler))
		);
	}

	public static ITraitEntry RegisterTrait(IModManifest owner, string name, TraitConfiguration configuration)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (UniqueNameToTraitEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A part trait with the unique name `{uniqueName}` is already registered", nameof(name));
		var entry = new TraitEntry(owner, uniqueName, configuration);
		UniqueNameToTraitEntry.Add(uniqueName, entry);
		return entry;
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Ship_RenderPartUI_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(1),
					ILMatches.Ldfld(nameof(G.state)),
					ILMatches.Ldfld(nameof(State.time)),
				])
				.Find([
					ILMatches.Ldarg(3).ExtractLabels(out var labels),
					ILMatches.Ldfld(nameof(Part.invincible)),
					ILMatches.Brfalse,
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Ldarg, 4),
					new CodeInstruction(OpCodes.Ldarg, 5),
					new CodeInstruction(OpCodes.Ldarg, 6),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_RenderPartUI_Transpiler_RenderCustom))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static void Ship_RenderPartUI_Transpiler_RenderCustom(G g, Combat? maybeCombat, Ship ship, Part part, int localX, string keyPrefix, bool isPreview)
	{
		var combat = maybeCombat ?? DB.fakeCombat;
		
		ScratchTraitList.Clear();
		var modifyCustomPartTraitsArgs = new IHook.ModifyCustomPartTraitsArgs
		{
			State = g.state,
			Combat = combat,
			Ship = ship,
			Part = part,
			LocalX = localX,
			Traits = ScratchTraitList,
		};
		
		foreach (var hook in Instance.Hooks)
			hook.ModifyCustomPartTraits(modifyCustomPartTraitsArgs);

		if (ScratchTraitList.Count == 0)
			return;
		if (g.boxes.LastOrDefault(box => box.key == new UIKey(StableUK.part, localX, keyPrefix)) is not { } box)
			return;

		var hitboxHeight = isPreview ? 25 : 34;
		var basePosition = box.rect.xy + new Vec(-1.0, ship.isPlayerShip ? hitboxHeight - 16 : 8);
		var defaultIconColor = new Color(1.0, 1.0, 1.0, 0.8 + Math.Sin(g.state.time * 4.0) * 0.3);

		var canRenderInDamageModSpot = part.GetDamageModifier() == PDamMod.none;
		var canRenderInStunModSpot = part.stunModifier == PStunMod.none;
		var nextCustomSpotIndex = 0;

		foreach (var trait in ScratchTraitList)
		{
			if (trait.Configuration.Tooltips is not null && box.IsHover())
				g.tooltips.Add(g.tooltips.pos, trait.Configuration.Tooltips(new()
				{
					State = g.state,
					Combat = combat,
					Ship = ship,
					Part = part,
					LocalX = localX,
				}));
			
			Vec position;
			if (canRenderInDamageModSpot)
			{
				position = basePosition + new Vec(1, 0);
				canRenderInDamageModSpot = false;
			}
			else if (canRenderInStunModSpot)
			{
				position = basePosition + new Vec(9, 0);
				canRenderInStunModSpot = false;
			}
			else
			{
				position = basePosition + new Vec(1 + nextCustomSpotIndex % 2 * 8, (1 + nextCustomSpotIndex / 2) * (ship.isPlayerShip ? -8 : 8));
				nextCustomSpotIndex++;
			}

			if (trait.Configuration.Renderer is { } renderer)
			{
				renderer(new()
				{
					State = g.state,
					Combat = combat,
					Ship = ship,
					Part = part,
					LocalX = localX,
					Position = position,
					DefaultIconColor = defaultIconColor,
				});
				continue;
			}

			var icon = trait.Configuration.Icon(new()
			{
				State = g.state,
				Combat = combat,
				Ship = ship,
				Part = part,
				LocalX = localX,
			});
			if (icon is null)
				continue;
			
			Draw.Sprite(icon.Value, position.x, position.y, color: defaultIconColor);
		}
	}

	private sealed class TraitEntry(IModManifest modOwner, string uniqueName, TraitConfiguration configuration) : ITraitEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public TraitConfiguration Configuration { get; } = configuration;
	}
}