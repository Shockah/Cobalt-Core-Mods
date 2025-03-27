namespace Shockah.Destiny;

internal sealed class ADelayNonSkippable : ADelay
{
	public override bool CanSkipTimerIfLastEvent()
		=> false;
}