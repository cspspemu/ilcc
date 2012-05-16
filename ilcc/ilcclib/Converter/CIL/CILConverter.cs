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
	public class FunctionReference
	{
		public MethodInfo MethodInfo;

		public FunctionReference(MethodInfo MethodInfo)
		{
			this.MethodInfo = MethodInfo;
		}
	}

	public class VariableReference
	{
		private FieldBuilder Field;
		private LocalBuilder Local;

		public VariableReference(FieldBuilder Field)
		{
			this.Field = Field;
		}

		public VariableReference(LocalBuilder Local)
		{
			this.Local = Local;
		}

		public void Load(SafeILGenerator SafeILGenerator)
		{
			if (Field != null)
			{
				SafeILGenerator.LoadField(Field);
			}
			else
			{
				//Console.WriteLine("Load local!");
				SafeILGenerator.LoadLocal(Local);
			}
		}

		public void LoadAddress(SafeILGenerator SafeILGenerator)
		{
			if (Field != null)
			{
				SafeILGenerator.LoadFieldAddress(Field);
			}
			else
			{
				SafeILGenerator.LoadLocalAddress(Local);
				//SafeILGenerator.LoadLocal(Local);
			}
		}
	}

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
		bool IsLeftValue = false;
		AScope<VariableReference> VariableScope = new AScope<VariableReference>();
		AScope<FunctionReference> FunctionScope = new AScope<FunctionReference>();

		static Type ConvertCTypeToType(CType CType)
		{
			return typeof(int);
		}

		CNodeTraverser __CNodeTraverser = new CNodeTraverser();

		public CILConverter()
		{
			__CNodeTraverser.AddClassMap(this);
			FunctionScope.Push("puts", new FunctionReference(typeof(CLib).GetMethod("puts")));
			FunctionScope.Push("puti", new FunctionReference(typeof(CLib).GetMethod("puti")));
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
					AScope<VariableReference>.NewScope(ref this.VariableScope, () =>
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
			});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="VariableDeclaration"></param>
		[CNodeTraverser]
		public void VariableDeclaration(CParser.VariableDeclaration VariableDeclaration)
		{
			var VariableName = VariableDeclaration.Symbol.Name;
			var VariableType = ConvertCTypeToType(VariableDeclaration.Symbol.Type);

			// Global Scope
			if (this.SafeILGenerator == null)
			{
				var Field = CurrentClass.DefineField(
					VariableName,
					VariableType,
					FieldAttributes.Static | FieldAttributes.Public
				);
				var Variable = new VariableReference(Field);

				this.VariableScope.Push(VariableName, Variable);

				if (VariableDeclaration.InitialValue != null)
				{
					Scopable.RefScope(ref SafeILGenerator, StaticInitializerSafeILGenerator, () =>
					{
						Variable.LoadAddress(SafeILGenerator);
						Traverse(VariableDeclaration.InitialValue);
						SafeILGenerator.StoreIndirect<int>();
					});
				}
			}
			// Local Scope
			else
			{
				var Local = this.SafeILGenerator.DeclareLocal(VariableType, VariableName);
				var Variable = new VariableReference(Local);

				this.VariableScope.Push(VariableName, Variable);

				if (VariableDeclaration.InitialValue != null)
				{
					Variable.LoadAddress(SafeILGenerator);
					Traverse(VariableDeclaration.InitialValue);
					SafeILGenerator.StoreIndirect<int>();
				}
			}
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

			AScope<VariableReference>.NewScope(ref this.VariableScope, () =>
			{
				Scopable.RefScope(ref this.CurrentMethod, CurrentMethod, () =>
				{
					var ILGenerator = CurrentMethod.GetILGenerator();
					var CurrentSafeILGenerator = new SafeILGenerator(ILGenerator, CheckTypes: false, DoDebug: false, DoLog: true);

					Scopable.RefScope(ref this.SafeILGenerator, CurrentSafeILGenerator, () =>
					{
						Traverse(FunctionDeclaration.FunctionBody);
						SafeILGenerator.Return();
					});
#if false
					foreach (var Instruction in CurrentSafeILGenerator.GetEmittedInstructions()) Console.WriteLine(Instruction);
#endif
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
		/// <param name="CompoundStatement"></param>
		[CNodeTraverser]
		public void DeclarationList(CParser.DeclarationList DeclarationList)
		{
			Traverse(DeclarationList.Declarations);
		}

		[CNodeTraverser]
		public void ForStatement(CParser.ForStatement ForStatement)
		{
			Traverse(ForStatement.Init);
			SafeILGenerator.PopLeft();

			var EndLoopLabel = SafeILGenerator.DefineLabel("EndLoopLabel");
			var LoopCheckConditionLabel = SafeILGenerator.DefineLabel("LoopCheckConditionLabel");
			{
				LoopCheckConditionLabel.Mark();
				Traverse(ForStatement.Condition);
				SafeILGenerator.BranchIfFalse(EndLoopLabel);

				{
					Traverse(ForStatement.LoopStatements);
				}

				Traverse(ForStatement.PostOperation);
				SafeILGenerator.BranchAlways(LoopCheckConditionLabel);
			}
			EndLoopLabel.Mark();
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

				var FunctionReference = FunctionScope.Find(IdentifierExpression.Identifier);
				if (FunctionReference == null)
				{
					throw(new NotImplementedException());
				}
				Traverse(FunctionCallExpression.Parameters.Expressions);
				SafeILGenerator.Call(FunctionReference.MethodInfo);
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

			SafeILGenerator.LoadField(GetStringPointerField(StringExpression.String));
			//SafeILGenerator.Push(StringExpression.Value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="IntegerExpression"></param>
		[CNodeTraverser]
		public void IntegerExpression(CParser.IntegerExpression IntegerExpression)
		{
			SafeILGenerator.Push(IntegerExpression.Value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="IdentifierExpression"></param>
		[CNodeTraverser]
		public void IdentifierExpression(CParser.IdentifierExpression IdentifierExpression)
		{
			var Variable = VariableScope.Find(IdentifierExpression.Identifier);
			//Console.WriteLine("Ident: {0}", Variable);
			if (IsLeftValue)
			{
				//Console.WriteLine(" Left");
				Variable.LoadAddress(SafeILGenerator);
			}
			else
			{
				//Console.WriteLine(" No Left");
				Variable.Load(SafeILGenerator);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="BinaryExpression"></param>
		[CNodeTraverser]
		public void BinaryExpression(CParser.BinaryExpression BinaryExpression)
		{
			switch (BinaryExpression.Operator)
			{
				case "+":
				case "*":
				case "<":
					Traverse(BinaryExpression.Left);
					Traverse(BinaryExpression.Right);
					switch (BinaryExpression.Operator)
					{
						case "+": SafeILGenerator.BinaryOperation(SafeBinaryOperator.AdditionSigned); break;
						case "*": SafeILGenerator.BinaryOperation(SafeBinaryOperator.MultiplySigned); break;
						case "<": SafeILGenerator.CompareBinary(SafeBinaryComparison.LessThanSigned); break;
					}
					break;
				case "=":
					Scopable.RefScope(ref IsLeftValue, true, () =>
					{
						Traverse(BinaryExpression.Left);
					});
					Traverse(BinaryExpression.Right);

					SafeILGenerator.StoreIndirect(typeof(int));
					break;
				default:
					throw(new NotImplementedException());
			}
		}

		[CNodeTraverser]
		public void UnaryExpression(CParser.UnaryExpression UnaryExpression)
		{
			switch (UnaryExpression.Operator)
			{
				case "++":
					Scopable.RefScope(ref IsLeftValue, true, () =>
					{
						Traverse(UnaryExpression.Right);
					});
					Traverse(UnaryExpression.Right);
					SafeILGenerator.Push(1);
					SafeILGenerator.BinaryOperation(SafeBinaryOperator.AdditionSigned);
					SafeILGenerator.StoreIndirect(typeof(int));

					if (UnaryExpression.OperatorPosition == CParser.OperatorPosition.Left)
					{
						Traverse(UnaryExpression.Right);
					}
					else
					{
						Traverse(UnaryExpression.Right);
						SafeILGenerator.Push(1);
						SafeILGenerator.BinaryOperation(SafeBinaryOperator.SubstractionSigned);
					}
					break;
				default:
					throw (new NotImplementedException());
			}
		}
	}
}
