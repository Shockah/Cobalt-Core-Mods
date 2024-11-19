namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IActionApi
	{
		AVariableHint SetTargetPlayer(AVariableHint action, bool targetPlayer);
	}
}
