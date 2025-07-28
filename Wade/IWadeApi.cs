using Nickel;
using Shockah.Kokoro;

namespace Shockah.Wade;

public interface IWadeApi
{
	IDeckEntry WadeDeck { get; }
	
	IStatusEntry OddsStatus { get; }
	IStatusEntry RedTrendStatus { get; }
	IStatusEntry GreenTrendStatus { get; }
	IStatusEntry LuckyDriveStatus { get; }

	ITrendCondition MakeTrendCondition(bool positive);
	ITrendCondition? AsTrendCondition(IKokoroApi.IV2.IConditionalApi.IBoolExpression condition);

	IRollAction MakeRollAction();
	IRollAction? AsRollAction(CardAction action);
	
	void RegisterHook(IHook hook, double priority = 0);
	void UnregisterHook(IHook hook);

	public interface ITrendCondition : IKokoroApi.IV2.IConditionalApi.IBoolExpression
	{
		bool Positive { get; set; }
		bool? OverrideValue { get; set; }

		ITrendCondition SetPositive(bool value);
		ITrendCondition SetOverrideValue(bool? value);
	}

	public interface IRollAction : IKokoroApi.IV2.ICardAction<CardAction>
	{
		bool TargetPlayer { get; set; }
		bool IsTurnStart { get; set; }
		
		IRollAction SetTargetPlayer(bool value);
		IRollAction SetIsTurnStart(bool value);
	}
	
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