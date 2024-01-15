using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dracula;

internal sealed class BatStuff : StuffBase
{
	[JsonConverter(typeof(StringEnumConverter))]
	public enum BatType
	{
		Normal, Bloodthirsty, Protective
	}

	[JsonConverter(typeof(StringEnumConverter))]
	private enum AnimationDestination
	{
		Rest, Enemy, Owner
	}

	[JsonProperty]
	public BatType Type = BatType.Normal;

	[JsonProperty]
	private (AnimationDestination From, AnimationDestination To, double Progress)? Animation;

	public override bool IsHostile()
		=> this.targetPlayer;

	public override Spr? GetIcon()
		=> (Type switch
		{
			BatType.Bloodthirsty => ModEntry.Instance.BatAIcon,
			BatType.Protective => ModEntry.Instance.BatBIcon,
			_ => ModEntry.Instance.BatIcon,
		}).Sprite;

	public override double GetWiggleAmount()
		=> 1;

	public override double GetWiggleRate()
		=> 7;

	public override Vec GetOffset(G g, bool doRound = false)
	{
		var offset = base.GetOffset(g, doRound);

		if (g.state.route is not Combat combat)
			return offset;

		int? ClosestShipPart(Ship ship)
		{
			var maxOffset = Math.Max(Math.Abs(ship.x - x), Math.Abs(ship.x + ship.parts.Count - 1 - x));
			for (var i = 0; i < maxOffset; i++)
			{
				if (i != 0 && ship.GetPartAtWorldX(x - i) is { } part1 && part1.type != PType.empty)
					return x - i;
				if (ship.GetPartAtWorldX(x + i) is { } part2 && part2.type != PType.empty)
					return x + i;
			}
			return null;
		}

		static double Ease(double f)
			=> -(Math.Cos(Math.PI * f) - 1) / 2;

		var ownerShip = targetPlayer ? combat.otherShip : g.state.ship;
		if (ClosestShipPart(ownerShip) is not { } closestShipPart || Animation is not { } animation)
			return offset;

		float playerShipOffset = 32;
		float enemyShipOffset = 40;
		Vec normalPosition = offset;
		Vec attackedPosition = new(offset.x, offset.y + (targetPlayer ? playerShipOffset : -enemyShipOffset));
		Vec ownerPosition = new(offset.x + (closestShipPart - x) * 16, offset.y + (targetPlayer ? -enemyShipOffset : playerShipOffset));

		Vec oldPosition;
		Vec targetPosition;
		double xOffset;

		oldPosition = animation.From switch
		{
			AnimationDestination.Enemy => attackedPosition,
			AnimationDestination.Owner => ownerPosition,
			_ => normalPosition
		};
		targetPosition = animation.To switch
		{
			AnimationDestination.Enemy => attackedPosition,
			AnimationDestination.Owner => ownerPosition,
			_ => normalPosition
		};

		if (animation.From == AnimationDestination.Rest)
			xOffset = -(1 - Math.Abs(animation.Progress - 0.5) * 2) * 24;
		else if (animation.From == AnimationDestination.Enemy)
			xOffset = (1 - Math.Abs(animation.Progress - 0.5) * 2) * 24;
		else
			xOffset = 0;

		var f = animation.Progress;
		f = Math.Clamp(f, 0, 1);
		f = Ease(f);
		return new Vec(
			oldPosition.x + (targetPosition.x - oldPosition.x) * f + xOffset,
			oldPosition.y + (targetPosition.y - oldPosition.y) * f
		);
	}

	public override void Render(G g, Vec v)
	{
		base.Render(g, v);

		var sprite = Type switch
		{
			BatType.Bloodthirsty => ModEntry.Instance.BatASprite,
			BatType.Protective => ModEntry.Instance.BatBSprite,
			_ => ModEntry.Instance.BatSprite,
		};
		DrawWithHilight(g, sprite.Sprite, v + GetOffset(g, doRound: false), flipY: targetPlayer);
	}

	public override List<CardAction>? GetActions(State s, Combat c)
		=> [
			new ABatAttack
			{
				Type = Type,
				TargetPlayer = targetPlayer,
				FromX = x
			}
		];

	public override List<Tooltip> GetTooltips()
	{
		List<Tooltip> tooltips = [
			new CustomTTGlossary(
				CustomTTGlossary.GlossaryType.midrow,
				() => GetIcon()!,
				() => ModEntry.Instance.Localizations.Localize(["midrow", "Bat", Type.ToString(), "name"]),
				() => ModEntry.Instance.Localizations.Localize(["midrow", "Bat", Type.ToString(), "description"])
			),
			new TTGlossary($"status.{ModEntry.Instance.BleedingStatus.Status.Key()}"),
			new TTGlossary($"status.{ModEntry.Instance.TransfusionStatus.Status.Key()}"),
		];

		if (Type == BatType.Normal)
			tooltips.Add(new TTGlossary($"status.{ModEntry.Instance.TransfusingStatus.Status.Key()}"));
		if (Type == BatType.Protective)
			tooltips.Add(new TTGlossary("midrow.bubbleShield"));
		return tooltips;
	}

	private sealed class ABatAttack : CardAction
	{
		[JsonConverter(typeof(StringEnumConverter))]
		private enum AnimationAction
		{
			None, HitEnemy, HitOwner
		}

		private const double ShortDistanceDuration = 0.4;
		private const double LongDistanceDuration = 0.7;

