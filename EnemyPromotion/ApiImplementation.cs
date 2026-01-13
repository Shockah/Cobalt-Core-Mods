using System;
using Nickel;

namespace Shockah.EnemyPromotion;

public sealed class ApiImplementation : IEnemyPromotionApi
{
	public bool CanPromoteEnemy(State state, AI enemy)
		=> ModEntry.Instance.PromotionHandlers.ContainsKey(enemy.Key());

	public AI? PromoteEnemy(State state, Vec position, AI enemy)
	{
		if (!ModEntry.Instance.PromotionHandlers.TryGetValue(enemy.Key(), out var handler))
			return null;

		var args = ModEntry.Instance.ArgsPool.Get<RawPromotedEnemyHandlerArgs>();
		try
		{
			args.State = state;
			args.Position = position;
			args.Enemy = enemy;
			return handler(args);
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}

	public void RegisterPromotedEnemy<T>(Func<IEnemyPromotionApi.IPromotedEnemyHandlerArgs<T>, AI> handler) where T : AI
	{
		if (DB.enemies.FirstOrNull(kvp => kvp.Value == typeof(T))?.Key is not { } enemyKey)
			throw new ArgumentException($"Unknown enemy type {typeof(T).FullName}");
		RegisterPromotedEnemy(enemyKey, args => handler(new UpcastingPromotedEnemyHandlerArgs<T>(args)));
	}

	public void RegisterPromotedEnemy(string enemyKey, Func<IEnemyPromotionApi.IPromotedEnemyHandlerArgs<AI>, AI> handler)
		=> ModEntry.Instance.PromotionHandlers[enemyKey] = handler;

	private sealed class RawPromotedEnemyHandlerArgs : IEnemyPromotionApi.IPromotedEnemyHandlerArgs<AI>
	{
		public State State { get; internal set; } = null!;
		public Vec Position { get; internal set; }
		public AI Enemy { get; internal set; } = null!;
	}

	private sealed class UpcastingPromotedEnemyHandlerArgs<T>(IEnemyPromotionApi.IPromotedEnemyHandlerArgs<AI> args) : IEnemyPromotionApi.IPromotedEnemyHandlerArgs<T> where T : AI
	{
		public State State
			=> args.State;

		public Vec Position
			=> args.Position;

		public T Enemy
			=> (T)args.Enemy;
	}
}