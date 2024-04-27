using daisyowl.text;
using FSPRO;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CobaltPetrichor
{
	internal class RorArtifactReward : ArtifactReward, OnMouseDown
	{
		public int uses;

		public List<Type> Shuffle(List<Type> list, State s)
		{
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = (int)Math.Floor(s.rngActions.Next() * n);
				Type value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
			return list;
		}

		public override void OnEnter(State s)
		{
			Manifest.isInRorArtifactSelection = true;
			GetOffering(s);
		}

		public override void OnExit(State s)
		{
			Manifest.isInRorArtifactSelection = false;
		}

		public void GetOffering(State s)
		{
			HashSet<Type> ownedTypes = (from art in s.EnumerateAllArtifacts()
										select art.GetType()).ToHashSet();

			List<Type> artifactsCommon = DB.artifactMetas
				.Where(kvp => kvp.Value.owner == (Deck)Manifest.DeckCommon.Id!)
				.Select(kvp => DB.artifacts[kvp.Key])
				.Where(t => !ownedTypes.Contains(t))
				.ToList();

			List<Type> artifactsUncommon = DB.artifactMetas
				.Where(kvp => kvp.Value.owner == (Deck)Manifest.DeckUncommon.Id!)
				.Select(kvp => DB.artifacts[kvp.Key])
				.Where(t => !ownedTypes.Contains(t))
				.ToList();

			List<Type> artifactsRare = DB.artifactMetas
				.Where(kvp => kvp.Value.owner == (Deck)Manifest.DeckRare.Id!)
				.Select(kvp => DB.artifacts[kvp.Key])
				.Where(t => !ownedTypes.Contains(t))
				.ToList();

			//IOrderedEnumerable<Type> randomCommon = artifactsCommon.OrderBy((Type item) => s.rngArtifactOfferings.NextInt());
			//IOrderedEnumerable<Type> randomUncommon = artifactsUncommon.OrderBy((Type item) => s.rngArtifactOfferings.NextInt());
			//IOrderedEnumerable<Type> randomRare = artifactsRare.OrderBy((Type item) => s.rngArtifactOfferings.NextInt());

			artifactsCommon = Shuffle(artifactsCommon, s);
			artifactsUncommon = Shuffle(artifactsUncommon, s);
			artifactsRare = Shuffle(artifactsRare, s);

			List<Artifact> list = new List<Artifact>();

			for (int i = 0; i < 1; i++)
			{
				int rng = s.rngArtifactOfferings.NextInt() % 100;
				Type? item = null;
				if (rng > 90 && artifactsRare.Count != 0) { item = artifactsRare[i]; }
				else
				{
					if (rng > 65 && artifactsUncommon.Count != 0) { item = artifactsUncommon[i]; }
					else if (artifactsCommon.Count != 0) { item = artifactsCommon[i]; }
					else if (artifactsUncommon.Count != 0) { item = artifactsUncommon[i]; }
					else if (artifactsRare.Count != 0) { item = artifactsRare[i]; }
				}
				if (item != null)
					list.Add((Artifact)Activator.CreateInstance(item)!);
			}
			artifacts = list;
		}

		public override void Render(G g)
		{
			if (artifacts.Count == 0)
			{
				g.CloseRoute(this);
			}

			int num = 180;
			int num2 = 29;
			SharedArt.DrawEngineering(g);
			string str = "WRECKED UES SHIP";
			Font stapler = DB.stapler;
			Color? color = Colors.textMain;
			TAlign? align = TAlign.Center;
			Draw.Text(str, 240.0, 44.0, stapler, color, null, null, null, align);
			string str2 = "Choose an item to take.";
			Color? color2 = Colors.textMain.gain(0.5);
			align = TAlign.Center;
			double? maxWidth = 300.0;
			Draw.Text(str2, 240.0, 69.0, null, color2, null, null, maxWidth, align);
			string str2b = "You will open " + uses + " more chests at this node.";
			Draw.Text(str2b, 240.0, 69.0+14.0, null, color, null, null, maxWidth, align);
			if (canSkip)
			{
				Vec localV = new Vec(210.0, 205.0);
				UIKey key = UK.artifactReward_skip;
				string text = Loc.T("uiShared.btnSkipRewards");
				OnMouseDown onMouseDown = this;
				Color? boxColor = Colors.textMain.gain(0.5);
				SharedArt.ButtonText(g, localV, key, text, null, boxColor, inactive: false, onMouseDown, null, null, null, null, autoFocus: false, showAsPressed: false, gamepadUntargetable: false, hasDownState: false, null, null, null, null, 0, 60.0);
			}

			for (int i = 0; i < artifacts.Count; i++)
			{
				Artifact artifact = artifacts[i];
				UIKey? key2 = new UIKey(UK.artifactReward_artifact, i);
				Rect? rect = new Rect(240 - (int)((double)num / 2.0), 144.0 + Math.Floor(((double)i - (double)artifacts.Count / 2.0) * (double)(num2 - 2)), num, num2);
				OnMouseDown onMouseDown = this;
				Box box = g.Push(key2, rect, null, autoFocus: true, noHoverSound: false, gamepadUntargetable: false, ReticleMode.Quad, onMouseDown);
				Vec xy = box.rect.xy;
				bool flag = artifact.GetMeta().pools.Contains(ArtifactPool.Boss);
				ArtifactMeta meta = artifact.GetMeta();
				//DeckDef deckDef = DB.decks[meta.owner];

				string displayName = "Common";
				Color col = Colors.white;
				if (meta.owner == (Deck)Manifest.DeckUncommon.Id!)
				{
					displayName = "Uncommon";
					col = Colors.heal;
				}
				if (meta.owner == (Deck)Manifest.DeckRare.Id!)
				{
					displayName = "Rare";
					col = Colors.hurt;
				}
				Color? boxColor;
				if (meta.pools.Contains(ArtifactPool.Boss))
				{

					Spr? id = StableSpr.buttons_artifact_glow;
					double x = xy.x - 8.0;
					double y = xy.y - 8.0;
					boxColor = col.gain(0.5);
					BlendState screen = BlendMode.Screen;
					Draw.Sprite(id, x, y, flipX: false, flipY: false, 0.0, null, null, null, null, boxColor, screen);
				}

				Spr? id2 = (box.IsHover() ? StableSpr.buttons_artifact_on : StableSpr.buttons_artifact);
				double x2 = xy.x;
				double y2 = xy.y;
				boxColor = col;
				Draw.Sprite(id2, x2, y2, flipX: false, flipY: false, 0.0, null, null, null, null, boxColor);
				if (box.IsHover())
				{
					g.tooltips.Add(xy + new Vec(num + 3, 2.0), artifact.GetTooltips());
				}

				Vec vec = xy + new Vec(0.0, box.IsHover() ? 1 : 0);
				Vec vec2 = vec + new Vec(14.0, 14.0);
				Vec vec3 = vec2;
				artifact.lastScreenPos = vec2 + new Vec(-7.0, -7.0);
				Draw.Sprite(artifact.GetSprite(), (int)(vec3.x - 7.0), (int)(vec3.y - 7.0));
				string locName = artifact.GetLocName();
				double x3 = vec.x + 32.0;
				double y3 = vec.y + 7.0;
				Color? color3 = col;
				boxColor = Colors.black;
				Draw.Text(locName, x3, y3, null, color3, null, null, null, null, dontDraw: false, null, boxColor);
				string str3 = ((meta.owner != 0) ? (displayName + " ") : "") + (flag ? Loc.T("artifactReward.bossArtifactSuffix", "Boss Artifact") : Loc.T("artifactReward.artifactSuffix", "Artifact"));
				double x4 = vec.x + 32.0;
				double y4 = vec.y + 15.0;
				boxColor = col.fadeAlpha(0.4);
				Color? outline = Colors.black;
				Draw.Text(str3, x4, y4, null, null, boxColor, null, null, null, dontDraw: false, null, outline);
				g.Pop();
			}
		}

		new public void OnMouseDown(G g, Box b)
		{
			if (b.key == UK.artifactReward_skip)
			{
				Audio.Play(Event.Click);
				Analytics.Log(g.state, "artifactReward", new
				{
					skipped = true,
					artifacts = artifacts.Select((Artifact r) => r.Key())
				});
				DoILeaveYet(g);
				//g.CloseRoute(this);
			}

			int? num = b.key?.ValueFor(UK.artifactReward_artifact);
			if (!num.HasValue)
			{
				return;
			}

			int valueOrDefault = num.GetValueOrDefault();
			Artifact? artifact = artifacts.ElementAtOrDefault(valueOrDefault);
			if (artifact != null)
			{
				Audio.Play(Event.CardHandling);
				Analytics.Log(g.state, "artifactReward", new
				{
					artifact = artifact.Key(),
					artifacts = artifacts.Select((Artifact r) => r.Key())
				});
				artifact.animation = artifact.lastScreenPos;
				g.state.SendArtifactToChar(artifact);
				DoILeaveYet(g);
				//g.CloseRoute(this);
			}
		}

		public void DoILeaveYet(G g)
		{
			uses--;
			if(uses <= 0)
			{
				g.CloseRoute(this);
			} else
			{
				GetOffering(g.state);
			}
		}
	}
}
