using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using parchmentArmada.Artifacts;
using parchmentArmada.Cards;
using System;
using System.Collections.Generic;
using System.IO;

namespace parchmentArmada.Ships
{
	internal class Proteus : ISpriteManifest, IShipPartManifest, IArtifactManifest, IShipManifest, IStartershipManifest, ICardManifest, IGlossaryManifest, IDeckManifest
    {

        public DirectoryInfo? ModRootFolder { get; set; }
        public string Name => "parchment.armada.proteus";
        public IEnumerable<DependencyEntry> Dependencies => new DependencyEntry[0];
        public DirectoryInfo? GameRootFolder { get; set; }
        public ILogger? Logger { get; set; }

        private Random rnd = new Random();

        public static Dictionary<string, ExternalSprite> sprites = new Dictionary<string, ExternalSprite>();
        public static Dictionary<string, ExternalPart> parts = new Dictionary<string, ExternalPart>();
        public static Dictionary<string, ExternalArtifact> artifacts = new Dictionary<string, ExternalArtifact>();
        public static Dictionary<string, ExternalCard> cards = new Dictionary<string, ExternalCard>();
        public static Dictionary<string, ExternalGlossary> glossary = new Dictionary<string, ExternalGlossary>();
        public static ExternalDeck? ProteusDeck { get; private set; }
        ExternalShip? proteus;

        private void addSprite(string name, ISpriteRegistry artRegistry)
        {
            if (ModRootFolder == null) throw new Exception("Root Folder not set");
            var path = Path.Combine(ModRootFolder.FullName, "Sprites", Path.GetFileName(name + ".png"));
            sprites.Add(name, new ExternalSprite("parchment.armada.proteus." + name, new FileInfo(path)));
            artRegistry.RegisterArt(sprites[name]);
        }

        private void addPart(string name, string sprite, PType type, bool flip, IShipPartRegistry registry)
        {
            //bool flips = rnd.Next(0, 2) == 1;
            parts.Add(name, new ExternalPart(
            "parchment.armada.proteus." + name,
            new Part() { active = true, damageModifier = PDamMod.none, type = type, flip = flip },
            sprites[sprite] ?? throw new Exception()));
            registry.RegisterPart(parts[name]);
        }

        private void addArtifact(string name, string art, string desc, Type artifact, IArtifactRegistry registry)
        {
            artifacts.Add(name, new ExternalArtifact("parchment.armada.proteus." + name, artifact, sprites[art], new ExternalGlossary[0]));
            artifacts[name].AddLocalisation(name, desc);
            registry.RegisterArtifact(artifacts[name]);
        }

        private void addCard(string name, string printedName, Type card, ICardRegistry registry)
        {
            cards.Add(name, new ExternalCard("parchment.armada.proteus." + name, card, ExternalSprite.GetRaw((int)StableSpr.cards_colorless), ProteusDeck));
            cards[name].AddLocalisation(printedName);
            registry.RegisterCard(cards[name]);
        }

        private void addGlossary(string name, string printedName, string sprite, string desc, IGlossaryRegisty registry)
        {
            glossary.Add(name, new ExternalGlossary("parchment.armada.glossary." + name, name, false, ExternalGlossary.GlossayType.action, sprites[sprite]));
            glossary[name].AddLocalisation("en", printedName, desc, null);
            registry.RegisterGlossary(glossary[name]);
        }

