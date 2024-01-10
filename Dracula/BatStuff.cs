using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Shockah.Dracula;

internal sealed class BatStuff : StuffBase
{
	private const double Duration = 1.5;

	[JsonProperty]
	private double AnimationProgress { get; set; }

	public override bool IsHostile()
		=> this.targetPlayer;

	public override Spr? GetIcon()
		=> ModEntry.Instance.BatIcon.Sprite;

	public override double GetWiggleAmount()
		=> 1;

	public override double GetWiggleRate()
		=> 7;

	public override void Render(G g, Vec v)
	{
		base.Render(g, v);

		void ReallyRender(G g, Vec v)
		{
			DrawWithHilight(g, ModEntry.Instance.BatSprite.Sprite, v + GetOffset(g, doRound: true));
		}

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

		if (g.state.route is not Combat combat)
		{
			ReallyRender(g, v);
			return;
		}

		var ownerShip = targetPlayer ? combat.otherShip : g.state.ship;
		if (ClosestShipPart(ownerShip) is not { } closestShipPart)
		{
			ReallyRender(g, v);
			return;
		}

		Vec normalPosition = v;
		Vec attackedPosition = new(v.x, v.y - 40);
		Vec ownerPosition = new(v.x + (closestShipPart - x) * 16, v.y + 32);

		Vec oldPosition;
		Vec targetPosition;
		double f;

		if (AnimationProgress >= 2.5)
		{
			oldPosition = normalPosition;
			targetPosition = attackedPosition;
			f = 1 - (AnimationProgress - 2.5);
		}
		else if (AnimationProgress >= 1)
		{
			oldPosition = attackedPosition;
			targetPosition = ownerPosition;
			f = 1 - (AnimationProgress - 1) / 1.5;
		}
		else
		{
			oldPosition = ownerPosition;
			targetPosition = normalPosition;
			f = 1 - AnimationProgress;
		}

		f = Math.Clamp(f, 0, 1);
		f = Ease(f);
		ReallyRender(
			g,
			new Vec(
				oldPosition.x + (targetPosition.x - oldPosition.x) * f,
				oldPosition.y + (targetPosition.y - oldPosition.y) * f
			)
		);
	}

	public override List<CardAction>? GetActions(State s, Combat c)
		=> [
			new ABatAttack
			{
				TargetPlayer = this.targetPlayer,
				FromX = x
			}
		];

	public override List<Tooltip> GetTooltips()
		=> [
			new CustomTTGlossary(
				CustomTTGlossary.GlossaryType.midrow,
				() => ModEntry.Instance.BatIcon.Sprite,
				() => ModEntry.Instance.Localizations.Localize(["midrow", "Bat", "name"]),
				() => ModEntry.Instance.Localizations.Localize(["midrow", "Bat", "description"])
			),
			new TTGlossary($"status.{ModEntry.Instance.BleedingStatus.Status.Key()}"),
			new TTGlossary($"status.{ModEntry.Instance.TransfusionStatus.Status.Key()}"),
			new TTGlossary($"status.{ModEntry.Instance.TransfusingStatus.Status.Key()}"),
		];

	private sealed class ABatAttack : CardAction
	{
		public required bool TargetPlayer { get; init; }
		public required int FromX { get; init; }

		[JsonProperty]
		private bool HasValidTarget { get; set; }

		[JsonProperty]
		private CardAction? CurrentSubAction { get; set; }

		[JsonProperty]
		private List<CardAction> QueuedSubActions { get; } = [];

		public override void Begin(G g, State s, Combat c)
		{
			timer = Duration;
			var attackedShip = TargetPlayer ? s.ship : c.otherShip;
			HasValidTarget = attackedShip.GetPartAtWorldX(FromX) is not null;
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
				AccessTools.DeclaredMethod(typeof(Combat), "BeginCardAction").Invoke(c, [g, queuedSubAction]);
				return;
			}

			var oldTimer = timer;
			base.Update(g, s, c);
			if (!c.stuff.TryGetValue(FromX, out var @object) || @object is not BatStuff bat)
				return;
			bat.AnimationProgress = timer / Duration * 3.5;

			var ownerShip = TargetPlayer ? c.otherShip : s.ship;
			var attackedShip = TargetPlayer ? s.ship : c.otherShip;

			if (oldTimer / Duration * 3.5 > 2.5 && timer / Duration * 3.5 <= 2.5)
			{
				if (HasValidTarget)
					QueuedSubActions.Add(new AStatus
					{
						targetPlayer = attackedShip.isPlayerShip,
						status = ModEntry.Instance.BleedingStatus.Status,
						statusAmount = 1
					});
			}
			else if (oldTimer / Duration * 3.5 > 1 && timer / Duration * 3.5 <= 1)
			{
				QueuedSubActions.Add(new AStatus
				{
					targetPlayer = ownerShip.isPlayerShip,
					status = HasValidTarget ? ModEntry.Instance.TransfusionStatus.Status : ModEntry.Instance.BleedingStatus.Status,
					statusAmount = 1
				});
			}
		}
	}
}