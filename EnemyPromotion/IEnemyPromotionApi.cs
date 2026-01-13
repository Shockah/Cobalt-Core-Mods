using System;

namespace Shockah.EnemyPromotion;

public interface IEnemyPromotionApi
{
	bool CanPromoteEnemy(State state, AI enemy);
	AI? PromoteEnemy(State state, Vec position, AI enemy);
	
	void RegisterPromotedEnemy<T>(Func<IPromotedEnemyHandlerArgs<T>, AI> handler) where T : AI;
	void RegisterPromotedEnemy(string enemyKey, Func<IPromotedEnemyHandlerArgs<AI>, AI> handler);

	public interface IPromotedEnemyHandlerArgs<out T> where T : AI
	{
		State State { get; }
		Vec Position { get; }
		T Enemy { get; }
	}
}