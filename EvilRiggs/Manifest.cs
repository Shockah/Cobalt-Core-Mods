using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using EvilRiggs.Cards.Common;
using EvilRiggs.Cards.Uncommon;
using EvilRiggs.Drones;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace EvilRiggs
{
	public class Manifest : ISpriteManifest, ICardManifest, IDeckManifest, IAnimationManifest, ICharacterManifest, IGlossaryManifest, IStatusManifest, IArtifactManifest, ICustomEventManifest
	{
		public DirectoryInfo? ModRootFolder { get; set; }
		public string Name => "parchment.evilRiggs.manifest";
		public DirectoryInfo? GameRootFolder { get; set; }
		public ILogger? Logger { get; set; }
		public IEnumerable<DependencyEntry> Dependencies => new DependencyEntry[0];

		public static Dictionary<string, ExternalSprite> sprites = new Dictionary<string, ExternalSprite>();
		public static Dictionary<string, ExternalGlossary> glossary = new Dictionary<string, ExternalGlossary>();
		public static Dictionary<string, ExternalCard> cards = new Dictionary<string, ExternalCard>();
		public static Dictionary<string, ExternalStatus> statuses = new Dictionary<string, ExternalStatus>();
		public static Dictionary<string, ExternalArtifact> artifacts = new Dictionary<string, ExternalArtifact>();
		public static ExternalDeck EvilRiggsDeck { get; private set; } = null!;
		public static ExternalCharacter EvilRiggsCharacter { get; private set; } = null!;

		public static ExternalAnimation EvilRiggsDefaultAnim { get; private set; } = null!;
		public static ExternalAnimation EvilRiggsMiniAnim { get; private set; } = null!;
		public static ExternalAnimation GameoverAnimation { get; private set; } = null!;
		public static ExternalAnimation SquintAnimation { get; private set; } = null!;

		private static System.Drawing.Color Evil_Riggs_Color = System.Drawing.Color.FromArgb(255, 156, 95);

		private static StuffBase? FootballBonkContext;

		private void addSprite(string name, ISpriteRegistry artRegistry)
		{
			if (ModRootFolder == null) throw new Exception("Root Folder not set");
			var path = Path.Combine(ModRootFolder.FullName, "Sprites", Path.GetFileName(name + ".png"));
			sprites.Add(name, new ExternalSprite("parchment.evilRiggs." + name, new FileInfo(path)));
			artRegistry.RegisterArt(sprites[name]);
		}

		private void addArtifact(string name, string art, string desc, Type artifact, IArtifactRegistry registry)
		{
			artifacts.Add(name, new ExternalArtifact("parchment.evilRiggs." + name, artifact, sprites[art], new ExternalGlossary[0], EvilRiggsDeck));
			artifacts[name].AddLocalisation(name, desc, "en");
			registry.RegisterArtifact(artifacts[name]);
		}

		private void addCard(string name, string printedName, Type card, ICardRegistry registry)
		{
			cards.Add(name, new ExternalCard("parchment.evilRiggs." + name, card, ExternalSprite.GetRaw((int)StableSpr.cards_colorless), EvilRiggsDeck));
			cards[name].AddLocalisation(printedName);
			registry.RegisterCard(cards[name]);
		}

		private void addGlossary(string name, string printedName, string sprite, string desc, ExternalGlossary.GlossayType glossaryType, IGlossaryRegisty registry)
		{
			glossary.Add(name, new ExternalGlossary("parchment.evilRiggs." + name, name, false, glossaryType, sprites[sprite]));
			glossary[name].AddLocalisation("en", printedName, desc, null);
			registry.RegisterGlossary(glossary[name]);
		}

		private void addStatus(string name, string printedName, string sprite, string desc, System.Drawing.Color color, IStatusRegistry statusRegistry)
		{
			statuses[name] = new ExternalStatus("parchment.evilRiggs." + name, true, color, null, sprites[sprite], false);
			statusRegistry.RegisterStatus(statuses[name]);
			statuses[name].AddLocalisation(printedName, desc);
		}

		public void LoadManifest(ISpriteRegistry artRegistry)
		{
			addSprite("evilRiggs_cardframe", artRegistry);
			addSprite("evilRiggs_panel", artRegistry);
			addSprite("evilRiggs_small", artRegistry);
			addSprite("pirateBoss_gameover", artRegistry);

			addSprite("evilRiggs_status_targetLock", artRegistry);
			addSprite("evilRiggs_status_rage", artRegistry);
			addSprite("evilRiggs_status_barrelRoll", artRegistry);
			addSprite("evilRiggs_status_sequ", artRegistry);
			addSprite("evilRiggs_status_discount", artRegistry);
			addSprite("evilRiggs_status_missing", artRegistry);
			addSprite("evilRiggs_status_engineRedirect", artRegistry);
			addSprite("evilRiggs_status_engineRedirectUsed", artRegistry);

			addSprite("cardart_ragedraw", artRegistry);
			addSprite("cardart_seq_normal_top", artRegistry);
			addSprite("cardart_seq_normal_bottom", artRegistry);
			addSprite("cardart_seq_offset_top", artRegistry);
			addSprite("cardart_seq_offset_bottom", artRegistry);

			addSprite("artifact_placeholder", artRegistry);
			addSprite("artifact_holdThatThought", artRegistry);
			addSprite("artifact_holdThatThoughtUsed", artRegistry);
			addSprite("artifact_spiltBoba", artRegistry);
			addSprite("artifact_spiltBobaUsed", artRegistry);
			addSprite("artifact_swarmPreloader", artRegistry);
			addSprite("artifact_blackPowderTips", artRegistry);
			addSprite("artifact_perpetualSpeedDevice", artRegistry);
			addSprite("artifact_perpetualSpeedDeviceUsed", artRegistry);
			addSprite("artifact_temperedRage", artRegistry);

			addSprite("icon_missile_light", artRegistry);
			addSprite("missile_light", artRegistry);

			string[] emotionList = { "embarrassed", "neutral", "serious", "squint" };
			foreach (string emotion in emotionList)
			{
				for (int i = 0; i < 5; i++)
				{
					addSprite("pirateBoss_" + emotion + "_" + i, artRegistry);
				}
			}
		}

		public void LoadManifest(ICardRegistry registry)
		{
			addCard("skedaddle", "Skedaddle", typeof(Cards.Common.Skedaddle), registry);
			addCard("lightMissile", "Light Missile", typeof(Cards.Common.LightMissile), registry);
			addCard("jostle", "Jostle", typeof(Cards.Common.Jostle), registry);
			addCard("airburst", "Airburst", typeof(Cards.Common.Airburst), registry);
			addCard("dirtDrive", "Dirt Drive", typeof(DirtDrive), registry);
			addCard("burstFire", "Burst Fire", typeof(Cards.Common.BurstFire), registry);
			addCard("bide", "Bide", typeof(Cards.Common.Bide), registry);
			addCard("scheme", "Scheme", typeof(Cards.Common.Scheme), registry);
			addCard("jammedBarrel", "Jammed Barrel", typeof(Cards.Common.JammedBarrel), registry);
			addCard("swerve", "Swerve", typeof(Cards.Common.Swerve), registry);
			addCard("readyOrNot", "Ready Or Not", typeof(Cards.Common.ReadyOrNot), registry);

			addCard("brilliance", "Brilliance", typeof(Cards.Uncommon.Brilliance), registry);
			addCard("targetLockV2", "Target Lock", typeof(Cards.Uncommon.TargetLockV2), registry);
			addCard("allTheButtons", "All The Buttons", typeof(Cards.Uncommon.AllTheButtons), registry);
			addCard("outrage", "Outrage", typeof(Cards.Uncommon.Outrage), registry);
			addCard("steamEngine", "Steam Engine", typeof(Cards.Uncommon.SteamEngine), registry);
			addCard("wildShot", "Wild Shot", typeof(Cards.Uncommon.WildShot), registry);
			//addCard("boomPewPew", "Boom Pew Pew", typeof(Cards.Uncommon.BoomPewPew), registry);
			//addCard("decisions", "Decisions", typeof(Cards.Uncommon.Decisions), registry);
			//addCard("targetLock", "Target Lock", typeof(Cards.Uncommon.TargetLock), registry);

			addCard("noEscape", "No Escape", typeof(Cards.Rare.NoEscape), registry);
			//addCard("rocketSalvo", "Rocket Salvo", typeof(Cards.Rare.RocketSalvo), registry);
			addCard("doABarrelRoll", "Do A Barrel Roll", typeof(Cards.Rare.DoABarrelRoll), registry);
			addCard("finale", "Finale", typeof(Cards.Rare.Finale), registry);
			addCard("strangelove", "Strangelove", typeof(Cards.Rare.Strangelove), registry);
			addCard("engineRedirect", "Engine Redirect", typeof(Cards.Rare.EngineRedirect), registry);
		}

		public void LoadManifest(IArtifactRegistry registry)
		{
			addArtifact("SPILT BOBA", "artifact_spiltBoba", "The first time you are hit each turn, <c=downside>draw 1 less card next turn</c> and gain 2 <c=status>RAGE</c>.", typeof(Artifacts.SpiltBoba), registry);
			addArtifact("SWARM PRELOADER", "artifact_swarmPreloader", "<c=card>Missile Swarm</c> now costs 0 <c=energy>ENERGY</c>.", typeof(Artifacts.SwarmPreLoader), registry);
			addArtifact("HOLD THAT THOUGHT", "artifact_holdThatThought", "The first <c=cardtrait>INFINITE</c> card you play each combat that costs more than <c=energy>0</c> gains <c=cardtrait>RETAIN</c> until the end of combat.", typeof(Artifacts.HoldThatThought), registry);
			//addArtifact("BLACK POWDER", "artifact_blackPowderTips", "Every fourth missile you launch is a <c=midrow>black powder missile</c>.", typeof(Artifacts.BlackPowder), registry);
			//addArtifact("PERPETUAL SPEED DEVICE", "artifact_perpetualSpeedDevice", "The first time each turn you have no cards in your hand, gain 3 <c=status>EVADE</c>.", typeof(Artifacts.PerpetualSpeedDevice), registry);
			addArtifact("TEMPERED RAGE", "artifact_temperedRage", "At the start of your turn, if you have 4 or more <c=status>RAGE</c>, draw 2 additional cards.", typeof(Artifacts.TemperedRage), registry);
		}


		public void LoadManifest(IDeckRegistry registry)
		{
			ExternalSprite cardArtDefault = ExternalSprite.GetRaw((int)StableSpr.cards_colorless);
			ExternalSprite borderSprite = sprites["evilRiggs_cardframe"] ?? throw new Exception();
			EvilRiggsDeck = new ExternalDeck(
				"parchment.evilRiggs.evilRiggsDeck",
				Evil_Riggs_Color,
				System.Drawing.Color.Black,
				cardArtDefault,
				borderSprite,
				null);
			registry.RegisterDeck(EvilRiggsDeck);
		}

		public void LoadManifest(IGlossaryRegisty registry)
		{
			glossary.Add("droneMoveRandom", new ExternalGlossary("action.droneMoveRandom", "droneMoveRandom", false, ExternalGlossary.GlossayType.action, ExternalSprite.GetRaw((int)StableSpr.icons_droneMoveRandom)));
			glossary["droneMoveRandom"].AddLocalisation("en", "Move Midrow Randomly", "Instantly move the midrow {0} spaces in a random direction.", null);
			registry.RegisterGlossary(glossary["droneMoveRandom"]);

			addGlossary("sequentialHint", "Sequential", "evilRiggs_status_sequ", "Activates the top action the first time you play this each turn, and the bottom action on all subsequent plays.", ExternalGlossary.GlossayType.action, registry);
			addGlossary("sequential", "Sequential", "evilRiggs_status_sequ", "Activates the top action the first time you play this each turn, and the bottom action on all subsequent plays.", ExternalGlossary.GlossayType.action, registry);
			addGlossary("targetLock", "Target Lock", "evilRiggs_status_targetLock", "Convert all missiles in the midrow into seeker missiles.", ExternalGlossary.GlossayType.action, registry);
			addGlossary("missileLight", "Light Missile", "icon_missile_light", "This missile is going to deal {0} damage.", ExternalGlossary.GlossayType.midrow, registry);
		}

		public void LoadManifest(IStatusRegistry statusRegistry)
		{
			addStatus("rage", "Rage", "evilRiggs_status_rage", "When your rage hits 7, add a powerful <c=card>Missile Swarm</c> into your hand.", System.Drawing.Color.Red, statusRegistry);
			addStatus("targetLock", "Target Lock", "evilRiggs_status_targetLock", "Your next {0} missiles will be <c=drone>Seeker Missiles</c>.", System.Drawing.Color.Purple, statusRegistry);
			addStatus("barrelRoll", "Barrel Roll", "evilRiggs_status_barrelRoll", "At the start of your turn, move {0} spaces in a random direction.", System.Drawing.Color.Yellow, statusRegistry);
			addStatus("discountNextTurn", "Discount Next Turn", "evilRiggs_status_discount", "At the start of your next turn, the leftmost {0} cards in your hand get a <c=energy>1 ENERGY</c> discount", System.Drawing.Color.Blue, statusRegistry);
			addStatus("engineRedirect", "Engine Redirect", "evilRiggs_status_engineRedirect", "The first {0} times you move each turn, draw a card.", System.Drawing.Color.Blue, statusRegistry);
			addStatus("engineRedirectUsed", "Engine Redirect (Used)", "evilRiggs_status_engineRedirectUsed", "You have drawn {0} cards from <c=status>Engine Redirect</c> this turn.", System.Drawing.Color.Gray, statusRegistry);

			var harmony = new Harmony("parchment.evilRiggs.harmony");
			EvilRiggsPatchLogic(harmony);
		}

		void IAnimationManifest.LoadManifest(IAnimationRegistry registry)
		{
			EvilRiggsDefaultAnim = new ExternalAnimation("parchment.EvilRiggs.anims.default",
				EvilRiggsDeck ?? throw new Exception("missing deck"),
				"neutral", false,
				new ExternalSprite[] { sprites["pirateBoss_neutral_0"] ?? throw new Exception("missing potrait") });

			registry.RegisterAnimation(EvilRiggsDefaultAnim);

			EvilRiggsMiniAnim = new ExternalAnimation("parchment.EvilRiggs.anims.mini",
			   EvilRiggsDeck ?? throw new Exception("missing deck"),
			   "mini", false,
			   new ExternalSprite[] { sprites["evilRiggs_small"] ?? throw new Exception("missing mini") });

			registry.RegisterAnimation(EvilRiggsMiniAnim);


			GameoverAnimation = new ExternalAnimation("parchment.EvilRiggs.anims.GameOver", EvilRiggsDeck, "gameover", false, new ExternalSprite[] { sprites["pirateBoss_gameover"] });
			registry.RegisterAnimation(GameoverAnimation);


			SquintAnimation = new ExternalAnimation("parchment.EvilRiggs.anims.squint", EvilRiggsDeck, "talk_squint", false, new ExternalSprite[] { sprites["pirateBoss_squint_0"] });
			registry.RegisterAnimation(SquintAnimation);

		}

		void ICharacterManifest.LoadManifest(ICharacterRegistry registry)
		{
			EvilRiggsCharacter = new ExternalCharacter("parchment.EvilRiggs.character",
				EvilRiggsDeck ?? throw new Exception("Missing Deck"),
				sprites["evilRiggs_panel"] ?? throw new Exception("Missing Potrait"),
				new Type[] { typeof(Cards.Common.Skedaddle), typeof(Cards.Common.LightMissile) },
				new Type[0],
				EvilRiggsDefaultAnim ?? throw new Exception("missing default animation"),
				EvilRiggsMiniAnim ?? throw new Exception("missing mini animation"));

			EvilRiggsCharacter.AddNameLocalisation("Riggs?");

			EvilRiggsCharacter.AddDescLocalisation("<c=riggs>RIGGS?</c>\n<c=riggs>Riggs's</c> alternate self. Her cards have a similar focus to <c=riggs>Riggs's</c>, but with more chaos and more missiles.");

			registry.RegisterCharacter(EvilRiggsCharacter);
		}

		void ICustomEventManifest.LoadManifest(ICustomEventHub eventHub)
		{
			eventHub.ConnectToEvent<Func<IManifest, IPrelaunchContactPoint>>("Nickel::OnAfterDbInitPhaseFinished", contactPointProvider =>
			{
				var contactPoint = contactPointProvider(this);

				if (contactPoint.GetApi<IDraculaApi>("Shockah.Dracula") is { } draculaApi)
				{
					draculaApi.RegisterBloodTapOptionProvider((Status)statuses["rage"].Id!.Value, (_, _, status) => [
						new AHurt { targetPlayer = true, hurtAmount = 1 },
						new AStatus { targetPlayer = true, status = status, statusAmount = 5 },
					]);
					draculaApi.RegisterBloodTapOptionProvider((Status)statuses["barrelRoll"].Id!.Value, (_, _, status) => [
						new AHurt { targetPlayer = true, hurtAmount = 1 },
						new AStatus { targetPlayer = true, status = status, statusAmount = 2 },
					]);
					draculaApi.RegisterBloodTapOptionProvider((Status)statuses["engineRedirect"].Id!.Value, (_, _, status) => [
						new AHurt { targetPlayer = true, hurtAmount = 1 },
						new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
					]);
				}

				contactPoint.GetApi<IMoreDifficultiesApi>("TheJazMaster.MoreDifficulties")?.RegisterAltStarters(
					deck: (Deck)EvilRiggsDeck.Id!.Value,
					starterDeck: new StarterDeck
					{
						cards = [
							new JammedBarrel(),
							new Scheme()
						]
					}
				);
			});
		}

		private void EvilRiggsPatchLogic(Harmony harmony)
		{
			var patch_target_1 = typeof(AStatus).GetMethod("Begin", AccessTools.all);
			var patch_method_1 = typeof(Manifest).GetMethod("EvilRiggsRagePatch", AccessTools.all);
			harmony.Patch(patch_target_1, postfix: new HarmonyMethod(patch_method_1));

			var patch_target_2 = typeof(ASpawn).GetMethod("Begin", AccessTools.all);
			var patch_method_2 = typeof(Manifest).GetMethod("EvilRiggsTargetLockPatch", AccessTools.all);
			//harmony.Patch(patch_target_2, prefix: new HarmonyMethod(patch_method_2));

			var patch_target_3 = typeof(ASpawn).GetMethod("GetTooltips", AccessTools.all);
			var patch_method_3 = typeof(Manifest).GetMethod("EvilRiggsTargetLockIconPatch", AccessTools.all);
			//harmony.Patch(patch_target_3, prefix: new HarmonyMethod(patch_method_3));

			var patch_target_4 = typeof(ASpawn).GetMethod("GetIcon", AccessTools.all);
			//harmony.Patch(patch_target_4, prefix: new HarmonyMethod(patch_method_3));

			var patch_target_5 = typeof(Ship).GetMethod("OnBeginTurn", AccessTools.all);
			var patch_method_5 = typeof(Manifest).GetMethod("EvilRiggsStartOfTurnStatusPatch", AccessTools.all);
			harmony.Patch(patch_target_5, postfix: new HarmonyMethod(patch_method_5));

			var patch_target_6 = typeof(AMove).GetMethod("Begin", AccessTools.all);
			var patch_method_6 = typeof(Manifest).GetMethod("EvilRiggsEngineRedirectPatch", AccessTools.all);
			harmony.Patch(patch_target_6, postfix: new HarmonyMethod(patch_method_6));

			var patch_target_7 = typeof(Football).GetMethod("GetActionsOnBonkedWhileInvincible", AccessTools.all);
			var patch_method_7 = typeof(Manifest).GetMethod("EvilRiggsFootballBonkPatch", AccessTools.all);
			harmony.Patch(patch_target_7, prefix: new HarmonyMethod(patch_method_7));

			var patch_target_8 = typeof(Football).GetMethod("GetActionsOnShotWhileInvincible", AccessTools.all);
			var patch_method_8 = typeof(Manifest).GetMethod("EvilRiggsFootballShootPatch", AccessTools.all);
			harmony.Patch(patch_target_8, prefix: new HarmonyMethod(patch_method_8));
		}

		private static void EvilRiggsRagePatch(G g, State s, Combat c, AStatus __instance)
		{
			Status status = __instance.status;
			int amt = __instance.statusAmount;
			int playerAmt = s.ship.Get(status);
			int disc = 0;
			foreach(Artifact artifact in s.EnumerateAllArtifacts())
			{
				if(artifact.GetType() == typeof(Artifacts.SwarmPreLoader))
				{
					disc = -2;
				}
			}
			if (status == (Status)statuses["rage"].Id! && playerAmt >= 7)
			{
				c.Queue(new AAddCard() { card = new EvilRiggsCard() { singleUseOverride = true, temporaryOverride = true, discount = disc }, destination = CardDestination.Hand, amount = 1 });
				c.Queue(new AStatus() { targetPlayer = true, status = (Status)statuses["rage"].Id!, statusAmount = -15 });
			}
		}

		private static void EvilRiggsTargetLockPatch(G g, State s, Combat c, ref ASpawn __instance)
		{
			Status status = (Status)statuses["targetLock"].Id!;
			int playerAmt = s.ship.Get(status);
			if (status == (Status)statuses["rage"].Id! && playerAmt >= 1 && __instance.thing.GetType() == typeof(Missile))
			{
				__instance.thing = new Missile { yAnimation = 0.0, missileType = MissileType.seeker };
				c.Queue(new AStatus() { targetPlayer = true, status = status, statusAmount = -1 });
			}
		}

		private static void EvilRiggsTargetLockIconPatch(State s, ref ASpawn __instance)
		{
			Status status = (Status)statuses["rage"].Id!;
			int playerAmt = s.ship.Get(status);
			if (status == (Status)statuses["rage"].Id! && playerAmt >= 1 && __instance.thing.GetType() == typeof(Missile))
			{
				__instance.thing = new Missile { yAnimation = 0.0, missileType = MissileType.seeker };
			}
		}

		private static void EvilRiggsStartOfTurnStatusPatch(State s, Combat c, ref Ship __instance)
		{
			Status statusRedirectUsed = (Status)statuses["engineRedirectUsed"].Id!;
			__instance.Set(statusRedirectUsed, 0);

			Status statusDiscount = (Status)statuses["discountNextTurn"].Id!;
			if (__instance.Get(statusDiscount) > 0)
			{
				int discountAmt = __instance.Get(statusDiscount);
				c.Queue(new CardActions.ADiscountFirstCards { amount = discountAmt, offset = 0 });
				__instance.Set(statusDiscount, 0);
			}

			Status statusBarrelRoll = (Status)statuses["barrelRoll"].Id!;
			if (__instance.Get(statusBarrelRoll) > 0)
			{
				int rollAmt = __instance.Get(statusBarrelRoll);
				c.QueueImmediate(new AMove { targetPlayer = true, dir = -rollAmt, isRandom = true });
			}

			foreach(Card card in c.hand)
			{
				foreach(CardAction action in card.GetActions(s,c))
				{
					if(action.GetType() == typeof(CardActions.ASequential))
					{
						card.flipped = false;
					}
				}
			}
		}

		private static void EvilRiggsEngineRedirectPatch(G g, State s, Combat c, ref AMove __instance)
		{
			Status statusRedirect = (Status)statuses["engineRedirect"].Id!;
			Status statusRedirectUsed = (Status)statuses["engineRedirectUsed"].Id!;
			Ship ship = (__instance.targetPlayer ? s.ship : c.otherShip);
			if (ship.Get(statusRedirect) > ship.Get(statusRedirectUsed))
			{
				c.QueueImmediate(new ADrawCard { count = 1 });
				c.QueueImmediate(new AStatus { targetPlayer = true, status = statusRedirectUsed, statusAmount = 1 });
			}
		}

		private static void EvilRiggsFootballBonkPatch(StuffBase thing)
		{
			FootballBonkContext = thing;
		}

		private static void EvilRiggsFootballShootPatch(ref int damage)
		{
			if (FootballBonkContext is MissileLight)
			{
				damage--;
			}

			FootballBonkContext = null;
		}
	}
}