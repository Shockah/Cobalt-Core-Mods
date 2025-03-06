// using System;
// using System.Collections.Generic;
// using System.Reflection;
// using Nanoray.PluginManager;
// using Nickel;
//
// namespace Shockah.Natasha;
//
// internal sealed class NatashaCatArtifact : Artifact, IRegisterable
// {
// 	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
// 	{
// 		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
// 			return;
// 		
// 		helper.Content.Artifacts.RegisterArtifact("NatashaCat", new()
// 		{
// 			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
// 			Meta = new()
// 			{
// 				owner = api.DuoArtifactVanillaDeck,
// 				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
// 			},
// 			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/Cat.png")).Sprite,
// 			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Cat", "name"]).Localize,
// 			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Cat", "description"]).Localize
// 		});
//
// 		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.NatashaDeck.Deck, Deck.colorless]);
// 	}
// 	
// 	private static readonly Lazy<List<Status>> PossibleStatuses = new(() =>
// 	{
// 		var results = new List<Status>();
// 		var set = new HashSet<Status>();
//
// 		foreach (var card in DB.releasedCards)
// 		{
// 			var meta = card.GetMeta();
// 			if (meta.dontOffer)
// 				continue;
//
// 			try
// 			{
// 				HandleUpgrade(Upgrade.None);
// 				foreach (var upgrade in meta.upgradesTo)
// 					HandleUpgrade(upgrade);
// 			}
// 			finally
// 			{
// 				card.upgrade = Upgrade.None;
// 			}
// 			
// 			void HandleUpgrade(Upgrade upgrade)
// 			{
// 				card.upgrade = upgrade;
// 				
// 				foreach (var baseAction in card.GetActions(DB.fakeState, DB.fakeCombat))
// 				{
// 					foreach (var wrappedAction in ModEntry.Instance.KokoroApi.WrappedActions.GetWrappedCardActionsRecursively(baseAction))
// 					{
// 						if (wrappedAction is AStatus statusAction)
// 						{
// 							if (statusAction.targetPlayer)
// 								continue;
// 							HandleStatus(statusAction.status);
// 						}
// 						else if (wrappedAction is AAttack attackAction)
// 						{
// 							if (attackAction.targetPlayer)
// 								continue;
// 							if (attackAction.status is not { } attackStatus)
// 								continue;
// 							HandleStatus(attackStatus);
// 						}
//
// 						void HandleStatus(Status status)
// 						{
// 							if (!set.Add(status))
// 								return;
// 							if (status is Status.shield or Status.tempShield)
// 								return;
// 							if (!DB.statuses.TryGetValue(status, out var statusDef))
// 								return;
// 							if (statusDef.isGood)
// 								return;
// 							
// 							results.Add(status);
// 						}
// 					
// 					}
// 				}
// 			}
// 		}
//
// 		return results;
// 	});
//
// 	public override void OnTurnStart(State state, Combat combat)
// 	{
// 		base.OnTurnStart(state, combat);
// 		if (combat.turn == 0)
// 			return;
//
// 		combat.QueueImmediate(new AStatus
// 		{
// 			targetPlayer = false,
// 			status = PossibleStatuses.Value[state.rngActions.NextInt() % PossibleStatuses.Value.Count],
// 			statusAmount = 1,
// 			artifactPulse = Key(),
// 		});
// 	}
// }