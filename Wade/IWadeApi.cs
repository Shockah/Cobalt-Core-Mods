namespace Shockah.Wade;

public interface IWadeApi
{
	public interface IHook
	{
		void OnOddsRoll(IOnOddsRollsArgs args) { }

		public interface IOnOddsRollsArgs
		{
			State State { get; }
			Combat Combat { get; }
			Ship Ship { get; }
			bool IsTurnStart { get; }
			int OldOdds { get; }
			int NewOdds { get; }
		}
	}
}