using FMOD;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class OperationAltairArtifact : Artifact, IRegisterable
{
	private static bool IsSimulating;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("OperationAltair", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.NatashaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/OperationAltair.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "OperationAltair", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "OperationAltair", "description"]).Localize
		});

		// specifically using non-delayed Harmony - these calls get inlined
		ModEntry.Instance.Helper.Utilities.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Audio), nameof(Audio.Play), [typeof(GUID), typeof(bool)]),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Audio_Play_Prefix))
		);
		ModEntry.Instance.Helper.Utilities.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Audio), nameof(Audio.Play), [typeof(string), typeof(bool)]),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Audio_Play_Prefix))
		);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			new TTGlossary("cardtrait.singleUse"),
			.. (ModEntry.Instance.KokoroApi.Limited.Trait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? []),
		];

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		Pulse();

		var pfx = PFXState.Create();
		var dt = MG.inst.g.dt;
		var drawPile = new List<Card>(state.deck);
		var discardPile = new List<Card>(combat.discard);
		var exhaustPile = new List<Card>(combat.exhausted);
		var hand = new List<Card>(combat.hand);
		List<Card> allCards = [.. drawPile, .. discardPile, .. exhaustPile, .. hand];

		try
		{
			IsSimulating = true;
			PFXState.BlankOut();
			MG.inst.g.dt = 1000;
			state.deck.Clear();
			combat.discard.Clear();
			combat.exhausted.Clear();
			combat.hand.Clear();

			var fakeState = Mutil.DeepCopy(state);
			var fakeCombat = Mutil.DeepCopy(combat);
			fakeState.route = fakeCombat;
			MG.inst.g.state = fakeState;

			fakeState.artifacts.Clear();
			foreach (var character in fakeState.characters)
				character.artifacts.Clear();

			FullyUpdateCombat();

			foreach (var card in allCards)
			{
				var data = card.GetDataWithOverrides(fakeState);
				if (data.singleUse)
					continue;

				fakeState.deck.Clear();
				fakeCombat.discard.Clear();
				fakeCombat.exhausted.Clear();
				fakeCombat.hand.Clear();
				fakeState.deck.AddRange(allCards);
				fakeState.deck.Remove(card);
				fakeCombat.hand.Add(card);

				fakeCombat.energy = Math.Max(fakeState.ship.baseEnergy, data.cost);
				fakeCombat.TryPlayCard(fakeState, card);

				FullyUpdateCombat();
			}

			void FullyUpdateCombat()
			{
				while (fakeCombat.currentCardAction is not null || fakeCombat.cardActions.Count != 0)
				{
					fakeCombat.Update(MG.inst.g);
					if (fakeCombat.routeOverride is not null)
					{
						fakeCombat.currentCardAction = null;
						fakeCombat.cardActions.Clear();
						break;
					}
				}
			}
		}
		finally
		{
			foreach (var card in allCards)
				ModEntry.Instance.KokoroApi.Limited.ResetLimitedUses(state, card);

			IsSimulating = false;
			pfx.Restore();
			MG.inst.g.dt = dt;
			MG.inst.g.state = state;
			state.deck.AddRange(drawPile);
			combat.discard.AddRange(discardPile);
			combat.exhausted.AddRange(exhaustPile);
			combat.hand.AddRange(hand);
		}
	}

	private static bool Audio_Play_Prefix()
		=> !IsSimulating;

	private sealed class PFXState
	{
		public required ParticleSystem combatAlpha { private get; init; }
		public required ParticleSystem combatAdd { private get; init; }
		public required ParticleSystem combatExplosion { private get; init; }
		public required ParticleSystem combatExplosionUnder { private get; init; }
		public required ParticleSystem combatExplosionSmoke { private get; init; }
		public required ParticleSystem combatExplosionWhiteSmoke { private get; init; }
		public required ParticleSystem combatScreenFadeOut { private get; init; }
		public required ParticleSystem screenSpaceAdd { private get; init; }
		public required ParticleSystem screenSpaceAlpha { private get; init; }
		public required ParticleSystem screenSpaceExplosion { private get; init; }
		public required Sparks combatSparks { private get; init; }
		public required Sparks screenSpaceSparks { private get; init; }

		public static PFXState Create()
			=> new()
			{
				combatAlpha = PFX.combatAlpha,
				combatAdd = PFX.combatAdd,
				combatExplosion = PFX.combatExplosion,
				combatExplosionUnder = PFX.combatExplosionUnder,
				combatExplosionSmoke = PFX.combatExplosionSmoke,
				combatExplosionWhiteSmoke = PFX.combatExplosionWhiteSmoke,
				combatScreenFadeOut = PFX.combatScreenFadeOut,
				screenSpaceAdd = PFX.screenSpaceAdd,
				screenSpaceAlpha = PFX.screenSpaceAlpha,
				screenSpaceExplosion = PFX.screenSpaceExplosion,
				combatSparks = PFX.combatSparks,
				screenSpaceSparks = PFX.screenSpaceSparks,
			};

		public static void BlankOut()
		{
			PFX.combatAlpha = new();
			PFX.combatAdd = new();
			PFX.combatExplosion = new();
			PFX.combatExplosionUnder = new();
			PFX.combatExplosionSmoke = new();
			PFX.combatExplosionWhiteSmoke = new();
			PFX.combatScreenFadeOut = new();
			PFX.screenSpaceAdd = new();
			PFX.screenSpaceAlpha = new();
			PFX.screenSpaceExplosion = new();
			PFX.combatSparks = new();
			PFX.screenSpaceSparks = new();
		}

		public void Restore()
		{
			PFX.combatAlpha = combatAlpha;
			PFX.combatAdd = combatAdd;
			PFX.combatExplosion = combatExplosion;
			PFX.combatExplosionUnder = combatExplosionUnder;
			PFX.combatExplosionSmoke = combatExplosionSmoke;
			PFX.combatExplosionWhiteSmoke = combatExplosionWhiteSmoke;
			PFX.combatScreenFadeOut = combatScreenFadeOut;
			PFX.screenSpaceAdd = screenSpaceAdd;
			PFX.screenSpaceAlpha = screenSpaceAlpha;
			PFX.screenSpaceExplosion = screenSpaceExplosion;
			PFX.combatSparks = combatSparks;
			PFX.screenSpaceSparks = screenSpaceSparks;
		}
	}
}