        public void LoadManifest(ISpriteRegistry artRegistry)
        {
            addSprite("proteus_cannon", artRegistry);
            addSprite("proteus_cockpit", artRegistry);
            addSprite("proteus_missiles", artRegistry);
            addSprite("proteus_scaffold", artRegistry);
            addSprite("proteus_wing", artRegistry);
            addSprite("proteus_armor", artRegistry);
            addSprite("proteus_thrusters", artRegistry);
            addSprite("proteus_fuel", artRegistry);
            addSprite("proteus_cannon2", artRegistry);
            addSprite("proteus_cannon3", artRegistry);
            addSprite("proteus_missiles2", artRegistry);
            addSprite("proteus_thrusters2", artRegistry);
            addSprite("proteus_reactor", artRegistry);

            addSprite("proteus_target0", artRegistry);
            addSprite("proteus_target1", artRegistry);
            addSprite("proteus_target2", artRegistry);
            addSprite("proteus_target3", artRegistry);
            addSprite("proteus_target4", artRegistry);
            addSprite("proteus_target5", artRegistry);
            addSprite("proteus_target6", artRegistry);
            addSprite("proteus_target7", artRegistry);

            addSprite("proteus_type_cannon", artRegistry);
            addSprite("proteus_type_cockpit", artRegistry);
            addSprite("proteus_type_missiles", artRegistry);
            addSprite("proteus_type_armor", artRegistry);
            addSprite("proteus_type_scaffold", artRegistry);
            addSprite("proteus_type_thrusters", artRegistry);
            addSprite("proteus_type_fuel", artRegistry);
            addSprite("proteus_type_cannon2", artRegistry);
            addSprite("proteus_type_cannon3", artRegistry);
            addSprite("proteus_type_missiles2", artRegistry);
            addSprite("proteus_type_thrusters2", artRegistry);
            addSprite("proteus_type_reactor", artRegistry);
            addSprite("proteus_type_unknown", artRegistry);

            addSprite("proteus_chassis", artRegistry);
            addSprite("proteus_blank", artRegistry);
            addSprite("proteus_startingArtifact", artRegistry);
            addSprite("proteus_cardframe", artRegistry);
        }

        public void LoadManifest(IShipPartRegistry registry)
        {
            addPart("cannon", "proteus_cannon", PType.cannon, false, registry);
            addPart("cockpit", "proteus_cockpit", PType.cockpit, false, registry);
            addPart("missiles", "proteus_missiles", PType.missiles, false, registry);
            addPart("scaffold", "proteus_scaffold", PType.empty, false, registry);
            addPart("wing", "proteus_wing", PType.wing, false, registry);
            addPart("armor", "proteus_armor", PType.wing, false, registry);
            addPart("thrusters", "proteus_thrusters", PType.wing, false, registry);
            addPart("fuel", "proteus_fuel", PType.wing, false, registry);
            addPart("cannon2", "proteus_cannon2", PType.cannon, false, registry);
            addPart("cannon3", "proteus_cannon3", PType.wing, false, registry);
            addPart("missiles2", "proteus_missiles2", PType.missiles, false, registry);
            addPart("thrusters2", "proteus_thrusters2", PType.wing, false, registry);
            addPart("reactor", "proteus_reactor", PType.wing, false, registry);
        }

        public void LoadManifest(IArtifactRegistry registry)
        {
            var harmony = new Harmony("parchment.armada.harmony.proteus");
            ProteusIconLogic(harmony);

            addArtifact("NANO SWARM", "proteus_startingArtifact", "At the start of each turn, gain a random <c=card>Reconfigure.</c>", typeof(Artifacts.ProteusStartingArtifact), registry);
        }

        public void LoadManifest(ICardRegistry registry)
        {
            addCard("mutator", "Reconfigure", typeof(Cards.ProteusMutator), registry);
            addCard("missileCard", "Eject Waste", typeof(Cards.ProteusMissileCard), registry);
        }

