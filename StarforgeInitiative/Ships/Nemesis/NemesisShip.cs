using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class NemesisShip : IRegisterable
{
	private static readonly List<Type> ArtifactTypes = [
		typeof(NemesisPrepareForTroubleArtifact),
		typeof(NemesisAndMakeItDoubleArtifact),
		typeof(NemesisReactiveShieldArtifact),
		typeof(NemesisThirdWheelArtifact),
	];
	
	private static readonly List<Type> CardTypes = [
		typeof(NemesisBasicDualDroneCard),
	];
	
	private static readonly List<Type> RegisterableTypes = [
		.. ArtifactTypes,
		.. CardTypes,
	];
	
	internal static IShipEntry ShipEntry { get; private set; } = null!;
	internal static IPartEntry LeftCannonCommsEntry { get; private set; } = null!;
	internal static IPartEntry LeftCannonEntry { get; private set; } = null!;
	internal static IPartEntry MidCannonEntry { get; private set; } = null!;
	internal static IPartEntry RightCannonEntry { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		LeftCannonCommsEntry = helper.Content.Ships.RegisterPart("NemesisCannonLeftComms", new()
		{
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Nemesis/Ship/CannonLeftComms.png")).Sprite,
		});
		LeftCannonEntry = helper.Content.Ships.RegisterPart("NemesisCannonLeft", new()
		{
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Nemesis/Ship/CannonLeftActive.png")).Sprite,
			DisabledSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Nemesis/Ship/CannonLeftInactive.png")).Sprite,
		});
		MidCannonEntry = helper.Content.Ships.RegisterPart("NemesisCannonMid", new()
		{
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Nemesis/Ship/CannonMidActive.png")).Sprite,
			DisabledSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Nemesis/Ship/CannonMidInactive.png")).Sprite,
		});
		RightCannonEntry = helper.Content.Ships.RegisterPart("NemesisCannonRight", new()
		{
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Nemesis/Ship/CannonRightActive.png")).Sprite,
			DisabledSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Nemesis/Ship/CannonRightInactive.png")).Sprite,
		});
		
		ShipEntry = helper.Content.Ships.RegisterShip("Nemesis", new()
		{
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Nemesis", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Nemesis", "description"]).Localize,
			Ship = new()
			{
				ship = new()
				{
					hull = 15,
					hullMax = 15,
					shieldMaxBase = 5,
					parts = [
						new() {
							type = PType.wing,
							skin = helper.Content.Ships.RegisterPart("NemesisWingLeft", new()
							{
								Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Nemesis/Ship/WingLeft.png")).Sprite
							}).UniqueName,
							damageModifier = PDamMod.armor,
						},
						new() { type = PType.comms, skin = LeftCannonCommsEntry.UniqueName },
						new() {
							type = PType.missiles,
							skin = helper.Content.Ships.RegisterPart("NemesisBay", new()
							{
								Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Nemesis/Ship/Bay.png")).Sprite
							}).UniqueName,
						},
						new() { type = PType.cannon, skin = MidCannonEntry.UniqueName, active = false },
						new() {
							type = PType.cockpit,
							skin = helper.Content.Ships.RegisterPart("NemesisCockpit", new()
							{
								Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Nemesis/Ship/Cockpit.png")).Sprite
							}).UniqueName,
						},
						new() { type = PType.cannon, skin = RightCannonEntry.UniqueName },
						new() {
							type = PType.wing,
							skin = helper.Content.Ships.RegisterPart("NemesisWingRight", new()
							{
								Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Nemesis/Ship/WingRight.png")).Sprite
							}).UniqueName,
							damageModifier = PDamMod.armor,
						},
					]
				},
				artifacts = [
					new ShieldPrep(),
					new NemesisPrepareForTroubleArtifact(),
					new NemesisAndMakeItDoubleArtifact(),
				],
				cards = [
					new CannonColorless(),
					new DodgeColorless(),
					new BasicShieldColorless(),
					new NemesisBasicDualDroneCard(),
				],
			},
			UnderChassisSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Nemesis/Ship/Chassis.png")).Sprite,
			ExclusiveArtifactTypes = ArtifactTypes.ToHashSet(),
		});
		
		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
	}
}