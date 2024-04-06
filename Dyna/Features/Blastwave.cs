using HarmonyLib;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;

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
	private static ISpriteEntry StunBlastwaveIcon = null!;

	private static AAttack? AttackContext;

	public BlastwaveManager()
	{
		BlastwaveIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icons/Blastwave.png"));
		WideBlastwaveIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icons/BlastwaveWide.png"));
		StunBlastwaveIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icons/BlastwaveStun.png"));

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.GetTooltips)),
			postfix: new HarmonyMethod(GetType(), nameof(AAttack_GetTooltips_Postfix))
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
			Source = attack,
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

	private static void AAttack_GetTooltips_Postfix(AAttack __instance, State s, ref List<Tooltip> __result)
	{
		if (!__instance.IsBlastwave())
			return;

		__result.AddRange(new BlastwaveAction
		{
			Source = __instance,
			TargetPlayer = __instance.targetPlayer,
			WorldX = 0,
			Damage = __instance.GetBlastwaveDamage(),
			Range = __instance.GetBlastwaveRange(),
			IsStunwave = __instance.IsStunwave(),
		}.GetTooltips(s));
	}

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
			ISpriteEntry icon;
			if (attack.IsStunwave())
				icon = StunBlastwaveIcon;
			else if (attack.GetBlastwaveRange() >= 2)
				icon = WideBlastwaveIcon;
			else
				icon = BlastwaveIcon;

			Draw.Sprite(icon.Sprite, initialX + __result, position.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
		}
		__result += 9;

		if (attack.GetBlastwaveDamage() is { } damage)
		{
			__result++;
			if (!dontDraw)
				BigNumbers.Render(damage, initialX + __result, position.y, action.disabled ? Colors.disabledText : Colors.redd);
			__result += damage.ToString().Length * 6;
		}

		return false;
	}

	internal sealed class BlastwaveAction : CardAction
	{
		private const double SinglePartDuration = 0.2;

		public required AAttack Source;
		public bool TargetPlayer = false;
		public required int WorldX;
		public required int? Damage;
		public int Range = 1;
		public bool IsStunwave;
		public bool IsPiercing = false;

		public override List<Tooltip> GetTooltips(State s)
		{
			string key;
			string name;
			string description;
			ISpriteEntry icon;

			if (IsStunwave)
			{
				icon = StunBlastwaveIcon;
				name = ModEntry.Instance.Localizations.Localize(["action", "Blastwave", "name", "Stun"]);
				if (Damage is { } damage)
				{
					key = $"{ModEntry.Instance.Package.Manifest.UniqueName}::Blastwave::Stun::StunDamage";
					description = ModEntry.Instance.Localizations.Localize(["action", "Blastwave", "description", "StunDamage"], new { Range, Damage = damage });
				}
				else
				{
					key = $"{ModEntry.Instance.Package.Manifest.UniqueName}::Blastwave::Stun::Stun";
					description = ModEntry.Instance.Localizations.Localize(["action", "Blastwave", "description", "Stun"], new { Range });
				}
			}
			else if (Range >= 2)
			{
				icon = WideBlastwaveIcon;
				key = $"{ModEntry.Instance.Package.Manifest.UniqueName}::Blastwave::Wide::Damage";
				name = ModEntry.Instance.Localizations.Localize(["action", "Blastwave", "name", "Wide"]);
				description = ModEntry.Instance.Localizations.Localize(["action", "Blastwave", "description", "Damage"], new { Range, Damage = Damage ?? 0 });
			}
			else
			{
				icon = BlastwaveIcon;
				key = $"{ModEntry.Instance.Package.Manifest.UniqueName}::Blastwave::Normal::Damage";
				name = ModEntry.Instance.Localizations.Localize(["action", "Blastwave", "name", "Normal"]);
				description = ModEntry.Instance.Localizations.Localize(["action", "Blastwave", "description", "Damage"], new { Range, Damage = Damage ?? 0 });
			}

			List<Tooltip> tooltips = [
				new CustomTTGlossary(
					CustomTTGlossary.GlossaryType.action,
					() => icon.Sprite,
					() => name,
					() => description,
					key: key
				)
			];

			if (IsStunwave)
				tooltips.Add(new TTGlossary("action.stun"));

			return tooltips;
		}

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = Range * SinglePartDuration;

			if (Range > 0)
				Run(g, s, c, 1);

			var targetShip = TargetPlayer ? s.ship : c.otherShip;
			foreach (var hook in ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.KokoroApi, s.EnumerateAllArtifacts()))
				hook.OnBlastwaveTrigger(s, c, targetShip, WorldX);
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
				EffectSpawnerExt.HitEffect(g, TargetPlayer, raycastResult, damageDone);

				if (!TargetPlayer)
				{
					combat.otherShip.ai?.OnHitByAttack(state, combat, worldX, Source);
					foreach (var artifact in state.EnumerateAllArtifacts())
						artifact.OnEnemyGetHit(state, combat, part);
				}

				foreach (var hook in ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.KokoroApi, state.EnumerateAllArtifacts()))
					hook.OnBlastwaveHit(state, combat, targetShip, worldX, WorldX);
			}

			RunAt(WorldX - offset);
			RunAt(WorldX + offset);
		}
	}
}