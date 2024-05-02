namespace EvilRiggs
{
	internal abstract class SequentialCard : Card
	{
		public bool SequenceInitiated;

		public override void OnDraw(State s, Combat c)
		{
			base.OnDraw(s, c);
			SequenceInitiated = false;
		}
	}
}