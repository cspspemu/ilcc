using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Converter
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class CConverterAttribute : Attribute
	{
		public string Id { get; set; }

		public string Description { get; set; }

		public override string ToString()
		{
			return String.Format("{0} - {1}", Id, Description);
		}
	}
}
