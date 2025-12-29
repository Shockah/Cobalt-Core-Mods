using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class AgniShip : IRegisterable
{
	private static readonly List<Type> ArtifactTypes = [
		typeof(AgniGeneratorOverloadArtifact),
		typeof(AgniWaterCoolingArtifact),
		typeof(AgniUpgradedCapacitorsArtifact),
	];
	
	private static readonly List<Type> CardTypes = [
	];
	
	private static readonly List<Type> RegisterableTypes = [
		.. ArtifactTypes,
		.. CardTypes,
		typeof(AgniOverload),
	];
	
	internal static IShipEntry ShipEntry { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ShipEntry = helper.Content.Ships.RegisterShip("Agni", new()
		{
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Agni", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Agni", "description"]).Localize,
			Ship = new()
			{
				ship = new()
				{
					hull = 10,
					hullMax = 10,
					shieldMaxBase = 4,
					parts = [
						new() { type = PType.wing, skin = "wing" },
						new() { type = PType.missiles, skin = "missiles" },
						new() { type = PType.cockpit, skin = "cockpit_artemis" },
						new() { type = PType.cannon, skin = "cannon_cicada3" },
						new() { type = PType.wing, skin = "wing", flip = true },
					],
					chassisUnder = "chassis_goliath",
				},
				artifacts = [
					new ShieldPrep(),
					new AgniGeneratorOverloadArtifact(),
				],
				cards = [
					new CannonColorless(),
					new CannonColorless(),
					new DodgeColorless(),
					new BasicShieldColorless(),
				],
			},
			// UnderChassisSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Agni/Ship/Chassis.png")).Sprite,
			ExclusiveArtifactTypes = ArtifactTypes.ToHashSet(),
		});
		
		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
	}
}