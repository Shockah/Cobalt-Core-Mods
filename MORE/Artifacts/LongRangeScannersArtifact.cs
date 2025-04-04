using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.MORE;

internal sealed class LongRangeScannersArtifact : Artifact, IRegisterable
{
	private static IArtifactEntry ArtifactEntry = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ArtifactEntry = helper.Content.Artifacts.RegisterArtifact("LongRangeScanners", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.Common],
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifact/LongRangeScanners.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "LongRangeScanners", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "LongRangeScanners", "description"]).Localize
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(MapRoute), nameof(MapRoute.DrawMapMarker)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(MapRoute_DrawMapMarker_Prefix)))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(MapBattle), nameof(MapBattle.GetTooltips)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(MapBattle_GetTooltips_Postfix)))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(MapEvent), nameof(MapEvent.GetTooltips)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(MapEvent_GetTooltips_Postfix)))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(MapArtifact), nameof(MapArtifact.GetTooltips)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(MapArtifact_GetTooltips_Postfix)))
		);
	}

	private static void MapRoute_DrawMapMarker_Prefix(G g, MapBase map, KeyValuePair<Vec, Marker> pair, bool isPreview)
	{
		var oldState = g.state;
		
		if (isPreview)
			return;
		if (pair.Value.contents is not (MapBattle or MapEvent or MapArtifact))
			return;
		if (pair.Value.contents is not MapBattle { battleType: BattleType.Boss } && !map.CanGoHere(pair.Key))
			return;
		if (!g.state.EnumerateAllArtifacts().Any(a => a is LongRangeScannersArtifact))
			return;

		try
		{
			if (pair.Value.contents is MapBattle battleNode)
			{
				if (ModEntry.Instance.Helper.ModData.ContainsModData(battleNode, "ExposedBattleShip"))
					return;
				
				var stateCopy = Mutil.DeepCopy(g.state);
				g.state = stateCopy;
				_ = stateCopy.map.MakeRoute(stateCopy, pair.Key);
				
				if (!stateCopy.map.markers.TryGetValue(pair.Key, out var nodeCopy))
					return;
				if (nodeCopy.contents is not MapBattle battleNodeCopy)
					return;
				if (battleNodeCopy.ai is not { } ai)
					return;
				
				ModEntry.Instance.Helper.ModData.SetModData(battleNode, "ExposedBattleShip", ai.Key());

				if (battleNode.battleType != BattleType.Boss)
				{
					var combat = Combat.Make(stateCopy, ai);
					RecordFightModifier();
					combat.PlayerWon(g);
					RecordBattleCard();
					RecordBattleArtifact();
					
					void RecordBattleCard()
					{
						if (stateCopy.rewardsQueue.OfType<ACardOffering>().LastOrDefault() is not { } postCombatCardOffering)
							return;
						if (postCombatCardOffering.BeginWithRoute(g, stateCopy, combat) is not CardReward postCombatCardReward)
							return;
						if (postCombatCardReward.cards.Count == 0)
							return;
						ModEntry.Instance.Helper.ModData.SetModData(battleNode, "ExposedBattleCard", postCombatCardReward.cards[0].Key());
					}
				
					void RecordBattleArtifact()
					{
						if (stateCopy.rewardsQueue.OfType<AArtifactOffering>().LastOrDefault() is not { } postCombatArtifactOffering)
							return;
						if (postCombatArtifactOffering.BeginWithRoute(g, stateCopy, combat) is not ArtifactReward postCombatArtifactReward)
							return;
						if (postCombatArtifactReward.artifacts.Count == 0)
							return;
						ModEntry.Instance.Helper.ModData.SetModData(battleNode, "ExposedBattleArtifact", postCombatArtifactReward.artifacts[0].Key());
					}
				
					void RecordFightModifier()
					{
						if (combat.modifier is not { } modifier)
							return;
						ModEntry.Instance.Helper.ModData.SetModData(battleNode, "ExposedBattleModifier", Mutil.DeepCopy(modifier));
					}
				}
			}
			else if (pair.Value.contents is MapEvent eventNode)
			{
				if (ModEntry.Instance.Helper.ModData.ContainsModData(eventNode, "ExposedEvent"))
					return;

				var stateCopy = Mutil.DeepCopy(g.state);
				g.state = stateCopy;
				if (stateCopy.map.MakeRoute(stateCopy, pair.Key) is not Dialogue route)
					return;
				if (!DB.story.all.TryGetValue(route.ctx.script, out var storyNode))
					return;

				var flippedLines = storyNode.lines
					.SelectMany(instruction => instruction switch
					{
						Say say => [say],
						SaySwitch saySwitch => saySwitch.lines,
						_ => []
					})
					.Where(say => say.flipped);

				ModEntry.Instance.Helper.ModData.SetModData(eventNode, "ExposedEvent", route.ctx.script);
				if (flippedLines.FirstOrDefault(say => say.who != "comp") is { } flippedLine)
					ModEntry.Instance.Helper.ModData.SetModData(eventNode, "ExposedEventCharacter", flippedLine.who);
			}
			else if (pair.Value.contents is MapArtifact artifactNode)
			{
				if (ModEntry.Instance.Helper.ModData.ContainsModData(artifactNode, "ExposedArtifact"))
					return;
				
				var stateCopy = Mutil.DeepCopy(g.state);
				g.state = stateCopy;
				if (stateCopy.map.MakeRoute(stateCopy, pair.Key) is not ArtifactReward route)
					return;
				
				if (route.artifacts.Count == 0)
				{
					ModEntry.Instance.Helper.ModData.SetModData(artifactNode, "ExposedArtifact", "");
					return;
				}
				
				ModEntry.Instance.Helper.ModData.SetModData(artifactNode, "ExposedArtifact", route.artifacts[0].Key());
			}
		}
		finally
		{
			g.state = oldState;
		}
	}

	private static void MapBattle_GetTooltips_Postfix(MapBattle __instance, ref List<Tooltip> __result)
	{
		var tooltips = __result;
		if (!ModEntry.Instance.Helper.ModData.TryGetModData<string>(__instance, "ExposedBattleShip", out var aiKey))
			return;
		if (!DB.currentLocale.strings.ContainsKey("enemy.{0}.name".FF(aiKey)))
			return;

		tooltips.Add(new GlossaryTooltip($"artifact.{ArtifactEntry.UniqueName}::Battle")
		{
			TitleColor = Colors.textBold,
			Title = ModEntry.Instance.Localizations.Localize(["artifact", "LongRangeScanners", "battleTooltip", "title"]),
			Description = ModEntry.Instance.Localizations.Localize(["artifact", "LongRangeScanners", "battleTooltip", "description"], new { Name = Loc.GetLocString("enemy.{0}.name".FF(aiKey)) }),
		});

		if (ModEntry.Instance.Helper.ModData.TryGetModData<FightModifier>(__instance, "ExposedBattleModifier", out var modifier))
		{
			tooltips.Add(new GlossaryTooltip($"artifact.{ArtifactEntry.UniqueName}::Battle::Modifier")
			{
				TitleColor = Colors.textBold,
				Description = ModEntry.Instance.Localizations.Localize(["artifact", "LongRangeScanners", "battleTooltip", "modifier"], new { Name = modifier.GetNameLoc() }),
			});
			tooltips.AddRange(modifier.GetTooltips(DB.fakeState, DB.fakeCombat));
		}

		if (HandleArtifact())
		{
			__result = tooltips;
			return;
		}
		if (HandleCard())
		{
			__result = tooltips;
			return;
		}
		__result = tooltips;

		bool HandleArtifact()
		{
			if (!ModEntry.Instance.Helper.ModData.TryGetModData<string>(__instance, "ExposedBattleArtifact", out var artifactKey))
				return false;
			if (ModEntry.Instance.Helper.Content.Artifacts.LookupByUniqueName(artifactKey) is not { } entry)
				return false;
			if (entry.Configuration.Name?.Invoke(DB.currentLocale.locale) is not { } artifactName || string.IsNullOrEmpty(artifactName))
				return false;

			tooltips.Add(new GlossaryTooltip($"artifact.{ArtifactEntry.UniqueName}::Battle::Artifact")
			{
				TitleColor = Colors.textBold,
				Description = ModEntry.Instance.Localizations.Localize(["artifact", "LongRangeScanners", "battleTooltip", "artifact"]),
			});
			tooltips.AddRange(((Artifact)Activator.CreateInstance(entry.Configuration.ArtifactType)!).GetTooltips());
			return true;
		}

		bool HandleCard()
		{
			if (!ModEntry.Instance.Helper.ModData.TryGetModData<string>(__instance, "ExposedBattleCard", out var cardKey))
				return false;
			if (ModEntry.Instance.Helper.Content.Cards.LookupByUniqueName(cardKey) is not { } entry)
				return false;
			if (entry.Configuration.Name?.Invoke(DB.currentLocale.locale) is not { } cardName || string.IsNullOrEmpty(cardName))
				return false;

			tooltips.Add(new GlossaryTooltip($"artifact.{ArtifactEntry.UniqueName}::Battle::Card")
			{
				TitleColor = Colors.textBold,
				Description = ModEntry.Instance.Localizations.Localize(["artifact", "LongRangeScanners", "battleTooltip", "card"]),
			});
			tooltips.Add(new TTCard { card = (Card)Activator.CreateInstance(entry.Configuration.CardType)! });
			return true;
		}
	}

	private static void MapEvent_GetTooltips_Postfix(MapBattle __instance, G g, ref List<Tooltip> __result)
	{
		if (ModEntry.Instance.Helper.ModData.TryGetModData<string>(__instance, "ExposedEventCharacter", out var eventCharacterKey))
		{
			if (eventCharacterKey is "void" or "tentacle")
			{
				__result.Add(new GlossaryTooltip($"artifact.{ArtifactEntry.UniqueName}::Event")
				{
					TitleColor = Colors.textBold,
					Title = ModEntry.Instance.Localizations.Localize(["artifact", "LongRangeScanners", "eventTooltip", eventCharacterKey, "title"]),
					Description = ModEntry.Instance.Localizations.Localize(["artifact", "LongRangeScanners", "eventTooltip", eventCharacterKey, "description"]),
				});
			}
			else
			{
				if (eventCharacterKey != "spike" && !DB.currentLocale.strings.ContainsKey(DB.Join("char.", eventCharacterKey)))
					return;
				if (ModEntry.Instance.Helper.Content.Characters.V2.LookupByCharacterType(eventCharacterKey) is null)
					return;

				var displayName = Character.GetDisplayName(eventCharacterKey, g.state);
				if (string.IsNullOrEmpty(displayName))
				{
					__result.Add(new GlossaryTooltip($"artifact.{ArtifactEntry.UniqueName}::Event")
					{
						TitleColor = Colors.textBold,
						Title = ModEntry.Instance.Localizations.Localize(["artifact", "LongRangeScanners", "eventTooltip", "unknown", "title"]),
						Description = ModEntry.Instance.Localizations.Localize(["artifact", "LongRangeScanners", "eventTooltip", "unknown", "description"]),
					});
				}
				else
				{
					__result.Add(new GlossaryTooltip($"artifact.{ArtifactEntry.UniqueName}::Event")
					{
						TitleColor = Colors.textBold,
						Title = ModEntry.Instance.Localizations.Localize(["artifact", "LongRangeScanners", "eventTooltip", "character", "title"]),
						Description = ModEntry.Instance.Localizations.Localize(["artifact", "LongRangeScanners", "eventTooltip", "character", "description"], new { Name = Character.GetDisplayName(eventCharacterKey, g.state) }),
					});
				}
			}
		}
		else if (ModEntry.Instance.Helper.ModData.TryGetModData<string>(__instance, "ExposedEvent", out var eventKey))
		{
			var localizationKey = eventKey switch
			{
				"LoseCharacterCard" => "blackHole",
				"ChoiceHPForArtifact" => "minefield",
				"CrystallizedFriendEvent" => "crystal",
				_ => "unknown",
			};
			
			__result.Add(new GlossaryTooltip($"artifact.{ArtifactEntry.UniqueName}::Event")
			{
				TitleColor = Colors.textBold,
				Title = ModEntry.Instance.Localizations.Localize(["artifact", "LongRangeScanners", "eventTooltip", localizationKey, "title"]),
				Description = ModEntry.Instance.Localizations.Localize(["artifact", "LongRangeScanners", "eventTooltip", localizationKey, "description"]),
			});
		}
	}

	private static void MapArtifact_GetTooltips_Postfix(MapArtifact __instance, ref List<Tooltip> __result)
	{
		if (!ModEntry.Instance.Helper.ModData.TryGetModData<string>(__instance, "ExposedArtifact", out var artifactKey))
			return;
		if (ModEntry.Instance.Helper.Content.Artifacts.LookupByUniqueName(artifactKey) is not { } entry)
			return;
		if (entry.Configuration.Name?.Invoke(DB.currentLocale.locale) is not { } artifactName || string.IsNullOrEmpty(artifactName))
			return;

		__result.Add(new GlossaryTooltip($"artifact.{ArtifactEntry.UniqueName}::Artifact")
		{
			TitleColor = Colors.textBold,
			Title = ModEntry.Instance.Localizations.Localize(["artifact", "LongRangeScanners", "artifactTooltip", "title"]),
			Description = ModEntry.Instance.Localizations.Localize(["artifact", "LongRangeScanners", "artifactTooltip", "description"], new { Name = artifactName.ToUpper() }),
		});
		__result.AddRange(((Artifact)Activator.CreateInstance(entry.Configuration.ArtifactType)!).GetTooltips());
	}
}