using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class FlickerShip : IRegisterable
{
	private static readonly List<Type> ArtifactTypes = [
		typeof(FlickerFleetingCoreArtifact),
	];
	
	private static readonly List<Type> CardTypes = [
	];
	
	private static readonly List<Type> RegisterableTypes = [
		.. ArtifactTypes,
		.. CardTypes,
		typeof(FlickerBorrow),
	];
	
	internal static IShipEntry ShipEntry { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ShipEntry = helper.Content.Ships.RegisterShip("Flicker", new()
		{
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Flicker", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Flicker", "description"]).Localize,
			Ship = new()
			{
				ship = new()
				{
					hull = 7,
					hullMax = 7,
					shieldMaxBase = 2,
					baseEnergy = 2,
					parts = [
						new()
						{
							type = PType.cannon,
							skin = "cannon_apollo",
						},
						new()
						{
							type = PType.cockpit,
							skin = "cockpit_apollo",
						},
						new()
						{
							type = PType.missiles,
							skin = "missiles_apollo",
						},
					],
				},
				artifacts = [
					new ShieldPrep(),
					new FlickerFleetingCoreArtifact(),
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