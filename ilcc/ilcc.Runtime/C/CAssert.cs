using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcc.Runtime.C
{
	unsafe public sealed class CAssert
	{
		static public void _wassert(char* Message, char* File, uint Line)
		{
			throw (new Exception(String.Format("Assert failed! {0} at {1}:{2}", CLibUtils.GetStringFromPointerWide(Message), CLibUtils.GetStringFromPointerWide(File), Line)));
		}
	}
}
