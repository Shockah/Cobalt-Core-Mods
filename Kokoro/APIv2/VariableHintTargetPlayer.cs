namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IVariableHintTargetPlayerApi VariableHintTargetPlayerTargetPlayer { get; }

		public interface IVariableHintTargetPlayerApi
		{
			public interface IVariableHint : ICardAction<AVariableHint>
			{
				bool TargetPlayer { get; set; }

				IVariableHint SetTargetPlayer(bool value);
			}
			
			IVariableHint? AsVariableHint(AVariableHint action);
			IVariableHint MakeVariableHint(AVariableHint action);
		}
	}
}
