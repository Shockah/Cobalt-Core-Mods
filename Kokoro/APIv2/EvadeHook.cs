using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IEvadeHookApi EvadeHook { get; }

		public interface IEvadeHookApi
		{
			IEvadeActionEntry DefaultAction { get; }
			IEvadePaymentOption DefaultActionPaymentOption { get; }
			IEvadePaymentOption DebugActionPaymentOption { get; }
			IEvadePrecondition DefaultActionAnchorPrecondition { get; }
			IEvadePrecondition DefaultActionEngineLockPrecondition { get; }
			IEvadePostcondition DefaultActionEngineStallPostcondition { get; }
			
			IEvadeActionEntry RegisterAction(IEvadeAction action, double priority = 0);

			IEvadePrecondition.IResult MakePreconditionResult(bool isAllowed);
			IEvadePostcondition.IResult MakePostconditionResult(bool isAllowed);
			
			void RegisterHook(IHook hook, double priority = 0);
			void UnregisterHook(IHook hook);
			
			public enum Direction
			{
				Left = -1,
				Right = 1
			}

			public interface IEvadeActionEntry
			{
				IEvadeAction Action { get; }
				IEnumerable<IEvadePaymentOption> PaymentOptions { get; }
				IEnumerable<IEvadePrecondition> Preconditions { get; }
				IEnumerable<IEvadePostcondition> Postconditions { get; }

				IEvadeActionEntry RegisterPaymentOption(IEvadePaymentOption paymentOption, double priority = 0);
				IEvadeActionEntry UnregisterPaymentOption(IEvadePaymentOption paymentOption);

				IEvadeActionEntry RegisterPrecondition(IEvadePrecondition precondition, double priority = 0);
				IEvadeActionEntry UnregisterPrecondition(IEvadePrecondition precondition);

				IEvadeActionEntry RegisterPostcondition(IEvadePostcondition postcondition, double priority = 0);
				IEvadeActionEntry UnregisterPostcondition(IEvadePostcondition postcondition);
			}
			
			public interface IEvadeAction
			{
				bool CanDoEvadeAction(ICanDoEvadeArgs args);
				IReadOnlyList<CardAction> ProvideEvadeActions(IProvideEvadeActionsArgs args);
				
				public interface ICanDoEvadeArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
				}

				public interface IProvideEvadeActionsArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IEvadePaymentOption PaymentOption { get; }
				}
			}

			public interface IEvadePaymentOption
			{
				bool CanPayForEvade(ICanPayForEvadeArgs args);
				IReadOnlyList<CardAction> ProvideEvadePaymentActions(IProvideEvadePaymentActionsArgs args);
				
				public interface ICanPayForEvadeArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IEvadeActionEntry Entry { get; }
				}
				
				public interface IProvideEvadePaymentActionsArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IEvadeActionEntry Entry { get; }
				}
			}

			public interface IEvadePrecondition
			{
				IResult IsEvadeAllowed(IIsEvadeAllowedArgs args);

				public interface IResult
				{
					bool IsAllowed { get; set; }
					bool ShakeShipOnFail { get; set; }
					IList<CardAction> ActionsOnFail { get; set; }
					
					IResult SetIsAllowed(bool value);
					IResult SetShakeShipOnFail(bool value);
					IResult SetActionsOnFail(IList<CardAction> value);
				}
				
				public interface IIsEvadeAllowedArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IEvadeActionEntry Entry { get; }
					bool ForRendering { get; }
				}
			}

			public interface IEvadePostcondition
			{
				IResult IsEvadeAllowed(IIsEvadeAllowedArgs args);

				public interface IResult
				{
					bool IsAllowed { get; set; }
					bool ShakeShipOnFail { get; set; }
					IList<CardAction> ActionsOnFail { get; set; }
					
					IResult SetIsAllowed(bool value);
					IResult SetShakeShipOnFail(bool value);
					IResult SetActionsOnFail(IList<CardAction> value);
				}
				
				public interface IIsEvadeAllowedArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IEvadeActionEntry Entry { get; }
					IEvadePaymentOption PaymentOption { get; }
				}
			}
			
			public interface IHook : IKokoroV2ApiHook
			{
				bool IsEvadeActionEnabled(IIsEvadeActionEnabledArgs args) => true;
				bool IsEvadePaymentOptionEnabled(IIsEvadePaymentOptionEnabledArgs args) => true;
				bool IsEvadePreconditionEnabled(IIsEvadePreconditionEnabledArgs args) => true;
				bool IsEvadePostconditionEnabled(IIsEvadePostconditionEnabledArgs args) => true;
				void EvadePreconditionFailed(IEvadePreconditionFailedArgs args) { }
				void EvadePostconditionFailed(IEvadePostconditionFailedArgs args) { }
				void AfterEvade(IAfterEvadeArgs args) { }

				public interface IIsEvadeActionEnabledArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IEvadeActionEntry Entry { get; }
				}

				public interface IIsEvadePaymentOptionEnabledArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IEvadeActionEntry Entry { get; }
					IEvadePaymentOption PaymentOption { get; }
				}

				public interface IIsEvadePreconditionEnabledArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IEvadeActionEntry Entry { get; }
					IEvadePrecondition Precondition { get; }
				}

				public interface IIsEvadePostconditionEnabledArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IEvadeActionEntry Entry { get; }
					IEvadePaymentOption PaymentOption { get; }
					IEvadePostcondition Postcondition { get; }
				}

				public interface IEvadePreconditionFailedArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IEvadeActionEntry Entry { get; }
					IEvadePrecondition Precondition { get; }
					IReadOnlyList<CardAction> QueuedActions { get; }
				}

				public interface IEvadePostconditionFailedArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IEvadeActionEntry Entry { get; }
					IEvadePaymentOption PaymentOption { get; }
					IEvadePostcondition Postcondition { get; }
					IReadOnlyList<CardAction> QueuedActions { get; }
				}

				public interface IAfterEvadeArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IEvadeActionEntry Entry { get; }
					IEvadePaymentOption PaymentOption { get; }
					IReadOnlyList<CardAction> QueuedActions { get; }
				}
			}
		}
	}
}
