namespace Shockah.Natasha;

public interface IBlochApi
{
	CardAction MakeOnTurnEndAction(CardAction action);
	CardAction MakeSpontaneousAction(CardAction action);
}