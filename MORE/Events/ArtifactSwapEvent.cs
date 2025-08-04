using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using daisyowl.text;
using HarmonyLib;
using JetBrains.Annotations;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.MORE;

internal sealed class ArtifactSwapEvent : IRegisterable
{
	private static string EventName = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		EventName = $"{package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}";

		DB.story.all[EventName] = new()
		{
			type = NodeType.@event,
			canSpawnOnMap = true,
			oncePerRun = true,
			zones = ["zone_three"],
			bg = "BGGarbo",
			lines = [
				new CustomSay
				{
					who = "garbogirl",
					loopTag = "neutral",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "ArtifactSwap", "1-Garbogirl"])
				},
			],
			choiceFunc = EventName
		};
		DB.story.all[$"{EventName}::End"] = new()
		{
			type = NodeType.@event,
			canSpawnOnMap = false,
			oncePerRun = true,
			bg = "BGGarbo",
			lines = [
				new CustomSay
				{
					who = "garbogirl",
					loopTag = "neutral",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "ArtifactSwap", "End-1-Garbogirl"])
				},
			]
		};

		DB.eventChoiceFns[EventName] = AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetChoices));
	}

	public static void UpdateSettings(IPluginPackage<IModManifest> package, IModHelper helper, ProfileSettings settings)
	{
		var node = DB.story.all[EventName];
		node.never = settings.DisabledEvents.Contains(MoreEvent.ArtifactSwap) ? true : null;
		node.dontCountForProgression = settings.DisabledEvents.Contains(MoreEvent.ArtifactSwap);
	}

	[UsedImplicitly]
	private static List<Choice> GetChoices(State state)
		=> [
			new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "ArtifactSwap", "Choice-Yes", "title"]),
				key = $"{EventName}::End",
				actions = [
					new ArtifactChoiceAction(),
					new ATooltipAction
					{
						Tooltips = [new TTText { text = ModEntry.Instance.Localizations.Localize(["event", "ArtifactSwap", "Choice-Yes", "description"]) }]
					}
				],
			},
			new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "ArtifactSwap", "Choice-YesSpecial", "title"]),
				key = $"{EventName}::End",
				actions = [
					new ArtifactChoiceAction { Special = true },
					new ATooltipAction
					{
						Tooltips = [new TTText { text = ModEntry.Instance.Localizations.Localize(["event", "ArtifactSwap", "Choice-YesSpecial", "description"]) }]
					}
				],
			},
			new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "ArtifactSwap", "Choice-No"]),
				key = $"{EventName}::End",
			}
		];

	private sealed class ArtifactChoiceAction : CardAction
	{
		public bool Special;
		
		public override Route BeginWithRoute(G g, State s, Combat c)
			=> new ArtifactChoiceRoute { Special = Special };
	}

	private sealed class ArtifactChoiceRoute : Route, OnMouseDown
	{
		private static readonly UK ChoiceKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
		
		public bool Special;
		
		private List<Artifact>? ApplicableArtifacts;
		
		public override void Render(G g)
		{
			const int maxColumns = 5;
			const int width = 13;
			const int height = 13;
			const int spacing = 4;

			ApplicableArtifacts ??= g.state.EnumerateAllArtifacts()
				.Where(a => !a.GetMeta().unremovable && !a.GetMeta().pools.Contains(ArtifactPool.Boss) && !a.GetMeta().pools.Contains(ArtifactPool.Unreleased))
				.ToList();

			var columns = Math.Min(maxColumns, ApplicableArtifacts.Count);
			var totalWidth = columns * width + (columns - 1) * spacing;
			
			SharedArt.DrawEngineering(g);
			
			Draw.Text(ModEntry.Instance.Localizations.Localize(["event", "ArtifactSwap", "tradeTitle"]), g.mg.PIX_W / 2, 44, font: DB.stapler, color: Colors.textMain, align: TAlign.Center);

			var x = 0;
			var y = 0;

			foreach (var artifact in ApplicableArtifacts)
			{
				var box = g.Push(
					new UIKey(ChoiceKey, str: artifact.Key()),
					new Rect(g.mg.PIX_W / 2 - totalWidth / 2 + x * (width + spacing), 80 + y * (height + spacing), width, height),
					autoFocus: true,
					onMouseDown: this
				);

				Draw.Sprite(artifact.GetSprite(), box.rect.x, box.rect.y);
				
				if (box.IsHover())
					g.tooltips.Add(box.rect.xy + new Vec(20), artifact.GetTooltips());

				g.Pop();
				
				x++;
				if (x >= columns)
				{
					x = 0;
					y++;
				}
			}
		}

		public void OnMouseDown(G g, Box b)
		{
			if (b.key?.k == ChoiceKey)
			{
				g.state.GetCurrentQueue().InsertRange(0, [
					new ALoseArtifact { artifactType = b.key!.Value.str! },
					new OfferingAction { OriginalArtifactKey = b.key!.Value.str!, Special = Special },
				]);
				g.CloseRoute(this, CBResult.Done);
			}
		}
	}

	private sealed class OfferingAction : AArtifactOffering
	{
		public required string OriginalArtifactKey;
		public bool Special;
		
		public override Route BeginWithRoute(G g, State s, Combat c)
		{
			timer = 0;
			canSkip = false;

			HashSet<string> artifactKeys;

			if (Special)
			{
				var currentArtifactKeys = s.EnumerateAllArtifacts().Select(a => a.Key()).ToHashSet();
				
				artifactKeys = ModEntry.Instance.AltruisticArtifactKeys
					.Select(key => (Key: key, Type: DB.artifacts.GetValueOrDefault(key), Meta: DB.artifactMetas.GetValueOrDefault(key)))
					.Where(e => e.Type is not null && e.Meta is not null)
					.Where(e => !currentArtifactKeys.Contains(e.Key))
					.Where(e => e.Meta!.pools.Contains(ArtifactPool.Common) && s.characters.All(character => character.deckType != e.Meta.owner))
					.Where(e => !ArtifactReward.GetBlockedArtifacts(s).Contains(e.Type!))
					.Select(e => e.Key)
					.ToHashSet();
			}
			else
			{
				artifactKeys = [];
				for (var i = 0; i < 100; i++)
				{
					var artifacts = ArtifactReward.GetOffering(g.state, amount, limitDeck, limitPools)
						.Where(a => !artifactKeys.Contains(a.Key()) && OriginalArtifactKey != a.Key());
				
					foreach (var artifact in artifacts)
						artifactKeys.Add(artifact.Key());

					if (artifactKeys.Count >= 3)
						break;
				}
			}
			
			return new ArtifactReward
			{
				artifacts = artifactKeys
					.OrderBy(key => key)
					.Shuffle(s.rngArtifactOfferings)
					.Take(3)
					.Select(key => (Artifact)Activator.CreateInstance(DB.artifacts[key])!)
					.ToList(),
				canSkip = canSkip
			};
		}
	}
}
