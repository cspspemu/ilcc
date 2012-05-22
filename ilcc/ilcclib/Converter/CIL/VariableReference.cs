using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Codegen;
using System.Reflection.Emit;
using System.Reflection;
using ilcclib.Types;

namespace ilcclib.Converter.CIL
{
	public class VariableReference
	{
		//public CSymbol CSymbol;
		public string Name;
		public CType CType;
		private FieldInfo Field;
		private LocalBuilder Local;
		private SafeArgument Argument;

		public VariableReference(string Name, CType CType, FieldInfo Field)
		{
			this.Name = Name;
			this.CType = CType;
			this.Field = Field;
		}

		public VariableReference(string Name, CType CType, LocalBuilder Local)
		{
			this.Name = Name;
			this.CType = CType;
			this.Local = Local;
		}

		public VariableReference(string Name, CType CType, SafeArgument Argument)
		{
			this.Name = Name;
			this.CType = CType;
			this.Argument = Argument;
		}

		public void Load(SafeILGenerator SafeILGenerator)
		{
			if (Field != null)
			{
				SafeILGenerator.LoadField(Field);
			}
			else if (Local != null)
			{
				//Console.WriteLine("Load local!");
				SafeILGenerator.LoadLocal(Local);
			}
			else if (Argument != null)
			{
				SafeILGenerator.LoadArgument(Argument);
			}
			else
			{
				throw (new Exception("Invalid Variable Reference"));
			}
		}

		public void LoadAddress(SafeILGenerator SafeILGenerator)
		{
			if (Field != null)
			{
				SafeILGenerator.LoadFieldAddress(Field);
			}
			else if (Local != null)
			{
				SafeILGenerator.LoadLocalAddress(Local);
				//SafeILGenerator.LoadLocal(Local);
			}
			else
			{
				SafeILGenerator.LoadArgumentAddress(Argument);
			}
		}
	}
}
