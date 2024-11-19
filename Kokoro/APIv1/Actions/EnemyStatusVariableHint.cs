namespace Shockah.Kokoro;

// TODO: V2

public partial interface IKokoroApi
{
	public partial interface IActionApi
	{
		AVariableHint SetTargetPlayer(AVariableHint action, bool targetPlayer);
	}
}
