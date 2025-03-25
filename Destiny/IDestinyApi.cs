namespace Shockah.Destiny;

public interface IDestinyApi
{
	void RegisterHook(IHook hook, double priority = 0);
	void UnregisterHook(IHook hook);
	
	public interface IHook
	{
		void ModifyExplosiveDamage(IModifyExplosiveDamageArgs args) { }
		void OnExplosiveTrigger(IOnExplosiveTriggerArgs args) { }

		public interface IModifyExplosiveDamageArgs
		{
			State State { get; }
			Combat Combat { get; }
			Card? Card { get; }
			int BaseDamage { get; }
			int CurrentDamage { get; set; }
		}
		
		public interface IOnExplosiveTriggerArgs
		{
			State State { get; }
			Combat Combat { get; }
			Card? Card { get; }
			AAttack AttackAction { get; }
		}
	}
}