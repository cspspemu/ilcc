using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Utils
{
	public class Scopable
	{
		static public void RefScope<TType>(ref TType Variable, TType NewValue, Action Action)
		{
			var OldValue = Variable;
			Variable = NewValue;
			try
			{
				Action();
			}
			finally
			{
				Variable = OldValue;
			}
		}
	}

	public class Scopable<TType>
	{
		public TType CurrentValue { get; private set; }

		public Scopable(TType InitialValue = default(TType))
		{
			this.CurrentValue = InitialValue;
		}

		public void Scope(TType NewValue, Action Action)
		{
			var PreviousValue = CurrentValue;
			CurrentValue = NewValue;
			try
			{
				Action();
			}
			finally
			{
				CurrentValue = PreviousValue;
			}
		}
	}
}
