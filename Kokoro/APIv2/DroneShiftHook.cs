using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IDroneShiftHookApi DroneShiftHook { get; }

		public interface IDroneShiftHookApi
		{
			IDroneShiftActionEntry DefaultAction { get; }
			IDroneShiftPaymentOption DefaultActionPaymentOption { get; }
			IDroneShiftPaymentOption DebugActionPaymentOption { get; }
			
			IDroneShiftActionEntry RegisterAction(IDroneShiftAction action, double priority = 0);

			IDroneShiftPrecondition.IResult MakePreconditionResult(bool isAllowed);
			IDroneShiftPostcondition.IResult MakePostconditionResult(bool isAllowed);
			
			void RegisterHook(IHook hook, double priority = 0);
			void UnregisterHook(IHook hook);
			
			public enum Direction
			{
				Left = -1,
				Right = 1
			}

			public interface IDroneShiftActionEntry
			{
				IDroneShiftAction Action { get; }
				IEnumerable<IDroneShiftPaymentOption> PaymentOptions { get; }
				IEnumerable<IDroneShiftPrecondition> Preconditions { get; }
				IEnumerable<IDroneShiftPostcondition> Postconditions { get; }

				IDroneShiftActionEntry RegisterPaymentOption(IDroneShiftPaymentOption paymentOption, double priority = 0);
				IDroneShiftActionEntry UnregisterPaymentOption(IDroneShiftPaymentOption paymentOption);

				IDroneShiftActionEntry RegisterPrecondition(IDroneShiftPrecondition precondition, double priority = 0);
				IDroneShiftActionEntry UnregisterPrecondition(IDroneShiftPrecondition precondition);

				IDroneShiftActionEntry RegisterPostcondition(IDroneShiftPostcondition postcondition, double priority = 0);
				IDroneShiftActionEntry UnregisterPostcondition(IDroneShiftPostcondition postcondition);
			}
			
			public interface IDroneShiftAction
			{
				bool CanDoDroneShiftAction(ICanDoDroneShiftArgs args);
				IReadOnlyList<CardAction> ProvideDroneShiftActions(IProvideDroneShiftActionsArgs args);
				void DroneShiftButtonHovered(IDroneShiftButtonHoveredArgs args) { }

				public interface ICanDoDroneShiftArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
				}

				public interface IProvideDroneShiftActionsArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IDroneShiftPaymentOption PaymentOption { get; }
				}

				public interface IDroneShiftButtonHoveredArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
				}
			}

			public interface IDroneShiftPaymentOption
			{
				bool CanPayForDroneShift(ICanPayForDroneShiftArgs args);
				IReadOnlyList<CardAction> ProvideDroneShiftPaymentActions(IProvideDroneShiftPaymentActionsArgs args);
				void DroneShiftButtonHovered(IDroneShiftButtonHoveredArgs args) { }
				
				public interface ICanPayForDroneShiftArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IDroneShiftActionEntry Entry { get; }
				}
				
				public interface IProvideDroneShiftPaymentActionsArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IDroneShiftActionEntry Entry { get; }
				}

				public interface IDroneShiftButtonHoveredArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IDroneShiftActionEntry Entry { get; }
				}
			}

			public interface IDroneShiftPrecondition
			{
				IResult IsDroneShiftAllowed(IIsDroneShiftAllowedArgs args);
				void DroneShiftButtonHovered(IDroneShiftButtonHoveredArgs args) { }

				public interface IResult
				{
					bool IsAllowed { get; set; }
					bool ShakeShipOnFail { get; set; }
					IList<CardAction> ActionsOnFail { get; set; }
					
					IResult SetIsAllowed(bool value);
					IResult SetShakeShipOnFail(bool value);
					IResult SetActionsOnFail(IList<CardAction> value);
				}
				
				public interface IIsDroneShiftAllowedArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IDroneShiftActionEntry Entry { get; }
					bool ForRendering { get; }
				}

				public interface IDroneShiftButtonHoveredArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IDroneShiftActionEntry Entry { get; }
					IResult Result { get; }
				}
			}

			public interface IDroneShiftPostcondition
			{
				IResult IsDroneShiftAllowed(IIsDroneShiftAllowedArgs args);
				void DroneShiftButtonHovered(IDroneShiftButtonHoveredArgs args) { }

				public interface IResult
				{
					bool IsAllowed { get; set; }
					bool ShakeShipOnFail { get; set; }
					IList<CardAction> ActionsOnFail { get; set; }
					
					IResult SetIsAllowed(bool value);
					IResult SetShakeShipOnFail(bool value);
					IResult SetActionsOnFail(IList<CardAction> value);
				}
				
				public interface IIsDroneShiftAllowedArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IDroneShiftActionEntry Entry { get; }
					IDroneShiftPaymentOption PaymentOption { get; }
					bool ForRendering { get; }
				}

				public interface IDroneShiftButtonHoveredArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IDroneShiftActionEntry Entry { get; }
					IDroneShiftPaymentOption PaymentOption { get; }
					IResult Result { get; }
				}
			}
			
			public interface IHook : IKokoroV2ApiHook
			{
				bool IsDroneShiftActionEnabled(IIsDroneShiftActionEnabledArgs args) => true;
				bool IsDroneShiftPaymentOptionEnabled(IIsDroneShiftPaymentOptionEnabledArgs args) => true;
				bool IsDroneShiftPreconditionEnabled(IIsDroneShiftPreconditionEnabledArgs args) => true;
				bool IsDroneShiftPostconditionEnabled(IIsDroneShiftPostconditionEnabledArgs args) => true;
				void DroneShiftPreconditionFailed(IDroneShiftPreconditionFailedArgs args) { }
				void DroneShiftPostconditionFailed(IDroneShiftPostconditionFailedArgs args) { }
				void AfterDroneShift(IAfterDroneShiftArgs args) { }

				public interface IIsDroneShiftActionEnabledArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IDroneShiftActionEntry Entry { get; }
				}

				public interface IIsDroneShiftPaymentOptionEnabledArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IDroneShiftActionEntry Entry { get; }
					IDroneShiftPaymentOption PaymentOption { get; }
				}

				public interface IIsDroneShiftPreconditionEnabledArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IDroneShiftActionEntry Entry { get; }
					IDroneShiftPrecondition Precondition { get; }
				}

				public interface IIsDroneShiftPostconditionEnabledArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IDroneShiftActionEntry Entry { get; }
					IDroneShiftPaymentOption PaymentOption { get; }
					IDroneShiftPostcondition Postcondition { get; }
				}

				public interface IDroneShiftPreconditionFailedArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IDroneShiftActionEntry Entry { get; }
					IDroneShiftPrecondition Precondition { get; }
					IReadOnlyList<CardAction> QueuedActions { get; }
				}

				public interface IDroneShiftPostconditionFailedArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IDroneShiftActionEntry Entry { get; }
					IDroneShiftPaymentOption PaymentOption { get; }
					IDroneShiftPostcondition Postcondition { get; }
					IReadOnlyList<CardAction> QueuedActions { get; }
				}

				public interface IAfterDroneShiftArgs
				{
					State State { get; }
					Combat Combat { get; }
					Direction Direction { get; }
					IDroneShiftActionEntry Entry { get; }
					IDroneShiftPaymentOption PaymentOption { get; }
					IReadOnlyList<CardAction> QueuedActions { get; }
				}
			}
		}
	}
}
