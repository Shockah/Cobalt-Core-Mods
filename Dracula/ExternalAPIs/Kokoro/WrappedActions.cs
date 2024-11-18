﻿using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IWrappedActionsApi WrappedActions { get; }

		public interface IWrappedActionsApi
		{
			void RegisterHook(IHook hook, double priority = 0);
			void UnregisterHook(IHook hook);

			IEnumerable<CardAction>? GetWrappedCardActions(CardAction action);
			IEnumerable<CardAction> GetWrappedCardActionsRecursively(CardAction action);
			IEnumerable<CardAction> GetWrappedCardActionsRecursively(CardAction action, bool includingWrapperActions);
			
			public interface IHook
			{
				IEnumerable<CardAction>? GetWrappedCardActions(CardAction action);
			}
		}
	}
}