        public void LoadManifest(IGlossaryRegisty registry)
        {
            addGlossary("target0", "      Target Part", "proteus_target0", "If this card is within the first 7 cards of your hand, it will transform the ship part in the same position.", registry);
            addGlossary("target1", "      Target Part", "proteus_target1", "The first part from the left will be transformed.", registry);
            addGlossary("target2", "      Target Part", "proteus_target2", "The second part from the left will be transformed.", registry);
            addGlossary("target3", "      Target Part", "proteus_target3", "The third part from the left will be transformed.", registry);
            addGlossary("target4", "      Target Part", "proteus_target4", "The fourth part from the left will be transformed.", registry);
            addGlossary("target5", "      Target Part", "proteus_target5", "The fifth part from the left will be transformed.", registry);
            addGlossary("target6", "      Target Part", "proteus_target6", "The sixth part from the left will be transformed.", registry);
            addGlossary("target7", "      Target Part", "proteus_target7", "The seventh part from the left will be transformed.", registry);

            addGlossary("typexarmor", "      Armor Unit", "proteus_type_armor", "At the end of your turn, exhaust this card and transform a target ship part into an <c=part>Armor Unit.</c> <c=faint>(Amored, gives +1 Temp Shield at the start of each turn)</c>", registry);
            addGlossary("typexcannon", "      Cannon", "proteus_type_cannon", "At the end of your turn, exhaust this card and transform a target ship part into a <c=part>Cannon.</c>", registry);
            addGlossary("typexcannon2", "      Backup Cannon", "proteus_type_cannon2", "At the end of your turn, exhaust this card and transform a target ship part into a <c=part>Backup Cannon.</c> <c=faint>(Fragile, but otherwise normal cannon)</c>", registry);
            addGlossary("typexcockpit", "      Cockpit", "proteus_type_cockpit", "At the end of your turn, exhaust this card and transform a target ship part into a <c=part>Cockpit.</c>", registry);
            addGlossary("typexfuel", "      Fuel Tanks", "proteus_type_fuel", "At the end of your turn, exhaust this card and transform a target ship part into a <c=part>Fuel Canister.</c> <c=faint>(At the start of your turn, draw a card)</c>", registry);
            addGlossary("typexmissiles", "      Missile Bay", "proteus_type_missiles", "At the end of your turn, exhaust this card and transform a target ship part into a <c=part>Missile Bay.</c>", registry);
            addGlossary("typexmissiles2", "      Backup Missile Bay", "proteus_type_missiles2", "At the end of your turn, exhaust this card and transform a target ship part into a <c=part>Backup Missile Bay.</c> <c=faint>(Fragile, but otherwise normal missile bay)</c>", registry);
            addGlossary("typexreactor", "      Reactor", "proteus_type_reactor", "At the end of your turn, exhaust this card and transform a target ship part into a <c=part>Reactor.</c> <c=faint>(Brittle, gives +1 energy per turn)</c>", registry);
            addGlossary("typexscaffold", "      Scaffold", "proteus_type_scaffold", "At the end of your turn, exhaust this card and transform a target ship part into a <c=part>Scaffold.</c>", registry);
            addGlossary("typexthrusters", "      Thrusters (R)", "proteus_type_thrusters", "At the end of your turn, exhaust this card and transform a target ship part into a <c=part>Thrusters (R).</c> <c=faint>(At the start of your turn, move 1 space right)</c>", registry);
            addGlossary("typexthrusters2", "      Thrusters (L)", "proteus_type_thrusters2", "At the end of your turn, exhaust this card and transform a target ship part into a <c=part>Thrusters (L).</c> <c=faint>(At the start of your turn, move 1 space left)</c>", registry);
            addGlossary("typexunknown", "      Chosen Part", "proteus_type_unknown", "At the end of your turn, exhaust this and transform a target ship part into the part shown. Always gives a <c=part>Cannon, Missile Bay, or Cockpit</c> if you don't have one.", registry);
        }
        
        private static System.Drawing.Color ProteusColor = System.Drawing.Color.FromArgb(183,183,183);
        public void LoadManifest(IDeckRegistry registry)
        {
            ExternalSprite cardArtDefault = ExternalSprite.GetRaw((int)StableSpr.cards_colorless);
            ExternalSprite borderSprite = sprites["proteus_cardframe"] ?? throw new Exception();
            ProteusDeck = new ExternalDeck(
                "parchment.armada.proteus.deck",
                ProteusColor,
                System.Drawing.Color.Black,
                cardArtDefault,
                borderSprite,
                null);
            registry.RegisterDeck(ProteusDeck);
        }

