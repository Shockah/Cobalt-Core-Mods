using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using FMOD;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace parchmentArmada.Ships
{
	internal class Trinity : ISpriteManifest, IArtifactManifest, ICardManifest, IDeckManifest, IGlossaryManifest, IShipPartManifest, IShipManifest, IStartershipManifest
    {
        public DirectoryInfo? ModRootFolder { get; set; }
        public string Name => "parchment.armada.trinity.manifest";
        public ILogger? Logger { get; set; }
        public IEnumerable<DependencyEntry> Dependencies => new DependencyEntry[0];

        public static Dictionary<string, ExternalSprite> sprites = new Dictionary<string, ExternalSprite>();
        public static Dictionary<string, ExternalPart> parts = new Dictionary<string, ExternalPart>();
        public static Dictionary<string, ExternalArtifact> artifacts = new Dictionary<string, ExternalArtifact>();
        ExternalShip? trinity;

        public DirectoryInfo? GameRootFolder { get; set; }
        public static ExternalDeck? TrinityDeck { get; private set; }
        public static ExternalCard? TrinityStarterCard { get; private set; }
        public static ExternalCard? TrinityRagnarok { get; private set; }
        public static ExternalGlossary? TrinityActivateNextGlossary { get; private set; }

        private void addSprite(string name, ISpriteRegistry artRegistry)
        {
            if (ModRootFolder == null) throw new Exception("Root Folder not set");
            var path = Path.Combine(ModRootFolder.FullName, "Sprites", Path.GetFileName(name + ".png"));
            sprites.Add(name, new ExternalSprite("parchment.armada.trinity." + name, new FileInfo(path)));
            artRegistry.RegisterArt(sprites[name]);
        }

        private void addPart(string name, string sprite, PType type, bool flip, IShipPartRegistry registry)
        {
            parts.Add(name, new ExternalPart(
            "parchment.armada.trinity." + name,
            new Part() { active = true, damageModifier = PDamMod.none, type = type, flip = flip },
            sprites[sprite] ?? throw new Exception()));
            registry.RegisterPart(parts[name]);
        }

        private void addArtifact(string name, string art, string desc, Type artifact, IArtifactRegistry registry)
        {
            artifacts.Add(name, new ExternalArtifact("parchment.armada.trinity." + name, artifact, sprites[art], new ExternalGlossary[0], null, null, new string[] { "parchment.armada.Trinity.Ship" }));
            artifacts[name].AddLocalisation(name, desc, "en");
            registry.RegisterArtifact(artifacts[name]);
        }

        public void LoadManifest(ISpriteRegistry artRegistry)
        {
            addSprite("trinity_cannon", artRegistry);
            addSprite("trinity_cannon_off", artRegistry);
            addSprite("trinity_cockpit", artRegistry);
            addSprite("trinity_missiles", artRegistry);
            addSprite("trinity_scaffold", artRegistry);
            addSprite("trinity_chassis", artRegistry);
            addSprite("trinity_cardframe", artRegistry);
            addSprite("trinity_trivenge", artRegistry);
            addSprite("trinity_trihelix", artRegistry);
            addSprite("trinity_mini", artRegistry);
            addSprite("trinity_hardenedRage", artRegistry);
            addSprite("trinity_trirag", artRegistry);
        }

        public void LoadManifest(IShipPartRegistry registry)
        {
            parts.Add("cannon", new ExternalPart(
            "parchment.armada.Trinity.Cannon",
            new Part()
            {
                active = false,
                damageModifier = PDamMod.none,
                type = PType.cannon,
            },
            sprites["trinity_cannon"],
            sprites["trinity_cannon_off"]));
            registry.RegisterPart(parts["cannon"]);

            addPart("cockpit", "trinity_cockpit", PType.cockpit, false, registry);
            addPart("missiles", "trinity_missiles", PType.missiles, false, registry);
            addPart("scaffold", "trinity_scaffold", PType.empty, false, registry);
        }

        

        public void LoadManifest(IShipRegistry shipRegistry)
        {
            trinity = new ExternalShip("parchment.armada.Trinity.Ship",
                new Ship()
                {
                    baseDraw = 5,
                    baseEnergy = 3,
                    heatTrigger = 3,
                    heatMin = 0,
                    hull = 10,
                    hullMax = 10,
                    shieldMaxBase = 5
                },
                new ExternalPart[] { 
                    parts["missiles"],
                    parts["cockpit"],
                    parts["scaffold"],
                    parts["cannon"],
                    parts["cannon"],
                    parts["cannon"]
                },
                sprites["trinity_chassis"] ?? throw new Exception(),
                null
                );
            shipRegistry.RegisterShip(trinity);
        }

        public void LoadManifest(IStartershipRegistry registry)
        {
            if (trinity == null)
                return;
            var trinityShip = new ExternalStarterShip("parchment.armada.Trinity.Ship",
                trinity.GlobalName,
                new ExternalCard[] { TrinityStarterCard ?? throw new Exception(), TrinityStarterCard, TrinityStarterCard },
                new ExternalArtifact[] { artifacts["TRI-VENGE"], artifacts["FUEL HELIX"] },
                new Type[] { typeof(DodgeColorless), typeof(CannonColorless), typeof(CannonColorless) },
                new Type[] { typeof(ShieldPrep) });

            trinityShip.AddLocalisation("Trinity", "A powerful, triple-barreled ship whose unique cannons only become active when put under stress.");
            registry.RegisterStartership(trinityShip);
        }

        void IGlossaryManifest.LoadManifest(IGlossaryRegisty registry)
        {
            TrinityActivateNextGlossary = new ExternalGlossary("parchment.armada.glossary.ATrinityActivateNext", "ATrinityActivateNext", false, ExternalGlossary.GlossayType.action, sprites["trinity_mini"]);
            TrinityActivateNextGlossary.AddLocalisation("en", "Activate Cannon", "Activate your {0}leftmost inactive cannon{1}.", null);
            registry.RegisterGlossary(TrinityActivateNextGlossary);
        }

        public void LoadManifest(IArtifactRegistry registry)
        {
            var harmony = new Harmony("parchment.armada.Trinity");
            TrivengeLogic(harmony);

            addArtifact("TRI-VENGE", "trinity_trivenge", "<c=downside>Your cannons are inactive.</c> When a cannon is hit by an attack, activate it until the end of the turn.", typeof(Artifacts.TrinityTrivenge), registry);
            addArtifact("FUEL HELIX", "trinity_trihelix", "Draw one more card each turn.", typeof(Artifacts.TrinityTrihelix), registry);
            addArtifact("HARDENED RAGE", "trinity_hardenedRage", "(Trinity-exclusive artifact!)\nOn pickup, your <c=part>cannons</c> gain <c=parttrait>armor</c>. <c=downside>Lose 2 max shield on pickup.</c>", typeof(Artifacts.TrinityArmorCannons), registry);
            addArtifact("TRI-RAG", "trinity_trirag", "(Trinity-exclusive artifact!)\nAt the start of turn 3, gain a <c=card>Ragnarok</c>", typeof(Artifacts.TrinityTrirag), registry);
        }
        
        private void TrivengeLogic(Harmony harmony)
        {
            var damage_method = typeof(Ship).GetMethod("NormalDamage") ?? throw new Exception("Couldnt find Ship.NormalDamage method");
            var damage_pre = typeof(Trinity).GetMethod("TrivengeNormalDamagePatch", AccessTools.all) ?? throw new Exception("Couldnt find TrinityManifest.TrivengeNormalDamagePatch method");
            harmony.Patch(damage_method, prefix: new HarmonyMethod(damage_pre));

            var target_method_2 = typeof(ArtifactReward).GetMethod("GetBlockedArtifacts", AccessTools.all);
            var target_patch_2 = typeof(Trinity).GetMethod("AllBossArtifactLogic", AccessTools.all);
            harmony.Patch(target_method_2, postfix: new HarmonyMethod(target_patch_2));
        }

        private static void AllBossArtifactLogic(State s, ref HashSet<Type> __result)
        {
            if (s.ship.key != "parchment.armada.Trinity.Ship")
            {
                __result.Add(typeof(Artifacts.TrinityArmorCannons));
                __result.Add(typeof(Artifacts.TrinityTrirag));
            }
            if (s.ship.key != "parchment.armada.erisStarter")
            {
                __result.Add(typeof(Artifacts.ErisDroneHubV2));
                __result.Add(typeof(Artifacts.ErisAneris));
            }
        }

        private static bool TrivengeNormalDamagePatch(Ship __instance, State s, Combat c, int incomingDamage, int? maybeWorldGridX, bool piercing = false)
        {
            foreach(Artifact artifact in s.artifacts)
            {
                if(artifact.Name() == "TRI-VENGE" && __instance.isPlayerShip)
                {
					if (maybeWorldGridX is null)
						continue;

                    Part? part = s.ship.GetPartAtWorldX(maybeWorldGridX.Value);
                    if (part != null)
                    {
                        if (part.type == PType.cannon && !part.active && incomingDamage > 0)
                        {
                            part.active = true;
                            Audio.Play(new GUID?(FSPRO.Event.TogglePart));
                        }
                    }
                }
            }
            return true;
        }

        public void LoadManifest(ICardRegistry registry)
        {
            TrinityStarterCard = new ExternalCard("parchment.armada.TrinityStarterCard", typeof(Cards.TrinityStarterCard), ExternalSprite.GetRaw((int)StableSpr.cards_GoatDrone), TrinityDeck);
            TrinityStarterCard.AddLocalisation("Assault Bracing");
            registry.RegisterCard(TrinityStarterCard);

            TrinityRagnarok = new ExternalCard("parchment.armada.trinity.ragnarok", typeof(Cards.TrinityRagnarok), ExternalSprite.GetRaw((int)StableSpr.cards_GoatDrone), TrinityDeck);
            TrinityRagnarok.AddLocalisation("Ragnarok");
            registry.RegisterCard(TrinityRagnarok);
        }

        private static System.Drawing.Color TrinityColor = System.Drawing.Color.FromArgb(52, 61, 76);
        public void LoadManifest(IDeckRegistry registry)
        {
            ExternalSprite cardArtDefault = ExternalSprite.GetRaw((int)StableSpr.cards_colorless);
            ExternalSprite borderSprite = sprites["trinity_cardframe"] ?? throw new Exception();
            TrinityDeck = new ExternalDeck(
                "parchment.armada.trinityDeck",
                TrinityColor,
                System.Drawing.Color.White,
                cardArtDefault,
                borderSprite,
                null);
            registry.RegisterDeck(TrinityDeck);
        }
    }
}
