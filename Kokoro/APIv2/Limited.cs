using Nickel;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		ILimitedApi Limited { get; }

		public interface ILimitedApi
		{
			ICardTraitEntry Trait { get; }
			
			int GetBaseLimitedUses(string key, Upgrade upgrade);
			void SetBaseLimitedUses(string key, int value);
			void SetBaseLimitedUses(string key, Upgrade upgrade, int value);
			int GetStartingLimitedUses(State state, Card card);
			int GetLimitedUses(State state, Card card);
			void SetLimitedUses(State state, Card card, int value);
			void ResetLimitedUses(State state, Card card);
			
			IVariableHint? AsVariableHint(AVariableHint action);
			IVariableHint MakeVariableHint(int cardId);
			
			IChangeLimitedUsesAction? AsChangeLimitedUsesAction(CardAction action);
			IChangeLimitedUsesAction MakeChangeLimitedUsesAction(int cardId, int amount, AStatusMode mode = AStatusMode.Add);

			ICardSelect MakeCardSelect(ACardSelect action);
			ICardBrowse MakeCardBrowse(CardBrowse route);
			
			void RegisterHook(IHook hook, double priority = 0);
			void UnregisterHook(IHook hook);
			
			public interface IVariableHint : ICardAction<AVariableHint>
			{
				int CardId { get; set; }

				IVariableHint SetCardId(int value);
			}
			
			public interface IChangeLimitedUsesAction : ICardAction<CardAction>
			{
				int CardId { get; set; }
				int Amount { get; set; }
				AStatusMode Mode { get; set; }

				IChangeLimitedUsesAction SetCardId(int value);
				IChangeLimitedUsesAction SetAmount(int value);
				IChangeLimitedUsesAction SetMode(AStatusMode value);
			}
			
			public interface ICardSelect : ICardAction<ACardSelect>
			{
				bool? Limited { get; set; }

				ICardSelect SetLimited(bool? value);
			}
			
			public interface ICardBrowse : IRoute<CardBrowse>
			{
				bool? Limited { get; set; }

				ICardBrowse SetLimited(bool? value);
			}

			public interface IHook : IKokoroV2ApiHook
			{
				bool ModifyLimitedUses(IModifyLimitedUsesArgs args) => false;
				bool? IsSingleUseLimited(IIsSingleUseLimitedArgs args) => null;

				public interface IModifyLimitedUsesArgs
				{
					State State { get; }
					Card Card { get; }
					int BaseUses { get; }
					int Uses { get; set; }
				}
				
				public interface IIsSingleUseLimitedArgs
				{
					State State { get; }
					Card Card { get; }
				}
			}
		}
	}
}
