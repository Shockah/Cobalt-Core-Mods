﻿namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	IV2 V2 { get; }
	
	public partial interface IV2
	{
		public interface ICardAction<out T> where T : CardAction
		{
			T AsCardAction { get; }
		}
		
		public interface IRoute<out T> where T : Route
		{
			T AsRoute { get; }
		}

		public interface IKokoroV2ApiHook;
		
		public interface IHookPriority
		{
			double HookPriority { get; }
		}
	}
}