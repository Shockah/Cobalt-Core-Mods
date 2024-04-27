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
	internal class Helios : ISpriteManifest, IShipPartManifest, IShipManifest, IStartershipManifest, IStatusManifest, IArtifactManifest, ICardManifest
    {
        public DirectoryInfo? ModRootFolder { get; set; }
        public string Name => "parchmentEngineer.parchmentArmada.HeliosManifest";
        public ILogger? Logger { get; set; }
        public IEnumerable<DependencyEntry> Dependencies => new DependencyEntry[0];
        public DirectoryInfo? GameRootFolder { get; set; }

        public static Dictionary<string, ExternalSprite> sprites = new Dictionary<string, ExternalSprite>();
        public static Dictionary<string, ExternalPart> parts = new Dictionary<string, ExternalPart>();
        public static Dictionary<string, ExternalArtifact> artifacts = new Dictionary<string, ExternalArtifact>();
        ExternalShip? helios;
        public static ExternalStatus? solarCharge;
        public static ExternalArtifact? HeliosArtifact;

		public static ExternalCard HeliosHealCard { get; private set; } = null!;

        public static Vec?[] toDraw = new Vec?[20];
        public static int center = -1;
        public static Card? centerCard;
        public static int statusCount = 0;
        public static int handCount = 0;
        public static int heatVal;

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
            var path = Path.Combine(ModRootFolder.FullName, "Sprites", Path.GetFileName(name + ".png"));
            sprites.Add(name, new ExternalSprite("parchment.armada.helios." + name, new FileInfo(path)));
            artRegistry.RegisterArt(sprites[name]);
        }

        private void addArtifact(string name, string art, string desc, Type artifact, IArtifactRegistry registry)
        {
            artifacts.Add(name, new ExternalArtifact("parchment.armada.helios." + name, artifact, sprites[art], new ExternalGlossary[0], null, null, new string[] { "parchment.armada.Helios" }));
            artifacts[name].AddLocalisation(name, desc);
            registry.RegisterArtifact(artifacts[name]);
        }

        private void addPart(string name, string sprite, PType type, bool flip, IShipPartRegistry registry)
        {
            parts.Add(name, new ExternalPart(
            "parchment.armada.helios." + name,
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

        public void LoadManifest(ISpriteRegistry artRegistry)
        {
            addSprite("helios_cannon", artRegistry);
            addSprite("helios_cockpit", artRegistry);
            addSprite("helios_scaffold", artRegistry);
            addSprite("helios_missiles", artRegistry);
            addSprite("helios_chassis", artRegistry);
            addSprite("helios_solar_0", artRegistry);
            addSprite("helios_solar_1", artRegistry);
            addSprite("helios_solar_2", artRegistry);
            addSprite("helios_solar_3", artRegistry);
            addSprite("helios_solar_4", artRegistry);
            addSprite("helios_solar_5", artRegistry);
            addSprite("helios_solar_6", artRegistry);
            addSprite("helios_status", artRegistry);
            addSprite("helios_artifact", artRegistry);
            addSprite("helios_warning1", artRegistry);
            addSprite("helios_warning2", artRegistry);
        }

        public void LoadManifest(IShipPartRegistry registry)
        {
            addPart("cannon","helios_cannon", PType.cannon, false, registry);
            addPart("cockpit","helios_cockpit", PType.cockpit, false, registry);
            addPart("scaffold","helios_scaffold", PType.empty, false, registry);
            addPart("scaffoldf","helios_scaffold", PType.empty, true, registry);
            addPart("missiles","helios_missiles", PType.missiles, false, registry);
            addPart("solar","helios_solar_0", PType.special, false, registry);
            addPart("solarf","helios_solar_0", PType.special, true, registry);
        }

        public void LoadManifest(IShipRegistry shipRegistry)
        {
            helios = new ExternalShip("parchment.armada.Helios",
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
                    parts["cockpit"],
                    parts["scaffold"],
                    parts["scaffoldf"],
                    parts["cannon"],
                    parts["solar"],
                    parts["solarf"],
                    parts["missiles"]
                },
                sprites["helios_chassis"] ?? throw new Exception(),
                null
                );
            shipRegistry.RegisterShip(helios);
        }
        public void LoadManifest(IStartershipRegistry registry)
        {
            if (helios == null)
                return;
            var heliosShip = new ExternalStarterShip("parchment.armada.Helios",
                helios.GlobalName,
                new ExternalCard[] { HeliosHealCard },
                new ExternalArtifact[] { artifacts["MINIATURE STAR"], artifacts["SOLAR VENTS"] },
                new Type[] { typeof(DodgeColorless), typeof(BasicShieldColorless), typeof(CannonColorless), typeof(CannonColorless) },
                new Type[] { new ShieldPrep().GetType() });

            heliosShip.AddLocalisation("Helios", "An experimental ship built around a titanic solar cannon. Overheat to unleash the cannon's full potential.");
            registry.RegisterStartership(heliosShip);
        }

        public void LoadManifest(ICardRegistry registry)
        {
            HeliosHealCard = new ExternalCard("parchment.armada.Helios.healCard", typeof(Cards.HeliosHealCard), ExternalSprite.GetRaw((int)StableSpr.cards_ShieldSurge), null);
            HeliosHealCard.AddLocalisation("Basic Repair");
            registry.RegisterCard(HeliosHealCard);
        }

        public void LoadManifest(IStatusRegistry statusRegistry)
        {
            solarCharge = new ExternalStatus("parchment.armada.solarCharge", true, System.Drawing.Color.Red, null, sprites["helios_status"] ?? throw new Exception("missing sprite"), false);
            statusRegistry.RegisterStatus(solarCharge);
            solarCharge.AddLocalisation("Solar Charge", "Your solar cannon is charging. At 5 charges, it fires a burst of six shots.");
        }

        public void LoadManifest(IArtifactRegistry registry)
        {
            var harmony = new Harmony("parchment.armada.Helios");
            HeliosIconLogic(harmony);

            var spr = sprites["helios_artifact"];
            //HeliosArtifact = new ExternalArtifact(typeof(Artifacts.HeliosArtifact), "parchment.armada.HeliosArtifact", spr, null, new ExternalGlossary[0]);
            //HeliosArtifact.AddLocalisation("SOLAR CANNON", "After you play the centermost card of your hand, lose all remaining energy and gain 1 <c=status>Solar Charge</c> per energy lost. At 5 charges, your solar cannon fires a burst of six 1-damage shots.");
            //registry.RegisterArtifact(HeliosArtifact);
            addArtifact("MINIATURE STAR", "helios_artifact", "<c=downside>Whenever you move, gain 1 HEAT.</c> When you overheat, fire the Helios Cannon, deaing damage equal to twice your <c=status>HEAT</c>. <c=downside>Overheating does 1 additional self-damage for each HEAT over 3.</c>", typeof(Artifacts.HeliosArtifact), registry);
            addArtifact("SOLAR VENTS", "helios_artifact", "At the start of each turn, gain 1 <c=status>SERENITY</c> if you do not have any.", typeof(Artifacts.HeliosHeatsink), registry);
        }
        
        private void HeliosIconLogic(Harmony harmony)
        {
            var patch_method = typeof(Card).GetMethod("Render") ?? throw new Exception("Couldnt find method");
            var patch_target = typeof(Helios).GetMethod("HeliosCardDrawPatch", AccessTools.all) ?? throw new Exception("Couldnt find TrinityManifest.TrivengeNormalDamagePatch method");
            //harmony.Patch(patch_method, prefix: new HarmonyMethod(patch_target));

            var patch_target_2 = typeof(Ship).GetMethod("GetStatusSize", AccessTools.all);
            var patch_method_2 = typeof(Helios).GetMethod("HeliosStatusSizePatch", AccessTools.all);
            //harmony.Patch(patch_target_2, postfix: new HarmonyMethod(patch_method_2));

            var patch_target_3 = typeof(AMove).GetMethod("Begin", AccessTools.all);
            var patch_method_3 = typeof(Helios).GetMethod("HeliosMovePatch", AccessTools.all);
            harmony.Patch(patch_target_3, postfix: new HarmonyMethod(patch_method_3));

            var patch_target_4 = typeof(AOverheat).GetMethod("Begin", AccessTools.all);
            var patch_method_4 = typeof(Helios).GetMethod("HeliosHeatPrefix", AccessTools.all);
            harmony.Patch(patch_target_4, prefix: new HarmonyMethod(patch_method_4));

			//var patch_target_5 = typeof(Ship).GetMethod("GetPartAtLocalX", AccessTools.all);
			//var patch_method_5 = typeof(Helios).GetMethod("HeliosPartFinderPrefix", AccessTools.all);
			//harmony.Patch(patch_target_5, prefix: new HarmonyMethod(patch_method_5));
		}

		private static void HeliosMovePatch(G g, State s, Combat c, ref AMove __instance)
        {
            foreach (Artifact artifact in g.state.artifacts)
            {
                if (artifact.Name() == "MINIATURE STAR" && __instance.targetPlayer)
                {
                    c.QueueImmediate(new AStatus { targetPlayer = true, status = Status.heat, statusAmount = 1, artifactPulse=artifact.Key() });
                }
            }
        }

        private static void HeliosHeatPrefix(G g, State s, Combat c, ref AOverheat __instance)
        {
            foreach (Artifact artifact in g.state.artifacts)
            {
                if (artifact.Name() == "MINIATURE STAR" && __instance.targetPlayer)
                {
                    for (int i = 0; i < s.ship.Get(Status.heat)-3; i++)
                    {
                        s.ship.DirectHullDamage(s, c, 1);
                    }
                    c.Queue(new CardActions.AHeliosFireLaser() { artifactPulse = artifact.Key() });
                    for (int i=0; i<s.ship.Get(Status.heat); i++ ) {
                        c.Queue(new CardActions.AHeliosLaserFx { });
                        c.Queue(new AAttack() { damage = 1, fast = true, piercing = true });
                    }
                    c.Queue(new CardActions.AHeliosResetLaser() { });
                }
            }
        }

        private static void HeliosStatusSizePatch(G g, Status status, int amount, ref object __result)
        {
            var shipType = typeof(Ship);
            var statusPlanType = AccessTools.Inner(shipType, "StatusPlan")!;
            var asTextField = AccessTools.Field(statusPlanType, "asText")!;
            asTextField.SetValue(__result, false);

            /*if (status == (Status)(Helios.solarCharge.Id))
            {
                __result.asText = false;
                __result.asBars = true;
                __result.barMax = 5;
                __result.boxWidth = 17 + __result.barMax * (__result.barTickWidth + 1);
            }*/

        }

        private static bool HeliosCardDrawPatch(Card __instance, G g, Vec? posOverride = null, State? fakeState = null, bool ignoreAnim = false, bool ignoreHover = false, bool hideFace = false, bool hilight = false, bool showRarity = false, bool autoFocus = false, UIKey? keyOverride = null, OnMouseDown? onMouseDown = null, OnMouseDownRight? onMouseDownRight = null, OnInputPhase? onInputPhase = null, double? overrideWidth = null, UIKey? leftHint = null, UIKey? rightHint = null, UIKey? upHint = null, UIKey? downHint = null, int? renderAutopilot = null, bool? forceIsInteractible = null, bool reportTextBoxesForLocTest = false)
        {
            /*foreach (Vec vec in toDraw)
            {
                if(vec != null) {
                    double x = vec.x + vec.x;
                    double y = vec.y + vec.y - 1.0;
                    Draw.Rect(x, y, x + 21, y + 21, new Color(255, 0, 0));
                }
            }
            if(center > -1 && !(centerCard is null)) {
                //Rect rect = centerCard.GetScreenRect();
                double x = rect.x + centerCard.pos.x;
                double y = rect.y + centerCard.pos.y;
                double w = rect.w;
                double h = rect.h;
                //Draw.Rect(x,y,w,h, new Color(255, 0, 0));
            }*/

            Vec vec = posOverride ?? __instance.pos;
            Rect rect = (__instance.GetScreenRect() + vec + new Vec(0.0, __instance.hoverAnim * -2.0 + Mutil.Parabola(__instance.flipAnim) * -10.0 + Mutil.Parabola(Math.Abs(__instance.flopAnim)) * -10.0 * (double)Math.Sign(__instance.flopAnim))).round();
            //double yoff = Math.Min(Math.Max(Math.Abs(__instance.targetPos.x - __instance.pos.x)-10,0)*6, 10);
            double yoff = 0;
            //Draw.Rect(vec.x, vec.y, 300, 3, new Color(255,0,0));
            if (handCount > 8)
            {
                foreach (Artifact artifact in g.state.artifacts)
                {
                    if (artifact.Name() == "SOLAR CANNON")
                    {
                        Vec tPos = __instance.targetPos;
                        if (tPos.x > 190 && tPos.x < 220 && tPos.y > 120)
                        {
                            Spr spr = statusCount >= 9 ? (Spr)sprites["helios_warning2"].Id! : (Spr)sprites["helios_warning1"].Id!;
                            Draw.Sprite(spr, rect.x + 27, rect.y + 18 + yoff);
                        }
                    }
                }
            } else
            {
                foreach (Artifact artifact in g.state.artifacts)
                {
                    if (artifact.Name() == "SOLAR CANNON")
                    {
                        Vec tPos = __instance.targetPos;
                        if (tPos.x > 170 && tPos.x < 240 && tPos.y > 120)
                        {
                            Spr spr = statusCount >= 9 ? (Spr)sprites["helios_warning2"].Id! : (Spr)sprites["helios_warning1"].Id!;
                            Draw.Sprite(spr, rect.x + 27, rect.y + 18 + yoff);
                        }
                    }
                }
            }
            

            //Draw.Rect(0, 0, 20, 20, new Color(255, 0, 0));
            return true;
        }
    }
}
