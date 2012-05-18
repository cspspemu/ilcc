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
		[CNodeTraverser]
		public void Program(CParser.TranslationUnit Program)
		{
			Console.WriteLine("using System");
			Console.WriteLine("");
			Console.WriteLine("static public class Invoke");
			Console.WriteLine("{");
			Console.WriteLine("\tconst string DllName = \"mydll.dll\";");
			Traverse(Program.Declarations);
			Console.WriteLine("}");
		}

		[CNodeTraverser]
		public void DeclarationList(CParser.DeclarationList DeclarationList)
		{
			Traverse(DeclarationList.Declarations);
		}

		[CNodeTraverser]
		public void VariableDeclaration(CParser.VariableDeclaration VariableDeclaration)
		{
		}

		[CNodeTraverser]
		public void TypeDeclaration(CParser.TypeDeclaration TypeDeclaration)
		{
			var CStructType = TypeDeclaration.Symbol.Type.GetCStructType();
			if (CStructType != null)
			{
				Console.WriteLine("");
				Console.WriteLine("\t/// <summary>");
				Console.WriteLine("\t/// </summary>");
				Console.WriteLine("\tpublic struct {0}", TypeDeclaration.Symbol.Name);
				Console.WriteLine("\t{");
				{
					for (int n = 0; n < CStructType.Items.Count; n++)
					{
						var Item = CStructType.Items[n];
						if (n != 0)
						{
							Console.WriteLine("");
						}
						Console.WriteLine("\t\t/// <summary>");
						Console.WriteLine("\t\t/// </summary>");
						Console.WriteLine("\t\tpublic {0} {1};", ConvertCTypeToTypeString(Item.Type), Item.Name);
					}
				}
				Console.WriteLine("\t}");
			}
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
