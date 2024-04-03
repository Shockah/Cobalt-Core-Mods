using HarmonyLib;
using Nickel;
using Shockah.Shared;
using System;

namespace Shockah.Dyna;

internal static class BlastwaveExt
{
	public static bool IsBlastwave(this AAttack self)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(self, "IsBlastwave");

	public static bool IsStunwave(this AAttack self)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(self, "IsStunwave");

	public static int? GetBlastwaveDamage(this AAttack self)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<int>(self, "BlastwaveDamage");

	public static int GetBlastwaveRange(this AAttack self)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault(self, "BlastwaveRange", 1);

	public static AAttack SetBlastwave(this AAttack self, int? damage, int range = 1, bool isStunwave = false)
	{
		ModEntry.Instance.Helper.ModData.SetModData(self, "IsBlastwave", true);
		ModEntry.Instance.Helper.ModData.SetModData(self, "IsStunwave", isStunwave);
		ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "BlastwaveDamage", damage);
		ModEntry.Instance.Helper.ModData.SetModData(self, "BlastwaveRange", range);
		return self;
	}
}

internal sealed class BlastwaveManager
{
	private static ISpriteEntry BlastwaveIcon = null!;
	private static ISpriteEntry WideBlastwaveIcon = null!;

	private static AAttack? AttackContext;

	public BlastwaveManager()
	{
		BlastwaveIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icons/Blastwave.png"));
		WideBlastwaveIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icons/BlastwaveWide.png"));

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(GetType(), nameof(Card_RenderAction_Prefix))
		);

		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnPlayerTakeNormalDamage), (State state, Combat combat, Part? part) =>
		{
			TriggerBlastwaveIfNeeded(state, combat, part, targetPlayer: true);
		}, 0);

		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnEnemyGetHit), (State state, Combat combat, Part? part) =>
		{
			TriggerBlastwaveIfNeeded(state, combat, part, targetPlayer: false);
		}, 0);
	}

	private static void TriggerBlastwaveIfNeeded(State state, Combat combat, Part? part, bool targetPlayer)
	{
		if (part is not { } nonNullPart)
			return;
		if (AttackContext is not { } attack)
			return;
		if (!attack.IsBlastwave())
			return;

		var targetShip = targetPlayer ? state.ship : combat.otherShip;
		var worldX = targetShip.x + targetShip.parts.IndexOf(nonNullPart);

		attack.timer *= 0.5;
		combat.QueueImmediate(new BlastwaveAction
		{
			TargetPlayer = targetPlayer,
			WorldX = worldX,
			Damage = attack.GetBlastwaveDamage(),
			Range = attack.GetBlastwaveRange(),
			IsStunwave = attack.IsStunwave(),
		});
	}

	private static void AAttack_Begin_Prefix(AAttack __instance)
		=> AttackContext = __instance;

	private static void AAttack_Begin_Finalizer()
		=> AttackContext = null;

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not AAttack attack)
			return true;
		if (!attack.IsBlastwave())
			return true;

		var copy = Mutil.DeepCopy(attack);
		ModEntry.Instance.Helper.ModData.SetModData(copy, "IsBlastwave", false);

		var position = g.Push(rect: new()).rect.xy;
		int initialX = (int)position.x;

		position.x += Card.RenderAction(g, state, copy, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
		g.Pop();

		__result = (int)position.x - initialX;
		__result += 2;

		if (!dontDraw)
		{
			var icon = attack.GetBlastwaveRange() switch
			{
				<= 1 => BlastwaveIcon,
				>= 2 => WideBlastwaveIcon,
			};
			Draw.Sprite(icon.Sprite, initialX + __result, position.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
		}
		__result += 9;

		if (attack.GetBlastwaveDamage() is { } damage)
		{
			if (!dontDraw)
				BigNumbers.Render(damage, initialX + __result, position.y, action.disabled ? Colors.disabledText : Colors.redd);
			__result += damage.ToString().Length * 6;
		}

		if (attack.IsStunwave())
		{
			if (!dontDraw)
				Draw.Sprite(StableSpr.icons_stun, initialX + __result, position.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
			__result += 9;
		}

		return false;
	}

	private sealed class BlastwaveAction : CardAction
	{
		private const double SinglePartDuration = 0.2;

		public bool TargetPlayer = false;
		public required int WorldX;
		public required int? Damage;
		public int Range = 1;
		public bool IsStunwave;
		public bool IsPiercing = false;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = Range * SinglePartDuration;

			if (Range > 0)
				Run(g, s, c, 1);
		}

		public override void Update(G g, State s, Combat c)
		{
			var oldTimer = timer;
			base.Update(g, s, c);

			var maxTimer = Range * SinglePartDuration;
			var oldCurrentRange = Math.Min((int)((maxTimer - oldTimer) / SinglePartDuration), Range - 1);
			var newCurrentRange = Math.Min((int)((maxTimer - timer) / SinglePartDuration), Range - 1);

			for (var i = oldCurrentRange; i < newCurrentRange; i++)
				Run(g, s, c, i + 2);
		}

		private void Run(G g, State state, Combat combat, int offset)
		{
			var targetShip = TargetPlayer ? state.ship : combat.otherShip;

			void RunAt(int worldX)
			{
				if (targetShip.GetPartAtWorldX(worldX) is not { } part || part.type == PType.empty)
					return;

				var hitShield = targetShip.Get(Status.shield) + targetShip.Get(Status.tempShield) > 0;
				var damageDone = new DamageDone
				{
					hitShield = hitShield,
					hitHull = !hitShield
				};

				if (IsStunwave)
					new AStunPart { worldX = worldX }.FullyRun(g, state, combat);
				if (Damage is { } damage)
					damageDone = targetShip.NormalDamage(state, combat, damage, worldX, piercing: IsPiercing);

				var raycastResult = new RaycastResult
				{
					hitShip = true,
					worldX = worldX
				};
				EffectSpawner.Cannon(g, TargetPlayer, raycastResult, damageDone, isBeam: false);
			}

			RunAt(WorldX - offset);
			RunAt(WorldX + offset);
		}
	}
}