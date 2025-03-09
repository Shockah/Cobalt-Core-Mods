using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class KeplerShip : IRegisterable
{
	private static readonly List<Type> ArtifactTypes = [
		typeof(KeplerBayArtifact),
		typeof(KeplerBayV2Artifact),
		typeof(KeplerMissilePDSArtifact),
		typeof(KeplerMissileTractorBeamArtifact),
	];
	
	private static readonly List<Type> CardTypes = [
		typeof(KeplerToggleBayCard),
		typeof(KeplerSwarmModeCard),
		typeof(KeplerBasicDroneCard),
		typeof(KeplerBasicMineCard),
		typeof(KeplerRelaunchCard),
	];
	
	private static readonly List<Type> RegisterableTypes = [
		.. ArtifactTypes,
		.. CardTypes,
		typeof(KeplerMissileHitHookManager),
	];
	
	internal static IDeckEntry ShipDeck { get; private set; } = null!;
	internal static IShipEntry ShipEntry { get; private set; } = null!;
	internal static IPartEntry LeftBayEntry { get; private set; } = null!;
	internal static IPartEntry RightBayEntry { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ShipDeck = helper.Content.Decks.RegisterDeck("Kepler", new()
		{
			Definition = new() { color = new("519468"), titleColor = Colors.black },
			DefaultCardArt = StableSpr.cards_colorless,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Kepler/CardFrame.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Kepler", "name"]).Localize,
		});
		
		LeftBayEntry = helper.Content.Ships.RegisterPart("KeplerBayLeft", new()
		{
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Kepler/Ship/BayLeftActive.png")).Sprite,
			DisabledSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Kepler/Ship/BayLeftInactive.png")).Sprite,
		});
		RightBayEntry = helper.Content.Ships.RegisterPart("KeplerBayRight", new()
		{
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Kepler/Ship/BayRightActive.png")).Sprite,
			DisabledSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Kepler/Ship/BayRightInactive.png")).Sprite,
		});
		
		ShipEntry = helper.Content.Ships.RegisterShip("Kepler", new()
		{
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Kepler", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Kepler", "description"]).Localize,
			Ship = new()
			{
				ship = new()
				{
					hull = 7,
					hullMax = 7,
					shieldMaxBase = 3,
					parts = [
						new() { type = PType.missiles, skin = LeftBayEntry.UniqueName },
						new() {
							type = PType.cockpit,
							skin = helper.Content.Ships.RegisterPart("KeplerCockpit", new()
							{
								Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Kepler/Ship/Cockpit.png")).Sprite
							}).UniqueName,
						},
						new() {
							type = PType.cannon,
							skin = helper.Content.Ships.RegisterPart("KeplerCannon", new()
							{
								Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Kepler/Ship/Cannon.png")).Sprite
							}).UniqueName,
						},
						new() { type = PType.missiles, skin = RightBayEntry.UniqueName, active = false },
					]
				},
				artifacts = [
					new ShieldPrep(),
					new KeplerBayArtifact(),
					new KeplerMissilePDSArtifact(),
				],
				cards = [
					new DodgeColorless(),
					new DroneshiftColorless(),
					new KeplerBasicDroneCard(),
					new KeplerBasicMineCard(),
				],
			},
			UnderChassisSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Kepler/Ship/Chassis.png")).Sprite,
			ExclusiveArtifactTypes = ArtifactTypes.ToHashSet(),
		});
		
		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
	}
}