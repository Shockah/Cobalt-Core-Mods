using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class BreadnaughtShip : IRegisterable
{
	private static readonly List<Type> ArtifactTypes = [
		typeof(BreadnaughtMinigunArtifact),
		typeof(BreadnaughtRotorGreaseArtifact),
	];
	
	private static readonly List<Type> CardTypes = [
	];
	
	private static readonly List<Type> RegisterableTypes = [
		.. ArtifactTypes,
		.. CardTypes,
		typeof(BarrelSpinManager),
	];
	
	internal static IShipEntry ShipEntry { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ShipEntry = helper.Content.Ships.RegisterShip("Breadnaught", new()
		{
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Breadnaught", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Breadnaught", "description"]).Localize,
			Ship = new()
			{
				ship = new()
				{
					hull = 12,
					hullMax = 12,
					shieldMaxBase = 4,
					parts = [
						new() { type = PType.wing, skin = "wing_junker" },
						new() { type = PType.cockpit, skin = "cockpit_artemis" },
						new() { type = PType.cannon, skin = "cannon_conveyor" },
						new() { type = PType.missiles, skin = "missiles_conveyor" },
						new() { type = PType.wing, skin = "wing_junker", flip = true },
					]
				},
				artifacts = [
					new ShieldPrep(),
					new BreadnaughtMinigunArtifact(),
				],
				cards = [
					new CannonColorless(),
					new CannonColorless(),
					new DodgeColorless(),
					new BasicShieldColorless(),
				],
			},
			UnderChassisSprite = StableSpr.parts_chassis,
			ExclusiveArtifactTypes = ArtifactTypes.ToHashSet(),
		});
		
		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
	}
}