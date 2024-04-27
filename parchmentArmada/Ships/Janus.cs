using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace parchmentArmada.Ships
{
	internal class Janus : ISpriteManifest, IShipPartManifest, IShipManifest, IStartershipManifest, IStatusManifest, IArtifactManifest, ICardManifest
    {
        public DirectoryInfo? ModRootFolder { get; set; }
        public string Name => "parchmentEngineer.parchmentArmada.JanusManifest";
        public ILogger? Logger { get; set; }
        public IEnumerable<DependencyEntry> Dependencies => new DependencyEntry[0];
        public DirectoryInfo? GameRootFolder { get; set; }

        public static Dictionary<string, ExternalSprite> sprites = new Dictionary<string, ExternalSprite>();
        public static Dictionary<string, ExternalPart> parts = new Dictionary<string, ExternalPart>();
        public static Dictionary<string, ExternalArtifact> artifacts = new Dictionary<string, ExternalArtifact>();
        ExternalShip? janus;
        public static ExternalStatus? solarCharge;
        public static ExternalArtifact? HeliosArtifact;

        public struct StatusPlan
        {
            public int boxWidth;

            public bool asText;

            public bool asBars;

            public int barMax;

            public string? txt;

            public int barTickWidth;

            public StatusDef statusDef;
        }

        private void addSprite(string name, ISpriteRegistry artRegistry)
        {
            if (ModRootFolder == null) throw new Exception("Root Folder not set");
            var path = Path.Combine(ModRootFolder.FullName, "Sprites", "Janus", Path.GetFileName(name + ".png"));
            sprites.Add(name, new ExternalSprite("parchment.armada.janus." + name, new FileInfo(path)));
            artRegistry.RegisterArt(sprites[name]);
        }

        private void addArtifact(string name, string art, string desc, Type artifact, IArtifactRegistry registry)
        {
            artifacts.Add(name, new ExternalArtifact("parchment.armada.janus." + name, artifact, sprites[art], new ExternalGlossary[0], null, null, new string[] { "parchment.armada.Helios" }));
            artifacts[name].AddLocalisation(name, desc, "en");
            registry.RegisterArtifact(artifacts[name]);
        }

        private void addPart(string name, string sprite, PType type, bool flip, IShipPartRegistry registry)
        {
            parts.Add(name, new ExternalPart(
            "parchment.armada.janus." + name,
            new Part()
            {
                active = true,
                damageModifier = PDamMod.none,
                type = type,
                flip = flip
            },
            sprites[sprite] ?? throw new Exception()));
            registry.RegisterPart(parts[name]);
        }

        public void LoadManifest(ISpriteRegistry registry)
        {
			return;
            addSprite("a_cardDupe", registry);
            addSprite("a_powerGift", registry);
            addSprite("p_cannon", registry);
            addSprite("p_cannon2", registry);
            addSprite("p_missiles", registry);
            addSprite("p_cockpit", registry);
            addSprite("p_scaffold", registry);
            addSprite("p_wing", registry);
            addSprite("p_chassis", registry);
            addSprite("s_powerFragment", registry);
        }

        public void LoadManifest(IShipPartRegistry registry)
		{
			return;
			addPart("cannon", "p_cannon", PType.cannon, false, registry);
            addPart("cockpit", "p_cockpit", PType.cockpit, false, registry);
            addPart("scaffold", "p_scaffold", PType.empty, false, registry);
            addPart("missiles", "p_missiles", PType.missiles, false, registry);
            addPart("cannon2", "p_cannon2", PType.special, false, registry);
            addPart("wing", "p_wing", PType.wing, true, registry);
        }

        public void LoadManifest(IShipRegistry shipRegistry)
		{
			return;
			janus = new ExternalShip("parchment.armada.janusShip",
                new Ship()
                {
                    baseDraw = 5,
                    baseEnergy = 3,
                    heatTrigger = 3,
                    heatMin = 0,
                    hull = 7,
                    hullMax = 7,
                    shieldMaxBase = 4
                },
                new ExternalPart[] {
                    parts["missiles"],
                    parts["cannon"],
                    parts["scaffold"],
                    parts["cockpit"],
                    parts["scaffold"],
                    parts["cannon2"],
                    parts["wing"]
                },
                sprites["p_chassis"] ?? throw new Exception(),
                null
                );
            shipRegistry.RegisterShip(janus);
        }
        public void LoadManifest(IStartershipRegistry registry)
		{
			return;
			if (janus == null)
                return;
            var janusShip = new ExternalStarterShip("parchment.armada.janusStarterhsip",
                janus.GlobalName,
                new ExternalCard[] { },
                new ExternalArtifact[] { artifacts["CARD DUPE"], artifacts["POWER GIFT"] },
                new Type[] { typeof(DodgeColorless), typeof(BasicShieldColorless), typeof(CannonColorless), typeof(CannonColorless) },
                new Type[] { new ShieldPrep().GetType() });

            janusShip.AddLocalisation("Janus", "TBA.");
            registry.RegisterStartership(janusShip);
        }

        public void LoadManifest(ICardRegistry registry)
        {

        }

        public void LoadManifest(IStatusRegistry statusRegistry)
        {

        }

        public void LoadManifest(IArtifactRegistry registry)
		{
			return;
			var harmony = new Harmony("parchment.armada.janusHarmony");
            HeliosIconLogic(harmony);

            addArtifact("CARD DUPE", "a_cardDupe", "TBA", typeof(Artifacts.Janus_CardDupe), registry);
            addArtifact("POWER GIFT", "a_powerGift", "TBA", typeof(Artifacts.Janus_PowerGift), registry);
        }

        private void HeliosIconLogic(Harmony harmony)
        {
			//var patch_target_5 = typeof(Ship).GetMethod("GetPartAtLocalX", AccessTools.all);
			//var patch_method_5 = typeof(Helios).GetMethod("HeliosPartFinderPrefix", AccessTools.all);
			//harmony.Patch(patch_target_5, prefix: new HarmonyMethod(patch_method_5));
		}
	}
}