        public void LoadManifest(IShipRegistry shipRegistry)
        {
            proteus = new ExternalShip("parchment.armada.proteus.ship",
                new Ship()
                {
                    baseDraw = 5,
                    baseEnergy = 3,
                    heatTrigger = 3,
                    heatMin = 0,
                    hull = 10,
                    hullMax = 10,
                    shieldMaxBase = 4
                },
                new ExternalPart[] {
                    parts["wing"],
                    parts["wing"],
                    parts["wing"],
                    parts["wing"],
                    parts["wing"],
                    parts["wing"],
                    parts["wing"]
                },
                sprites["proteus_blank"] ?? throw new Exception(),
                null
                );
            shipRegistry.RegisterShip(proteus);
        }

        public void LoadManifest(IStartershipRegistry registry)
        {
            if (proteus == null)
                return;
            var proteusShip = new ExternalStarterShip("parchment.armada.startership.proteus",
                proteus.GlobalName,
                new ExternalCard[] { cards["missileCard"] },
                new ExternalArtifact[] { artifacts["NANO SWARM"] },
                new Type[] { typeof(DodgeColorless), typeof(BasicShieldColorless), typeof(CannonColorless) },
                new Type[] { typeof(ShieldPrep) });
            proteusShip.AddLocalisation("Proteus", "Less of a ship and more a swarm of independent nanobots, capable of constantly reconfiguring itself.");
			registry.RegisterStartership(proteusShip);
		}

        private void ProteusIconLogic(Harmony harmony)
        {
            var patch_target = typeof(Card).GetMethod("RenderAction", AccessTools.all) ?? throw new Exception("Couldnt find RenderAction method");
            var patch_method = typeof(Proteus).GetMethod("ProteusIconPatch", AccessTools.all) ?? throw new Exception("Couldnt find Proteus method");
            harmony.Patch(patch_target, postfix: new HarmonyMethod(patch_method));

            var patch_target_2 = typeof(Ship).GetMethod("DrawTopLayer", AccessTools.all);
            var patch_method_2 = typeof(Proteus).GetMethod("ProteusHullPatch", AccessTools.all);
            harmony.Patch(patch_target_2, postfix: new HarmonyMethod(patch_method_2));

            var patch_target_3 = typeof(AEndTurn).GetMethod("Begin", AccessTools.all);
            var patch_method_3 = typeof(Proteus).GetMethod("ProteusEndTurnMutate", AccessTools.all);
            harmony.Patch(patch_target_3, prefix: new HarmonyMethod(patch_method_3));
        }

        private static void ProteusEndTurnMutate(G g, State s, Combat c)
        {
            foreach(Card card in c.hand)
            { 
                if( typeof(ProteusMutator) == card.GetType())
                {
                    ProteusMutator card2 = (ProteusMutator)card;
                    c.Queue(new CardActions.AProteusMutate { pos = card2.posInHand, skin = card2.sprite, type = card2.part });
                    c.Queue(new AExhaustOtherCard { uuid = card.uuid });
                    //c.hand.Remove(card);
                    //c.exhausted.Add(card);
                }
            }
        }

        private static void ProteusIconPatch(G g, State state, CardAction action, ref int __result)
        {
            if (action is CardActions.AProteusMutate)
            {
                __result += 13;
            }
            if (action is CardActions.AProteusDisplayPart)
            {
                __result += 10;
            }
        }

        private static void ProteusHullPatch(G g, Vec v, Vec worldPos, Ship __instance)
        {
            if(__instance.isPlayerShip)
            {
                foreach (Artifact artifact in g.state.artifacts)
                {
                    if(typeof(ProteusStartingArtifact) == artifact.GetType())
                    {
                        Vec vec = worldPos + new Vec(__instance.parts.Count * 16 / 2);
                        double xspr = v.x + vec.x - 52;
                        double yspr = v.y + vec.y - 25 - 8;
                        //Draw.Sprite((Spr)sprites["proteus_chassis"].Id, xspr, yspr, flipX: false, flipY: false, 0.0, null);
                    }
                }
            }
        }
    }
}