		public required BatType Type { get; init; }
		public required bool TargetPlayer { get; init; }
		public required int FromX { get; init; }

		[JsonProperty]
		private bool HasValidTarget { get; set; }

		[JsonProperty]
		private readonly List<(AnimationDestination From, AnimationDestination To, double Duration, AnimationAction Action)> Animation = [];

		[JsonProperty]
		private CardAction? CurrentSubAction { get; set; }

		[JsonProperty]
		private List<CardAction> QueuedSubActions { get; } = [];

		public override bool CanSkipTimerIfLastEvent()
			=> false;

		public override void Begin(G g, State s, Combat c)
		{
			var attackedShip = TargetPlayer ? s.ship : c.otherShip;
			HasValidTarget = attackedShip.GetPartAtWorldX(FromX) is not null;

			var bat = c.stuff.TryGetValue(FromX, out var existingBat) ? existingBat : null;

			Animation.Add((From: AnimationDestination.Rest, To: AnimationDestination.Enemy, ShortDistanceDuration, Action: AnimationAction.HitEnemy));
			if (Type == BatType.Protective && bat?.bubbleShield == false && HasValidTarget)
			{
				Animation.Add((From: AnimationDestination.Enemy, To: AnimationDestination.Rest, ShortDistanceDuration, Action: AnimationAction.None));
			}
			else
			{
				Animation.Add((From: AnimationDestination.Enemy, To: AnimationDestination.Owner, LongDistanceDuration, Action: AnimationAction.HitOwner));
				Animation.Add((From: AnimationDestination.Owner, To: AnimationDestination.Rest, ShortDistanceDuration, Action: AnimationAction.None));
			}
			timer = Animation.Sum(a => a.Duration);
		}

		public override void Update(G g, State s, Combat c)
		{
			if (CurrentSubAction is { } currentSubAction)
			{
				CurrentSubAction.Update(g, s, c);
				if (CurrentSubAction.timer <= 0)
					CurrentSubAction = null;
				return;
			}
			if (QueuedSubActions.Count > 0)
			{
				var queuedSubAction = QueuedSubActions[0];
				QueuedSubActions.RemoveAt(0);
				CurrentSubAction = queuedSubAction;
				c.BeginCardAction(g, queuedSubAction);
				return;
			}

			var oldTimer = timer;
			base.Update(g, s, c);
			if (!c.stuff.TryGetValue(FromX, out var @object) || @object is not BatStuff bat)
				return;

			var ownerShip = TargetPlayer ? c.otherShip : s.ship;
			var attackedShip = TargetPlayer ? s.ship : c.otherShip;

			var totalTimer = Animation.Sum(a => a.Duration);
			var summedUpTimer = 0.0;
			var oldElapsedTimer = totalTimer - oldTimer;
			var newElapsedTimer = totalTimer - timer;

			foreach (var animation in Animation)
			{
				if (newElapsedTimer - summedUpTimer < 0)
					continue;

				if (newElapsedTimer - summedUpTimer < animation.Duration)
				{
					bat.Animation = (From: animation.From, To: animation.To, Progress: (newElapsedTimer - summedUpTimer) / animation.Duration);
				}
				else if (oldElapsedTimer - summedUpTimer < animation.Duration && newElapsedTimer - summedUpTimer > animation.Duration)
				{
					bat.Animation = (From: animation.From, To: animation.To, Progress: 1);
					timer = totalTimer - summedUpTimer - animation.Duration - 0.01;
					switch (animation.Action)
					{
						case AnimationAction.None:
							break;
						case AnimationAction.HitEnemy:
							HitEnemy(s, c, attackedShip);
							break;
						case AnimationAction.HitOwner:
							HitOwner(s, c, ownerShip);
							break;
					}
				}
				else
				{
					summedUpTimer += animation.Duration;
					continue;
				}
				break;
			}

			if (timer <= 0)
				bat.Animation = null;
		}

		private void HitEnemy(State state, Combat combat, Ship ship)
		{
			if (!HasValidTarget)
				return;
			if (!combat.stuff.TryGetValue(FromX, out var bat))
				return;

			if (Type == BatType.Bloodthirsty)
				ship.NormalDamage(state, combat, 1, FromX);
			else if (Type == BatType.Protective && !bat.bubbleShield)
				bat.bubbleShield = true;

			QueuedSubActions.Add(new AStatus
			{
				targetPlayer = ship.isPlayerShip,
				status = ModEntry.Instance.BleedingStatus.Status,
				statusAmount = 1
			});
		}

		private void HitOwner(State state, Combat combat, Ship ship)
		{
			if (Type == BatType.Bloodthirsty)
			{
				if (HasValidTarget)
					QueuedSubActions.Add(new AHeal
					{
						targetPlayer = ship.isPlayerShip,
						healAmount = 1
					});
				else
					ship.NormalDamage(state, combat, 1, FromX);
			}

			if (HasValidTarget)
			{
				QueuedSubActions.Add(new AStatus
				{
					targetPlayer = ship.isPlayerShip,
					status = ModEntry.Instance.TransfusionStatus.Status,
					statusAmount = 1
				});
			}
			else
			{
				QueuedSubActions.Add(new AStatus
				{
					targetPlayer = ship.isPlayerShip,
					status = ModEntry.Instance.BleedingStatus.Status,
					statusAmount = 1
				});
			}
		}
	}
}