namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IEnergyAsStatusApi EnergyAsStatus { get; }

		public interface IEnergyAsStatusApi
		{
			public interface IStatusAction : ICardAction<AStatus>;

			IVariableHint? AsVariableHint(AVariableHint action);
			IVariableHint MakeVariableHint(int? tooltipOverride = null);

			IStatusAction? AsStatusAction(AStatus action);
			IStatusAction MakeStatusAction(int amount);
			
			public interface IVariableHint : ICardAction<AVariableHint>
			{
				int? TooltipOverride { get; set; }

				IVariableHint SetTooltipOverride(int? value);
			}
		}
	}
}
