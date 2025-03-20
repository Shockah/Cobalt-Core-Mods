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
		typeof(BreadnaughtBasicMinigunCard),
		typeof(BreadnaughtBasicPushCard),
		typeof(BreadnaughtBasicPackageCard),
		typeof(BreadnaughtBasicWeakenCard),
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
						new()
						{
							type = PType.wing,
							skin = helper.Content.Ships.RegisterPart("BreadnaughtWingLeft", new()
							{
								Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Breadnaught/Ship/WingLeft.png")).Sprite
							}).UniqueName,
						},
						new()
						{
							type = PType.cockpit,
							skin = helper.Content.Ships.RegisterPart("BreadnaughtCockpit", new()
							{
								Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Breadnaught/Ship/Cockpit.png")).Sprite
							}).UniqueName,
						},
						new()
						{
							type = PType.cannon,
							skin = helper.Content.Ships.RegisterPart("BreadnaughtCannon", new()
							{
								Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Breadnaught/Ship/Cannon.png")).Sprite
							}).UniqueName,
						},
						new()
						{
							type = PType.missiles,
							skin = helper.Content.Ships.RegisterPart("BreadnaughtBay", new()
							{
								Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Breadnaught/Ship/Bay.png")).Sprite
							}).UniqueName,
						},
						new()
						{
							type = PType.wing,
							skin = helper.Content.Ships.RegisterPart("BreadnaughtWingRight", new()
							{
								Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Breadnaught/Ship/WingRight.png")).Sprite
							}).UniqueName,
						},
					]
				},
				artifacts = [
					new ShieldPrep(),
					new BreadnaughtMinigunArtifact(),
				],
				cards = [
					new BreadnaughtBasicPackageCard(),
					new BreadnaughtBasicPackageCard(),
					new DodgeColorless(),
					new BasicShieldColorless(),
				],
			},
			ExclusiveArtifactTypes = ArtifactTypes.ToHashSet(),
		});
		
		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
	}
}