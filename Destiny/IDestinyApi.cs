namespace Shockah.Destiny;

public interface IDestinyApi
{
	void RegisterHook(IHook hook, double priority = 0);
	void UnregisterHook(IHook hook);
	
	public interface IHook
	{
		void ModifyExplosiveDamage(IModifyExplosiveDamageArgs args) { }
		void OnExplosiveTrigger(IOnExplosiveTriggerArgs args) { }
		void OnPristineShieldTrigger(IOnPristineShieldTriggerArgs args) { }

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
		
		public interface IOnPristineShieldTriggerArgs
		{
			State State { get; }
			Combat Combat { get; }
			Ship Ship { get; }
			int Damage { get; }
			bool TickDown { get; set; }
		}
	}
}