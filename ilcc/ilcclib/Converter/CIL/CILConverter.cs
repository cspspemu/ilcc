using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Parser;
using ilcclib.Compiler;
using Codegen;
using System.Reflection.Emit;
using System.Reflection;
using ilcclib.Types;
using ilcclib.Utils;
using System.Diagnostics;
using ilcc.Runtime;

namespace ilcclib.Converter.CIL
{
#if false
	static internal class NodeExtensions
	{
		static public void TraverseChilds(this CParser.Node Node)
		{
		}
	}
#endif

	[CConverter(Id = "cil", Description = "Outputs .NET IL code (not fully implemented yet)")]
	public class CILConverter : ICConverter
	{
		CCompiler CCompiler;
		AssemblyBuilder AssemblyBuilder;
		ModuleBuilder ModuleBuilder;
		//TypeBuilder RootTypeBuilder;
		//string OutName = "_out.dll";
		string OutName = "_out.exe";
		TypeBuilder CurrentClass;
		MethodBuilder CurrentMethod;
		SafeILGenerator SafeILGenerator;
		SafeILGenerator StaticInitializerSafeILGenerator;
		MethodInfo EntryPoint = null;

		static Type ConvertCTypeToType(CType CType)
		{
			return typeof(int);
		}

		CNodeTraverser __CNodeTraverser = new CNodeTraverser();

		public CILConverter()
		{
			__CNodeTraverser.AddClassMap(this);
		}

		[DebuggerHidden]
		private void Traverse(params CParser.Node[] Nodes)
		{
			__CNodeTraverser.Traverse(Nodes);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="CCompiler"></param>
		/// <param name="Program"></param>
		void ICConverter.ConvertProgram(CCompiler CCompiler, CParser.Program Program)
		{
			this.CCompiler = CCompiler;
			Traverse(Program);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Program"></param>
		[CNodeTraverser]
		public void Program(CParser.Program Program)
		{
			this.AssemblyBuilder = SafeAssemblyUtils.CreateAssemblyBuilder("TestOut", @".");
			this.ModuleBuilder = this.AssemblyBuilder.CreateModuleBuilder(OutName);
			var RootTypeBuilder = this.ModuleBuilder.DefineType("Program", TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
			var InitializerBuilder = RootTypeBuilder.DefineTypeInitializer();
			var CurrentStaticInitializerSafeILGenerator = new SafeILGenerator(InitializerBuilder.GetILGenerator(), CheckTypes: false, DoDebug: false, DoLog: false);

			Scopable.RefScope(ref this.StaticInitializerSafeILGenerator, CurrentStaticInitializerSafeILGenerator, () =>
			{
				Scopable.RefScope(ref this.CurrentClass, RootTypeBuilder, () =>
				{
					try
					{
						Traverse(Program.Declarations);
					}
					finally
					{
						this.StaticInitializerSafeILGenerator.Return();
						RootTypeBuilder.CreateType();

						if (EntryPoint != null) this.AssemblyBuilder.SetEntryPoint(EntryPoint);
						this.AssemblyBuilder.Save(OutName);
					}
				});
			});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="VariableDeclaration"></param>
		[CNodeTraverser]
		public void VariableDeclaration(CParser.VariableDeclaration VariableDeclaration)
		{
			CurrentClass.DefineField(
				VariableDeclaration.Symbol.Name,
				ConvertCTypeToType(VariableDeclaration.Symbol.Type),
				FieldAttributes.Static | FieldAttributes.Public
			);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="FunctionDeclaration"></param>
		[CNodeTraverser]
		public void FunctionDeclaration(CParser.FunctionDeclaration FunctionDeclaration)
		{
			var FunctionName = FunctionDeclaration.CFunctionType.Name;

			var CurrentMethod = CurrentClass.DefineMethod(
				FunctionName,
				MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard,
				typeof(void),
				new Type[] { }
			);

			if (FunctionName == "main")
			{
				EntryPoint = CurrentMethod;
			}

			Scopable.RefScope(ref this.CurrentMethod, CurrentMethod, () =>
			{
				var ILGenerator = CurrentMethod.GetILGenerator();
				var CurrentSafeILGenerator = new SafeILGenerator(ILGenerator, CheckTypes: false, DoDebug: false, DoLog: false);

				Scopable.RefScope(ref this.SafeILGenerator, CurrentSafeILGenerator, () =>
				{
					Traverse(FunctionDeclaration.FunctionBody);
					SafeILGenerator.Return();
				});
			});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="CompoundStatement"></param>
		[CNodeTraverser]
		public void CompoundStatement(CParser.CompoundStatement CompoundStatement)
		{
			Traverse(CompoundStatement.Statements);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ExpressionStatement"></param>
		[CNodeTraverser]
		public void ExpressionStatement(CParser.ExpressionStatement ExpressionStatement)
		{
			Traverse(ExpressionStatement.Expression);
			SafeILGenerator.PopLeft();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="FunctionCallExpression"></param>
		[CNodeTraverser]
		public void FunctionCallExpression(CParser.FunctionCallExpression FunctionCallExpression)
		{
			var Function = FunctionCallExpression.Function;
			if (Function is CParser.IdentifierExpression)
			{
				var IdentifierExpression = Function as CParser.IdentifierExpression;

				if (IdentifierExpression.Value != "puts")
				{
					throw(new NotImplementedException());
				}
				Traverse(FunctionCallExpression.Parameters.Expressions);
				SafeILGenerator.Call(typeof(CLib).GetMethod("puts"));
				//SafeILGenerator.__ILGenerator.Emit(OpCodes.Call
				//throw (new NotImplementedException("Function: " + IdentifierExpression.Value));
			}
			else
			{
				throw(new NotImplementedException());
			}
		}

		private int UniqueId = 0;
		private Dictionary<string, FieldInfo> StringCache = new Dictionary<string, FieldInfo>();

		private FieldInfo GetStringPointerField(string String)
		{
			if (!StringCache.ContainsKey(String))
			{
				var FieldBuilder = CurrentClass.DefineField("$$__string_literal_" + (UniqueId++), typeof(sbyte*), FieldAttributes.Static | FieldAttributes.Public | FieldAttributes.InitOnly | FieldAttributes.HasDefault);

				this.StaticInitializerSafeILGenerator.Push(String);
				this.StaticInitializerSafeILGenerator.Call(typeof(CLibUtils).GetMethod("GetLiteralStringPointer"));
				this.StaticInitializerSafeILGenerator.StoreField(FieldBuilder);
				StringCache[String] = FieldBuilder;
			}

			return StringCache[String];
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="StringExpression"></param>
		[CNodeTraverser]
		public void StringExpression(CParser.StringExpression StringExpression)
		{
			
			//CurrentClass.DefineField(
			//SafeILGenerator.LoadNull();
			//SafeILGenerator.CastClass(CurrentClass);

			SafeILGenerator.LoadField(GetStringPointerField(StringExpression.Value));
			//SafeILGenerator.Push(StringExpression.Value);
		}
	}
}
