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
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
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
						new() { type = PType.wing, skin = "wing_bruiser", damageModifier = PDamMod.armor },
						new() { type = PType.comms, skin = "wing_jupiter_b" },
						new() { type = PType.missiles, skin = "missiles_sandwich" },
						new() { type = PType.cannon, skin = "wing_ares", active = false },
						new() { type = PType.cockpit, skin = "cockpit_goliath" },
						new() { type = PType.cannon, skin = "wing_ares", flip = true },
						new() { type = PType.wing, skin = "wing_bruiser", flip = true, damageModifier = PDamMod.armor },
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
			UnderChassisSprite = StableSpr.parts_chassis_bramblepelt,
			ExclusiveArtifactTypes = ArtifactTypes.ToHashSet(),
		});
		
		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
	}
}