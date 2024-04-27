using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace parchmentArmada.Ships
{
	internal class Eris : ISpriteManifest, IShipPartManifest, IShipManifest, IStartershipManifest, IArtifactManifest, ICardManifest, IDeckManifest, IGlossaryManifest
	{
		public DirectoryInfo? ModRootFolder { get; set; }
		public string Name => "parchment.armada.eris.manifest";
		public ILogger? Logger { get; set; }
		public IEnumerable<DependencyEntry> Dependencies => new DependencyEntry[0];

		public static Dictionary<string, ExternalSprite> sprites = new Dictionary<string, ExternalSprite>();
		public static Dictionary<string, ExternalPart> parts = new Dictionary<string, ExternalPart>();
		public static Dictionary<string, ExternalArtifact> artifacts = new Dictionary<string, ExternalArtifact>();
		ExternalShip? eris;

		public static ExternalCard ErisStrifeEngineCard { get; private set; } = null!;
		public static ExternalCard ErisStrifeForgeCard { get; private set; } = null!;
		public static ExternalCard ErisAnerisCard { get; private set; } = null!;
		public static ExternalDeck ErisDeck { get; private set; } = null!;
		public static ExternalGlossary ErisStrifeEngineGlossary { get; private set; } = null!;
		public static ExternalGlossary ErisAnerisGlossary { get; private set; } = null!;
		public DirectoryInfo? GameRootFolder { get; set; }

		private void addSprite(string name, ISpriteRegistry artRegistry)
		{
			if (ModRootFolder == null) throw new Exception("Root Folder not set");
			var path = Path.Combine(ModRootFolder.FullName, "Sprites", Path.GetFileName(name + ".png"));
			sprites.Add(name, new ExternalSprite("parchment.armada.eris." + name, new FileInfo(path)));
			artRegistry.RegisterArt(sprites[name]);
		}

		private void addPart(string name, string sprite, PType type, bool flip, IShipPartRegistry registry)
		{
			parts.Add(name, new ExternalPart(
			"parchment.armada.eris." + name,
			new Part() { active = true, damageModifier = PDamMod.none, type = type, flip = flip },
			sprites[sprite] ?? throw new Exception()));
			registry.RegisterPart(parts[name]);
		}

		private void addArtifact(string name, string art, string desc, Type artifact, IArtifactRegistry registry)
		{
			artifacts.Add(name, new ExternalArtifact("parchment.armada.eris." + name, artifact, sprites[art], new ExternalGlossary[0], null, null, new string[] { "parchment.armada.erisStarter" }));
			artifacts[name].AddLocalisation(name,  desc, "en");
			registry.RegisterArtifact(artifacts[name]);
		}


		public void LoadManifest(ISpriteRegistry artRegistry)
		{
			addSprite("eris_cannon", artRegistry);
			addSprite("eris_chassis", artRegistry);
			addSprite("eris_cockpit", artRegistry);
			addSprite("eris_missiles", artRegistry);
			addSprite("eris_scaffold", artRegistry);
			addSprite("eris_strifeEngine", artRegistry);
			addSprite("eris_sparkle", artRegistry);
			addSprite("eris_wing", artRegistry);
			addSprite("eris_strifeHub", artRegistry);
			addSprite("eris_discord", artRegistry);
			addSprite("eris_strifeMini", artRegistry);
			addSprite("eris_cardframe", artRegistry);
			addSprite("eris_aneris", artRegistry);
			addSprite("eris_anerisMini", artRegistry);
			addSprite("eris_strifeHubV2", artRegistry);
		}
		public void LoadManifest(IShipPartRegistry registry)
		{
			addPart("cannon", "eris_cannon", PType.cannon, false, registry);
			addPart("cockpit", "eris_cockpit", PType.cockpit, false, registry);
			addPart("missiles", "eris_missiles", PType.missiles, false, registry);
			addPart("scaffold", "eris_scaffold", PType.empty, false, registry);
			addPart("wing", "eris_wing", PType.wing, false, registry);
		}

		public void LoadManifest(IArtifactRegistry registry)
		{
			addArtifact("STRIFE DRONE HUB", "eris_strifeHub", "At the start of combat, gain a <c=card>Strife Engine.</c>", typeof(Artifacts.ErisDroneHub), registry);
			addArtifact("DISCORD", "eris_discord", "<c=downside>Your attacks deal 1 less damage.</c>", typeof(Artifacts.ErisDamageDown), registry);
			addArtifact("ANERIS", "eris_aneris", "(Eris-exclusive artifact!)\nAt the start of combat, gain an <c=card>Aneris.</c>", typeof(Artifacts.ErisAneris), registry);
			addArtifact("STRIFE DRONE HUB V2", "eris_strifeHubV2", "(Eris-exclusive artifact!)\nReplaces <c=artifact>STRIFE DRONE HUB.</c> At the start of combat, gain a <c=card>Strife Forge.</c>", typeof(Artifacts.ErisDroneHubV2), registry);
		}

		public void LoadManifest(ICardRegistry registry)
		{
			ErisStrifeEngineCard = new ExternalCard("parchment.armada.ErisStrifeEngineCard", typeof(Cards.ErisStrifeEngineCard), ExternalSprite.GetRaw((int)StableSpr.cards_GoatDrone), ErisDeck);
			ErisStrifeEngineCard.AddLocalisation("Strife\nEngine");
			registry.RegisterCard(ErisStrifeEngineCard);

			ErisStrifeForgeCard = new ExternalCard("parchment.armada.ErisStrifeForgeCard", typeof(Cards.ErisStrifeForgeCard), ExternalSprite.GetRaw((int)StableSpr.cards_GoatDrone), ErisDeck);
			ErisStrifeForgeCard.AddLocalisation("Strife\nForge");
			registry.RegisterCard(ErisStrifeForgeCard);

			ErisAnerisCard = new ExternalCard("parchment.armada.eris.aneris", typeof(Cards.ErisAnerisCard), ExternalSprite.GetRaw((int)StableSpr.cards_GoatDrone), ErisDeck);
			ErisAnerisCard.AddLocalisation("Aneris");
			registry.RegisterCard(ErisAnerisCard);
		}

		void IGlossaryManifest.LoadManifest(IGlossaryRegisty registry)
		{
			ErisStrifeEngineGlossary = new ExternalGlossary("parchment.armada.glossary.ErisStrifeEngineGlossary", "ErisStrifeEngine", false, ExternalGlossary.GlossayType.action, sprites["eris_strifeMini"] ?? throw new Exception("Miossing Hook Icon"));
			ErisStrifeEngineGlossary.AddLocalisation("en", "Strife Engine", "An indestructable drone. Each turn, shoots {0} shots towards the enemy ship and {1} shots towards your ship, then loses one of each charge. <c=card>When hit, gains charges equal to the hit's damage, plus one.</c> Maximum of 5 charges on each side.", null);
			registry.RegisterGlossary(ErisStrifeEngineGlossary);

			ErisAnerisGlossary = new ExternalGlossary("parchment.armada.glossary.ErisAnerrisGlossary", "ErisAneris", false, ExternalGlossary.GlossayType.action, sprites["eris_anerisMini"] ?? throw new Exception("Miossing Hook Icon"));
			ErisAnerisGlossary.AddLocalisation("en", "Reset Strife Engines", "Remove all charges from all <c=card>Strife Engines.</c>", null);
			registry.RegisterGlossary(ErisAnerisGlossary);
		}

		public void LoadManifest(IShipRegistry shipRegistry)
		{
			eris = new ExternalShip("parchment.armada.erisShip",
				new Ship()
				{
					baseDraw = 5,
					baseEnergy = 3,
					heatTrigger = 3,
					heatMin = 0,
					hull = 8,
					hullMax = 8,
					shieldMaxBase = 3
				},
				new ExternalPart[] {
					parts["wing"],
					parts["scaffold"],
					parts["missiles"],
					parts["cannon"],
					parts["scaffold"],
					parts["cockpit"]
				},
				sprites["eris_chassis"] ?? throw new Exception(),
				null
				);
			shipRegistry.RegisterShip(eris);
		}

		public void LoadManifest(IStartershipRegistry registry)
		{
			if (eris == null)
				return;
			var erisShip = new ExternalStarterShip("parchment.armada.erisStarter",
				eris.GlobalName,
				new ExternalCard[0],
				new ExternalArtifact[] { artifacts["STRIFE DRONE HUB"], artifacts["DISCORD"] },
				new Type[] { typeof(DodgeColorless), typeof(BasicShieldColorless), typeof(CannonColorless), typeof(DroneshiftColorless) },
				new Type[] { new ShieldPrep().GetType() });
			erisShip.AddLocalisation("Eris", "A light scout with exceptionally weak cannons, equipped to spread strife and discord.");
			registry.RegisterStartership(erisShip);
		}

		public void LoadManifest(IDeckRegistry registry)
		{
			ExternalSprite cardArtDefault = ExternalSprite.GetRaw((int)StableSpr.cards_colorless);
			ExternalSprite borderSprite = sprites["eris_cardframe"] ?? throw new Exception();
			ErisDeck = new ExternalDeck(
				"parchment.armada.erisDeck",
				System.Drawing.Color.FromArgb(86,50,88),
				System.Drawing.Color.White,
				cardArtDefault,
				borderSprite,
				null);
			registry.RegisterDeck(ErisDeck);
		}
	}
}
