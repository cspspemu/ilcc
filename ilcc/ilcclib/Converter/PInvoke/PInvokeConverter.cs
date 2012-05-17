using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Compiler;
using ilcclib.Parser;
using System.Diagnostics;
using ilcclib.Types;

namespace ilcclib.Converter.PInvoke
{
	[CConverter(Id = "pinvoke", Description = "Outputs .NET pinvoke source with function declarations and structures (not fully implemented yet)")]
	public class PInvokeConverter : TraversableCConverter
	{
		protected string ConvertCTypeToTypeString(CType CType)
		{
			var Type = ConvertCTypeToType(CType);

			if (Type == typeof(void)) return "void";

			if (Type == typeof(sbyte)) return "sbyte";
			if (Type == typeof(byte)) return "byte";

			if (Type == typeof(short)) return "short";
			if (Type == typeof(ushort)) return "ushort";

			if (Type == typeof(int)) return "int";
			if (Type == typeof(uint)) return "uint";

			if (Type == typeof(long)) return "long";
			if (Type == typeof(ulong)) return "ulong";

			//return Type.Name;
			return Type.ToString();
		}

		[CNodeTraverser]
		public void Program(CParser.TranslationUnit Program)
		{
			Console.WriteLine("using System");
			Console.WriteLine("");
			Console.WriteLine("static public class Invoke {");
			Console.WriteLine("\tconst string DllName = \"mydll.dll\";");
			Traverse(Program.Declarations);
			Console.WriteLine("}");
		}

		[CNodeTraverser]
		public void VariableDeclaration(CParser.VariableDeclaration VariableDeclaration)
		{
		}

		[CNodeTraverser]
		public void TypeDeclaration(CParser.TypeDeclaration TypeDeclaration)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="FunctionDeclaration"></param>
		[CNodeTraverser]
		public void FunctionDeclaration(CParser.FunctionDeclaration FunctionDeclaration)
		{
			if (FunctionDeclaration.FunctionBody != null)
			{
				//Console.WriteLine(FunctionDeclaration.FunctionBody);
				Console.WriteLine("");
				Console.WriteLine("\t/// <summary>");
				Console.WriteLine("\t/// </summary>");
				foreach (var Parameter in FunctionDeclaration.CFunctionType.Parameters)
				{
					Console.WriteLine("\t/// <param name=\"{0}\"></param>", Parameter.Name);
				}
				if (ConvertCTypeToType(FunctionDeclaration.CFunctionType.Return) != typeof(void))
				{
					Console.WriteLine("\t/// <returns></returns>");
				}
				Console.WriteLine("\t[DllImport(DllName)]");
				string FunctionHeader = "";
				FunctionHeader += "static public";
				FunctionHeader += " " + ConvertCTypeToTypeString(FunctionDeclaration.CFunctionType.Return);
				FunctionHeader += " " + FunctionDeclaration.CFunctionType.Name;
				FunctionHeader += "(";
				FunctionHeader += String.Join(", ", FunctionDeclaration.CFunctionType.Parameters.Select(Item =>
				{
					return ConvertCTypeToTypeString(Item.Type) + " " + Item.Name;
				}));
				FunctionHeader += ")";
				Console.WriteLine("\t{0};", FunctionHeader);
				//ConvertCTypeToType(FunctionDeclaration.CFunctionType.Return);
			}
		}
	}
}
