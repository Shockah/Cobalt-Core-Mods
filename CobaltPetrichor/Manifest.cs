using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace CobaltPetrichor
{
	public class Manifest : ISpriteManifest, IArtifactManifest, IGlossaryManifest, IDeckManifest, ICardManifest
	{
		public static Manifest Instance { get; private set; } = null!;
		public DirectoryInfo? ModRootFolder { get; set; }
		public string Name => "parchment.petrichor.manifest";
		public DirectoryInfo? GameRootFolder { get; set; }
		public ILogger? Logger { get; set; }
		public IEnumerable<DependencyEntry> Dependencies => new DependencyEntry[0];

		public static bool moved;
		public static bool hasArmsRace = false;
		public static bool isInRorArtifactSelection = false;
		public static int lastAttack = -1;

		public static bool[] enemyShipStickies = new bool[50];

		public static ExternalDeck DeckCommon { get; private set; } = null!;
		public static ExternalDeck DeckUncommon { get; private set; } = null!;
		public static ExternalDeck DeckRare { get; private set; } = null!;

		public static Dictionary<string, ExternalSprite> sprites = new Dictionary<string, ExternalSprite>();
		public static Dictionary<string, ExternalArtifact> artifacts = new Dictionary<string, ExternalArtifact>();
		public static Dictionary<string, ExternalGlossary> glossary = new Dictionary<string, ExternalGlossary>();
		public static Dictionary<string, ExternalCard> cards = new Dictionary<string, ExternalCard>();

		public Manifest()
		{
			Instance = this;
		}

		private void addSprite(string name, string folder, ISpriteRegistry artRegistry)
		{
			if (ModRootFolder == null) throw new Exception("Root Folder not set");
			var path = Path.Combine(ModRootFolder.FullName, "Sprites", folder, Path.GetFileName(name + ".png"));
			sprites.Add(name, new ExternalSprite("parchment.petrichor." + name, new FileInfo(path)));
			artRegistry.RegisterArt(sprites[name]);
		}

		private void addArtifact(string name, string art, string desc, Type artifact, ExternalDeck? owner, IArtifactRegistry registry)
		{
			artifacts.Add(name, new ExternalArtifact("parchment.petrichor." + name, artifact,  sprites[art], new ExternalGlossary[0], owner));
			artifacts[name].AddLocalisation(name, desc);
			registry.RegisterArtifact(artifacts[name]);
		}

		private void addGlossary(string name, string printedName, string sprite, string desc, IGlossaryRegisty registry)
		{
			glossary.Add(name, new ExternalGlossary("parchment.petrichor." + name, name, false, ExternalGlossary.GlossayType.action, sprites[sprite]));
			glossary[name].AddLocalisation("en", printedName, desc, null);
			registry.RegisterGlossary(glossary[name]);
		}

		private void addCard(string name, string printedName, Type card, ICardRegistry registry)
		{
			cards.Add(name, new ExternalCard("parchment.petrichor." + name, card, ExternalSprite.GetRaw((int)StableSpr.cards_colorless), DeckCommon));
			cards[name].AddLocalisation(printedName);
			registry.RegisterCard(cards[name]);
		}



		public void LoadManifest(ISpriteRegistry registry)
		{
			addSprite("fireShield", "Artifacts", registry);
			addSprite("fireShieldUsed", "Artifacts", registry);
			addSprite("warbanner", "Artifacts", registry);
			addSprite("meatNugget", "Artifacts", registry);
			addSprite("bustlingFungus", "Artifacts", registry);
			addSprite("bustlingFungusUsed", "Artifacts", registry);
			addSprite("stickyBomb", "Artifacts", registry);
			addSprite("mortarTube", "Artifacts", registry);
			addSprite("arcaneBlades", "Artifacts", registry);
			addSprite("hermitsScarf", "Artifacts", registry);
			addSprite("sproutingEgg", "Artifacts", registry);
			addSprite("sproutingEggUsed", "Artifacts", registry);
			addSprite("bundleOfFireworks", "Artifacts", registry);
			addSprite("bundleOfFireworksUsed", "Artifacts", registry);
			addSprite("crowbar", "Artifacts", registry);
			addSprite("paulsGoatHoof", "Artifacts", registry);
			addSprite("soldiersSyringe", "Artifacts", registry);
			addSprite("spikestrip", "Artifacts", registry);
			addSprite("topazBrooch", "Artifacts", registry);
			addSprite("topazBroochUsed", "Artifacts", registry);
			addSprite("lensMakersGlasses", "Artifacts", registry);

			addSprite("redWhip", "Artifacts", registry);
			addSprite("redWhipUsed", "Artifacts", registry);
			addSprite("armsRace", "Artifacts", registry);
			addSprite("timekeepersSecret", "Artifacts", registry);
			addSprite("timekeepersSecretUsed", "Artifacts", registry);
			addSprite("rustyJetpack", "Artifacts", registry);
			addSprite("atgMk1", "Artifacts", registry);
			addSprite("hoopoFeather", "Artifacts", registry);
			addSprite("royalMedalion", "Artifacts", registry);
			addSprite("filialImprinting", "Artifacts", registry);
			addSprite("huntersHarpoon", "Artifacts", registry);
			addSprite("lockedJewel", "Artifacts", registry);
			addSprite("lockedJewelUsed", "Artifacts", registry);
			addSprite("ukulele", "Artifacts", registry);
			addSprite("willOTheWisp", "Artifacts", registry);

			addSprite("beatingEmbryo", "Artifacts", registry);
			addSprite("thallium", "Artifacts", registry);
			addSprite("hardlightAfterburner", "Artifacts", registry);
			addSprite("happiestMask", "Artifacts", registry);
			addSprite("photonJetpack", "Artifacts", registry);
			addSprite("umbrella", "Artifacts", registry);
			addSprite("atgMk2", "Artifacts", registry);
			addSprite("heavenCracker", "Artifacts", registry);
			addSprite("telescopicSight", "Artifacts", registry);
			addSprite("shatteringJustice", "Artifacts", registry);

			addSprite("droneMeatNugget", "Drones", registry);
			addSprite("droneWarbanner", "Drones", registry);

			addSprite("miniMeatNugget", "Drones", registry);
			addSprite("miniWarbanner", "Drones", registry);

			addSprite("mapUES", "Map", registry);
			addSprite("limited", "Map", registry);
			addSprite("friendly", "Map", registry);
			addSprite("cardframe", "Cards", registry);

			for(int i=0; i<9; i++)
			{
				addSprite("rainstorm_"+i, "Map", registry);
			}
		}

		public void LoadManifest(IArtifactRegistry registry)
		{
			addArtifact("FIRE SHIELD", "fireShield", "When you lose 2 or more hull in a single turn, attack for 5 damage.", typeof(Artifacts.Common.FireShield), DeckCommon, registry);
			addArtifact("WARBANNER", "warbanner", "At the start of <c=enemyName>Elite</c> and <c=enemyName>Boss</c> encounters, place a warbanner. You have 1 <c=status>POWERDRIVE</c> while it stands.", typeof(Artifacts.Common.Warbanner), DeckCommon, registry);
			addArtifact("BUSTLING FUNGUS", "bustlingFungus", "At the end of each turn, if you have not moved that turn, gain 1 <c=status>TEMP SHIELD</c>.", typeof(Artifacts.Common.BustlingFungus), DeckCommon, registry);
			addArtifact("MEAT NUGGET", "meatNugget", "At the start of combat, spawn a meat nugget that heals its destroyer for <c=heal>1</c>.", typeof(Artifacts.Common.MeatNugget), DeckCommon, registry);
			//addArtifact("STICKY BOMB", "stickyBomb", "TBA.", typeof(Artifacts.Common.StickyBomb), DeckCommon, registry);
			addArtifact("MORTAR TUBE", "mortarTube", "Every 10 attacks, immedately launch a <c=drone>missile</c> that deals 2 damage.", typeof(Artifacts.Common.MortarTube), DeckCommon, registry);
			addArtifact("SPROUTING EGG", "sproutingEgg", "Gain 1 <c=status>SHIELD</c> each turn until you are hit. Recharges at the start of each combat.", typeof(Artifacts.Common.SproutingEgg), DeckCommon, registry);
			addArtifact("ARCANE BLADES", "arcaneBlades", "During  <c=enemyName>Elite</c> and <c=enemyName>Boss</c> encounters, every 3 turns, gain 3 <c=status>EVADE</c>.", typeof(Artifacts.Common.ArcaneBlades), DeckCommon, registry);
			addArtifact("HERMIT'S SCARF", "hermitsScarf", "At the start of turn 3, gain 2 <c=status>AUTODODGE</c>.", typeof(Artifacts.Common.HermitsScarf), DeckCommon, registry);
			addArtifact("BUNDLE OF FIREWORKS", "bundleOfFireworks", "The first time each combat your hand is empty, summon two <c=drone>seeker missiles</c> that each deal 2 damage.", typeof(Artifacts.Common.BundleOfFireworks), DeckCommon, registry);
			//addArtifact("CROWBAR", "crowbar", "TBA.", typeof(Artifacts.Common.Crowbar), DeckCommon, registry);
			//addArtifact("PAUL'S GOAT HOOF", "paulsGoatHoof", "TBA.", typeof(Artifacts.Common.PaulsGoatHoof), DeckCommon, registry);
			//addArtifact("TOPAZ BROOCH", "topazBrooch", "TBA.", typeof(Artifacts.Common.TopazBrooch), DeckCommon, registry);
			//addArtifact("SPIKESTRIP", "spikestrip", "TBA.", typeof(Artifacts.Common.Spikestrip), DeckCommon, registry);
			//addArtifact("SOLDIER'S SYRINGE", "soldiersSyringe", "TBA.", typeof(Artifacts.Common.SoldiersSyringe), DeckCommon, registry);
			//addArtifact("LENS MAKER'S GLASSES", "lensMakersGlasses", "TBA.", typeof(Artifacts.Common.LensMakersGlasses), DeckCommon, registry);

			addArtifact("RED WHIP", "redWhip", "At the end of each turn, if you have not made any attacks, gain 2 <c=status>EVADE</c>.", typeof(Artifacts.Uncommon.RedWhip), DeckUncommon, registry);
			addArtifact("ATG MISSILE MK. 1", "atgMk1", "Every 5 attacks, gain an <c=card>AtG Missile</c>.", typeof(Artifacts.Uncommon.AtgMk1), DeckUncommon, registry);
			addArtifact("TIMEKEEPER'S SECRET", "timekeepersSecret", "The first time you lose hull each combat, draw <c=status>3 cards</c> and gain <c=energy>3 energy</c>.", typeof(Artifacts.Uncommon.TimekeepersSecret), DeckUncommon, registry);
			addArtifact("ARMS RACE", "armsRace", "Your <c=drone>attack drones</c> deal 2 additional damage. At the start of combat, spawn one nearby.", typeof(Artifacts.Uncommon.ArmsRace), DeckUncommon, registry);
			addArtifact("FILIAL IMPRINTING", "filialImprinting", "At the start of combat, gain a <c=card>Strange Creature</c> that provides <c=status>OVERDRIVE, SHIELD,</c> or<c=status> EVADE</c>.", typeof(Artifacts.Uncommon.FilialImprinting), DeckUncommon, registry);
			addArtifact("HOOPO FEATHER", "hoopoFeather", "At the start of each turn, if you have no <c=status>EVADE</c>, gain 1 <c=status>HERMES BOOTS</c>.", typeof(Artifacts.Uncommon.HoopoFeather), DeckUncommon, registry);
			addArtifact("HUNTER'S HARPOON", "huntersHarpoon", "Every 10 cards you play, gain 4 <c=status>AUTOPILOT</c>. <c=downside>Whenever you play a card, lose 1 AUTOPILOT</c>.", typeof(Artifacts.Uncommon.HuntersHarpoon), DeckUncommon, registry);
			//addArtifact("UKULELE", "ukulele", "Every 7 attacks arcs to adjacent ship parts, hitting them for 1 damage and <c=action>stunning</c>.", typeof(Artifacts.Uncommon.Ukulele), DeckUncommon, registry);
			addArtifact("LOCKED JEWEL", "lockedJewel", "The first time each turn your hand is empty, gain 3 <c=status>SHIELD</c> and 3 <c=status>TEMP SHIELD</c>.", typeof(Artifacts.Uncommon.LockedJewel), DeckUncommon, registry);
			//addArtifact("ROYAL MEDALION", "royalMedalion", "TBA", typeof(Artifacts.Uncommon.RoyalMedalion), DeckUncommon, registry);
			addArtifact("RUSTY JETPACK", "rustyJetpack", "Gain 4 <c=status>EVADE</c> on the first turn. <c=downside>Spend 1 EVADE at the start of every subsequent turn to move 1 space randomly.</c>", typeof(Artifacts.Uncommon.RustyJetpack), DeckUncommon, registry);
			//addArtifact("WILL O' THE WISP", "willOTheWisp", "TBA", typeof(Artifacts.Uncommon.WillOTheWisp), DeckUncommon, registry);

			addArtifact("BEATING EMBRYO", "beatingEmbryo", "Every 6 cards played, gain 2 <c=status>BOOST</c>.", typeof(Artifacts.Rare.BeatingEmbryo), DeckRare, registry);
			addArtifact("THALLIUM", "thallium", "At the start of combat, apply 2 <c=status>CORRODE</c>.", typeof(Artifacts.Rare.Thallium), DeckRare, registry);
			addArtifact("HARDLIGHT AFTERBURNER", "hardlightAfterburner", "At the start of combat, gain an <c=card>Afterburner</c>.", typeof(Artifacts.Rare.HardlightAfterburner), DeckRare, registry);
			addArtifact("ATG MISSILE MK. 2", "atgMk2", "Every 5 attacks, gain three <c=card>AtG Missiles</c>.", typeof(Artifacts.Rare.AtgMk2), DeckRare, registry);
			addArtifact("UMBRELLA", "umbrella", "On the first turn of <c=enemyName>Elite</c> and <c=enemyName>Boss</c> encounters, gain 2 <c=status>PERFECT GUARD</c> and the enemy loses 5 hull.", typeof(Artifacts.Rare.Umbrella), DeckRare, registry);
			addArtifact("PHOTON JETPACK", "photonJetpack", "Gain 9 <c=status>EVADE</c> on the first turn. <c=downside>Lose 1 EVADE at the start of every subsequent turn.</c>", typeof(Artifacts.Rare.PhotonJetpack), DeckRare, registry);
			//addArtifact("TELESCOPIC SIGHT", "telescopicSight", "TBA", typeof(Artifacts.Rare.TelescopicSight), DeckRare, registry);
			//addArtifact("SHATTERING JUSTICE", "shatteringJustice", "TBA", typeof(Artifacts.Rare.ShatteringJustice), DeckRare, registry);
			//addArtifact("HEAVEN CRACKER", "heavenCracker", "TBA", typeof(Artifacts.Rare.HeavenCracker), DeckRare, registry);
			//addArtifact("HAPPIEST MASK", "happiestMask", "TBA", typeof(Artifacts.Rare.HappiestMask), DeckRare, registry);

			addArtifact("ENEMY SCALING", "rainstorm_0", "Every 10 turns, increase the difficulty. All enemies gain the following effects at the start of each combat:", typeof(Artifacts.EnemyScaling), null, registry);


			var harmony = new Harmony("parchment.petrichor.harmony");
			HarmonyPatches(harmony);
		}

		public void LoadManifest(IGlossaryRegisty registry)
		{
			addGlossary("warbanner", "Warbanner", "miniWarbanner", "When destroyed, lose <c=status>1 POWERDRIVE</c>.", registry);
			addGlossary("meatNugget", "Meat Nugget", "miniMeatNugget", "Destroying it will heal its destroyer's hull for <c=heal>1</c>.", registry);
			addGlossary("limitedHint", "Limited Use", "limited", "Can be played {0} more times before becoming exhausted.", registry);
			addGlossary("friendlyHint", "Friendly", "friendly", "At the end of each turn, this gains a 1 <c=energy>ENERGY</c> discount until played. <c=disabledText>When it is played, randomly change the buff it provides.</c>", registry);

			/*addGlossary("difficulty0", "Very Easy", "limited", "No Effect", registry);
			addGlossary("difficulty1", "Easy", "limited", "The enemy gains +1 max hull.", registry);
			addGlossary("difficulty2", "Medium", "limited", "The enemy gains +1 max hull.\nSummon an <c=drone>attack drone</c> facing towards you.", registry);
			addGlossary("difficulty3", "Hard", "limited", "The enemy gains +4 max hull.\nSummon an <c=drone>attack drone</c> facing towards you.", registry);
			addGlossary("difficulty4", "Very Hard", "limited", "The enemy gains +4 max hull.\nSummon an <c=drone>attack drone</c> facing towards you.\nTwo random enemy parts gain <c=part>armored</c>.", registry);
			addGlossary("difficulty5", "Insane", "limited", "The enemy gains +4 max hull.\nSummon three <c=drone>attack drones</c> facing towards you.\nTwo random enemy parts gain <c=part>armored</c>.", registry);
			addGlossary("difficulty6", "Impossible", "limited", "The enemy gains +4 max hull.\nSummon three <c=drone>attack drones</c> facing towards you.\nTwo random enemy parts gain <c=part>armored</c>.\nThe enemy gains 1 <c=status>POWERDRIVE</c>.", registry);
			addGlossary("difficulty7", "I SEE YOU", "limited", "The enemy gains +9 max hull.\nSummon three <c=drone>attack drones</c> facing towards you.\nTwo random enemy parts gain <c=part>armored</c>.\nThe enemy gains 1 <c=status>POWERDRIVE</c>.", registry);
			addGlossary("difficulty8", "I'M COMING FOR YOU", "limited", "The enemy gains +9 max hull.\nSummon three <c=drone>attack drones</c> facing towards you.\nTwo random enemy parts gain <c=part>armored</c>.\nThe enemy gains 3 <c=status>POWERDRIVE</c>.", registry);
			*/
			addGlossary("difficulty0", "Very Easy", "limited", "No Effect", registry);
			addGlossary("difficulty1", "Easy", "limited", "+1 Max Hull", registry);
			addGlossary("difficulty2", "Medium", "limited", "+1 Max Hull\nOne <c=drone>attack drone</c>", registry);
			addGlossary("difficulty3", "Hard", "limited", "+4 Max Hull\nOne <c=drone>attack drone</c>", registry);
			addGlossary("difficulty4", "Very Hard", "limited", "+4 Max Hull\nOne <c=drone>attack drone</c>\nTwo <c=part>armored</c> parts", registry);
			addGlossary("difficulty5", "Insane", "limited", "+4 Max Hull\nThree <c=drone>attack drones</c>\nTwo <c=part>armored</c> parts", registry);
			addGlossary("difficulty6", "Impossible", "limited", "+4 Max Hull\nThree <c=drone>attack drones</c>\nTwo <c=part>armored</c> parts\n1 <c=status>POWERDRIVE</c>", registry);
			addGlossary("difficulty7", "I SEE YOU", "limited", "+9 Max Hull\nThree <c=drone>attack drones</c>\nTwo <c=part>armored</c> parts\n1 <c=status>POWERDRIVE</c>", registry);
			addGlossary("difficulty8", "I'M COMING FOR YOU", "limited", "+9 Max Hull\nThree <c=drone>attack drones</c>\nTwo <c=part>armored</c> parts\n3 <c=status>POWERDRIVE</c>", registry);
		}

		public void LoadManifest(ICardRegistry registry)
		{
			addCard("cAfterburner", "Afterburner", typeof(Cards.CAfterburner), registry);
			addCard("cAtg", "AtG Missile", typeof(Cards.CAtg), registry);
			addCard("cStrangeCreature", "Strange Creature", typeof(Cards.CStrangeCreature), registry);
		}

		public void LoadManifest(IDeckRegistry registry)
		{
			ExternalSprite cardArtDefault = ExternalSprite.GetRaw((int)StableSpr.cards_colorless);
			ExternalSprite borderSpriteDefault = ExternalSprite.GetRaw((int)StableSpr.cardShared_border_colorless);
			DeckCommon = new ExternalDeck(
				"parchment.petrichor.deck.common",
				System.Drawing.Color.FromArgb(255,255,255),
				System.Drawing.Color.White,
				cardArtDefault,
				sprites["cardframe"],
				null);
			registry.RegisterDeck(DeckCommon);
			DeckUncommon = new ExternalDeck(
				"parchment.petrichor.deck.uncommon",
				System.Drawing.Color.FromArgb(56, 255, 148),
				System.Drawing.Color.Black,
				cardArtDefault,
				borderSpriteDefault,
				null);
			registry.RegisterDeck(DeckUncommon);
			DeckRare = new ExternalDeck(
				"parchment.petrichor.deck.rare",
				System.Drawing.Color.FromArgb(255, 56, 56),
				System.Drawing.Color.Black,
				cardArtDefault,
				borderSpriteDefault,
				null);
			registry.RegisterDeck(DeckRare);
		}

		public void HarmonyPatches(Harmony harmony)
		{
			var target_method_1 = typeof(MapBase).GetMethod("Populate", AccessTools.all) ?? throw new Exception();
			var target_patch_1 = typeof(Manifest).GetMethod("ReplaceArtifactNodePatch", AccessTools.all);
			harmony.Patch(target_method_1, postfix: new HarmonyMethod(target_patch_1));

			var target_method_2 = typeof(AMove).GetMethod("Begin", AccessTools.all) ?? throw new Exception();
			var target_patch_2 = typeof(Manifest).GetMethod("BustlingFungusPatch", AccessTools.all);
			harmony.Patch(target_method_2, postfix: new HarmonyMethod(target_patch_2));

			var target_method_3 = typeof(AttackDrone).GetMethod("AttackDamage", AccessTools.all) ?? throw new Exception();
			var target_patch_3 = typeof(Manifest).GetMethod("ArmsRacePatch", AccessTools.all);
			harmony.Patch(target_method_3, postfix: new HarmonyMethod(target_patch_3));

			var target_method_4 = typeof(ArtifactBrowse).GetMethod("Render", AccessTools.all) ?? throw new Exception();
			var target_patch_4 = typeof(Manifest).GetMethod("ArtifactBrowseTranspilerPatch", AccessTools.all);
			harmony.Patch(target_method_4, transpiler: new HarmonyMethod(target_patch_4));

			var target_method_5 = typeof(Ship).GetMethod("DrawTopLayer", AccessTools.all) ?? throw new Exception();
			var target_patch_5 = typeof(Manifest).GetMethod("StickyBombRenderPatch", AccessTools.all);
			harmony.Patch(target_method_5, postfix: new HarmonyMethod(target_patch_5));

			var target_method_6 = typeof(State).GetMethod("PopulateRun", AccessTools.all) ?? throw new Exception();
			var target_patch_6 = typeof(Manifest).GetMethod("StartingArtifactPatch", AccessTools.all);
			harmony.Patch(target_method_6, postfix: new HarmonyMethod(target_patch_6));

			var target_method_7 = typeof(DB).GetMethod("LoadStringsForLocale", AccessTools.all) ?? throw new Exception();
			var target_patch_7 = typeof(Manifest).GetMethod("DBLoadStringsForLocalePatch", AccessTools.all);
			harmony.Patch(target_method_7, postfix: new HarmonyMethod(target_patch_7));
		}

		private static void ReplaceArtifactNodePatch(ref MapBase __instance, State s, Rand rng)
		{
			MapBase.MapSpawnConfig spawnConfig = __instance.GetSpawnConfig(s, rng);
			for (int i = 0; i < spawnConfig.roomsPerFloor; i++)
			{
				Vec key = new Vec(i, spawnConfig.floorsNotCountingExit / 2);
				if (__instance.markers.TryGetValue(key, out Marker? value))
				{
					value.contents = new MapUES();
				}
				/*for (int j=0; j< spawnConfig.floorsNotCountingExit; j++)
				{
					Vec key = new Vec(i, j);
					if (__instance.markers.TryGetValue(key, out Marker value))
					{
						value.contents = new MapUES();
					}
				}*/
			}
		}

		private static void BustlingFungusPatch(AMove __instance, G g, State s, Combat c)
		{
			if (__instance.targetPlayer)
			{
				foreach (Artifact artifact in s.artifacts)
				{
					if (artifact.GetType() == typeof(Artifacts.Common.BustlingFungus))
					{
						moved = true;
					}
				}
			}
		}

		private static void ArmsRacePatch(ref AttackDrone __instance, ref int __result)
		{
			if(hasArmsRace && !__instance.targetPlayer) { __result += 2; }
		}

		private static IEnumerable<CodeInstruction> ArtifactBrowseTranspilerPatch(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
		{
			try
			{
				return new SequenceBlockMatcher<CodeInstruction>(instructions)
					.Find(
						ILMatches.LdcI4((int)Deck.colorless),
						ILMatches.Call("Contains"),
						ILMatches.Brfalse.GetBranchTarget(out var branchTarget)
					)
					.PointerMatcher(branchTarget)
					.ExtractLabels(out var labels)
					.Insert(
						SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
						new CodeInstruction(OpCodes.Ldloc_2).WithLabels(labels),
						new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactBrowseTranspilerPatchModifyDecks)))
					)
					.AllElements();
			}
			catch (Exception ex)
			{
				Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
				return instructions;
			}
		}

		private static void ArtifactBrowseTranspilerPatchModifyDecks(List<Deck> decks)
		{
			decks.Add((Deck)DeckCommon.Id!);
			decks.Add((Deck)DeckUncommon.Id!);
			decks.Add((Deck)DeckRare.Id!);
		}

		private static void ArtifactBrowseScrollMaxPatch(double dt, ref int maxScroll, ref double scroll, ref double scrollTarget)
		{
			if(true)
			{
				maxScroll += 500;
			}
		}
	
		private static void StickyBombRenderPatch(G g, Vec v, Vec worldPos, Ship __instance)
		{
			if (!__instance.isPlayerShip)
			{
				for (int i = 0; i < __instance.parts.Count; i++)
				{
					if (enemyShipStickies[i])
					{
						Part part = __instance.parts[i];
						Vec vec2 = worldPos + new Vec((double)(i * 16) + part.offset.x + (part.xLerped ?? 0) * 16.0, -32.0 + (__instance.isPlayerShip ? part.offset.y : (1.0 + (0.0 - part.offset.y))));
						Vec vec3 = v + vec2;
						double num2 = (int)vec3.x - 1;
						double y2 = (int)vec3.y - 1 + 37;
						Draw.Sprite((Spr)sprites["stickyBomb"].Id!, num2, y2, false, true, 0.0);
					}
				}
			}
		}

		private static void StartingArtifactPatch(ref State __instance)
		{
			__instance.SendArtifactToChar(new Artifacts.EnemyScaling() { difficulty = 0, turns = 0 }) ;
		}

		private static void DBLoadStringsForLocalePatch(ref Dictionary<string, string>? __result)
		{
			__result ??= [];
			__result[$"char.{((Deck)DeckCommon.Id!).Key()}"] = "Common Items";
			__result[$"char.{((Deck)DeckUncommon.Id!).Key()}"] = "Uncommon Items";
			__result[$"char.{((Deck)DeckRare.Id!).Key()}"] = "Rare Items";
		}
	}
}
