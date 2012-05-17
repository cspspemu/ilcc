//#define SHOW_INSTRUCTIONS

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
using System.Runtime.InteropServices;
using System.IO;

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
		public string Name { get; private set; }
		public MethodInfo MethodInfo { get; private set; }
		public SafeMethodTypeInfo SafeMethodTypeInfo { get; private set; }

		public FunctionReference(string Name, MethodInfo MethodInfo, SafeMethodTypeInfo SafeMethodTypeInfo = null)
		{
			this.Name = Name;
			this.MethodInfo = MethodInfo;
			this.SafeMethodTypeInfo = SafeMethodTypeInfo;
		}
	}

	public class VariableReference
	{
		public CSymbol CSymbol;
		private FieldBuilder Field;
		private LocalBuilder Local;
		private SafeArgument Argument;

		public VariableReference(CSymbol CSymbol, FieldBuilder Field)
		{
			this.CSymbol = CSymbol;
			this.Field = Field;
		}

		public VariableReference(CSymbol CSymbol, LocalBuilder Local)
		{
			this.CSymbol = CSymbol;
			this.Local = Local;
		}

		public VariableReference(CSymbol CSymbol, SafeArgument Argument)
		{
			this.CSymbol = CSymbol;
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
			else
			{
				SafeILGenerator.LoadArgument(Argument);
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
				throw(new NotImplementedException());
				//SafeILGenerator.LoadArgumentAddress(Argument);
			}
		}
	}

	[CConverter(Id = "cil", Description = "Outputs .NET IL code (not fully implemented yet)")]
	public class CILConverter : TraversableCConverter, CParser.IIdentifierTypeResolver
	{
		public AssemblyBuilder AssemblyBuilder { get; private set; }
		ModuleBuilder ModuleBuilder;
		//TypeBuilder RootTypeBuilder;
		//string OutName = "_out.dll";
		string OutFolder = ".";
		string OutName = "_out.exe";
		TypeBuilder CurrentClass;
		MethodBuilder CurrentMethod;
		public TypeBuilder RootTypeBuilder { get; private set; }
		SafeILGenerator SafeILGenerator;
		SafeILGenerator StaticInitializerSafeILGenerator;
		MethodInfo EntryPoint = null;
		bool GeneratingLeftValue = false;
		AScope<VariableReference> VariableScope = new AScope<VariableReference>();
		AScope<FunctionReference> FunctionScope = new AScope<FunctionReference>();
		bool SaveAssembly;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Type"></param>
		/// <returns></returns>
		static public IEnumerable<FunctionReference> GetFunctionReferencesFromType(Type Type)
		{
			foreach (var Method in Type.GetMethods())
			{
				if (Method.GetCustomAttributes(typeof(CFunctionExportAttribute), true).Length > 0)
				{
					yield return new FunctionReference(Method.Name, Method);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public CILConverter()
			: this(SaveAssembly: true)
		{
		}

		public CILConverter(bool SaveAssembly)
			: base()
		{
			this.SaveAssembly = SaveAssembly;
			RegisterCLibFunctions();
		}

		/// <summary>
		/// 
		/// </summary>
		private void RegisterCLibFunctions()
		{
			foreach (var FunctionReference in GetFunctionReferencesFromType(typeof(CLib)))
			{
				FunctionScope.Push(FunctionReference.Name, FunctionReference);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Program"></param>
		[CNodeTraverser]
		public void Program(CParser.TranslationUnit Program)
		{
			try { File.Delete(OutFolder + "\\" + OutName); } catch { }
			this.AssemblyBuilder = SafeAssemblyUtils.CreateAssemblyBuilder("TestOut", OutFolder);
			this.ModuleBuilder = this.AssemblyBuilder.CreateModuleBuilder(OutName);
			this.RootTypeBuilder = this.ModuleBuilder.DefineType("Program", TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
			PendingTypesToCreate.Add(this.RootTypeBuilder);
			var InitializerBuilder = this.RootTypeBuilder.DefineTypeInitializer();
			var CurrentStaticInitializerSafeILGenerator = new SafeILGenerator(InitializerBuilder.GetILGenerator(), CheckTypes: false, DoDebug: false, DoLog: false);

			Scopable.RefScope(ref this.StaticInitializerSafeILGenerator, CurrentStaticInitializerSafeILGenerator, () =>
			{
				Scopable.RefScope(ref this.CurrentClass, this.RootTypeBuilder, () =>
				{
					AScope<VariableReference>.NewScope(ref this.VariableScope, () =>
					{
						Traverse(Program.Declarations);
						this.StaticInitializerSafeILGenerator.Return();
						//RootTypeBuilder.CreateType();

						foreach (var TypeToCreate in PendingTypesToCreate) TypeToCreate.CreateType();

						if (EntryPoint != null) this.AssemblyBuilder.SetEntryPoint(EntryPoint);
						if (SaveAssembly)
						{
							this.AssemblyBuilder.Save(OutName);
						}
					});
				});
			});
		}

		List<TypeBuilder> PendingTypesToCreate = new List<TypeBuilder>();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="TypeDeclaration"></param>
		[CNodeTraverser]
		public void TypeDeclaration(CParser.TypeDeclaration TypeDeclaration)
		{
			var CSymbol = TypeDeclaration.Symbol;
			var CSimpleType = CSymbol.Type as CSimpleType;
			var CStructType = (CSimpleType != null) ? (CSimpleType.ComplexType as CStructType) : null;

			if (CStructType != null)
			{
				//var StructType = RootTypeBuilder.DefineNestedType(CSymbol.Name, TypeAttributes.NestedPublic | TypeAttributes.AutoLayout, RootTypeBuilder, (PackingSize)4);
				var StructType = ModuleBuilder.DefineType(CSymbol.Name, TypeAttributes.Public | TypeAttributes.AutoLayout, null, (PackingSize)4);
				PendingTypesToCreate.Add(StructType);

				//StructType.StructLayoutAttribute = new StructLayoutAttribute(LayoutKind.Sequential);
				{
					foreach (var Item in CStructType.Items)
					{
						StructType.DefineField(Item.Name, ConvertCTypeToType(Item.Type), FieldAttributes.Public);
					}
					//Console.Error.WriteLine("Not implemented TypeDeclaration");
				}
			}
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

			if (VariableName != null && VariableName.Length > 0 && VariableType != typeof(void))
			{
				// Global Scope
				if (this.SafeILGenerator == null)
				{
					var Field = CurrentClass.DefineField(
						VariableName,
						VariableType,
						FieldAttributes.Static | FieldAttributes.Public
					);
					var Variable = new VariableReference(VariableDeclaration.Symbol, Field);

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
					var Variable = new VariableReference(VariableDeclaration.Symbol, Local);

					this.VariableScope.Push(VariableName, Variable);

					if (VariableDeclaration.InitialValue != null)
					{
						Variable.LoadAddress(SafeILGenerator);
						Traverse(VariableDeclaration.InitialValue);
						SafeILGenerator.StoreIndirect<int>();
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="CastExpression"></param>
		[CNodeTraverser]
		public void CastExpression(CParser.CastExpression CastExpression)
		{
			Traverse(CastExpression.Right);
			SafeILGenerator.ConvertTo(ConvertCTypeToType(CastExpression.CastType));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="FunctionDeclaration"></param>
		[CNodeTraverser]
		public void FunctionDeclaration(CParser.FunctionDeclaration FunctionDeclaration)
		{
			var FunctionName = FunctionDeclaration.CFunctionType.Name;
			var ReturnType = ConvertCTypeToType(FunctionDeclaration.CFunctionType.Return);
			var ParameterTypes = FunctionDeclaration.CFunctionType.Parameters.Select(Item => ConvertCTypeToType(Item.Type)).ToArray();

			var CurrentMethod = CurrentClass.DefineMethod(
				FunctionName,
				MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard,
				ReturnType,
				ParameterTypes
			);

			FunctionScope.Push(FunctionName, new FunctionReference(FunctionName, CurrentMethod, new SafeMethodTypeInfo()
			{
				IsStatic = true,
				ReturnType = ReturnType,
				Parameters = ParameterTypes,
			}));

			if (FunctionName == "main")
			{
				EntryPoint = CurrentMethod;
			}

			var ILGenerator = CurrentMethod.GetILGenerator();
			var CurrentSafeILGenerator = new SafeILGenerator(ILGenerator, CheckTypes: false, DoDebug: false, DoLog: true);

			AScope<VariableReference>.NewScope(ref this.VariableScope, () =>
			{
				Scopable.RefScope(ref this.GotoContext, new LabelsContext(CurrentSafeILGenerator), () =>
				{
					Scopable.RefScope(ref this.CurrentMethod, CurrentMethod, () =>
					{
						Scopable.RefScope(ref this.SafeILGenerator, CurrentSafeILGenerator, () =>
						{
							// Set argument variables
							int ArgumentIndex = 0;
							foreach (var Parameter in FunctionDeclaration.CFunctionType.Parameters)
							{
								var Argument = SafeILGenerator.DeclareArgument(ConvertCTypeToType(Parameter.Type), ArgumentIndex);
								this.VariableScope.Push(Parameter.Name, new VariableReference(Parameter, Argument));
								ArgumentIndex++;
							}

							Traverse(FunctionDeclaration.FunctionBody);
							SafeILGenerator.Return();
						});
#if SHOW_INSTRUCTIONS
						Console.WriteLine("Code for '{0}':", FunctionName);
						foreach (var Instruction in CurrentSafeILGenerator.GetEmittedInstructions()) Console.WriteLine("  {0}", Instruction);
#endif
					});
				});
			});
		}

		public class LabelContext
		{
			public SafeLabel Label;

			public LabelContext(SafeLabel Label)
			{
				this.Label = Label;
			}
		}

		protected class LabelsContext
		{
			Dictionary<string, SafeLabel> Labels = new Dictionary<string, SafeLabel>();
			SafeILGenerator SafeILGenerator;

			public LabelsContext(SafeILGenerator SafeILGenerator)
			{
				this.SafeILGenerator = SafeILGenerator;
			}

			public SafeLabel GetLabel(string Name)
			{
				if (!Labels.ContainsKey(Name))
				{
					Labels.Add(Name, SafeILGenerator.DefineLabel(Name));
				}
				return Labels[Name];
			}
		}

		LabelsContext GotoContext;
		LabelContext BreakableContext;
		LabelContext ContinuableContext;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="SwitchStatement"></param>
		[CNodeTraverser]
		public void SwitchStatement(CParser.SwitchStatement SwitchStatement)
		{
			// TODO: improve speed with tables and proper switch instead of lot of "if" checks.
			var MapLabels = new Dictionary<int, SafeLabel>();
			var DefaultLabel = SafeILGenerator.DefineLabel("SwitchDefault");
			var EndLabel = SafeILGenerator.DefineLabel("SwitchEnd");

			//var SwitchExpressionLocal = SafeILGenerator.DeclareLocal<long>("SwitchReference");
			//SafeILGenerator.LoadLocal(SwitchExpressionLocal);

			//foreach (var SwitchCaseStatement in SwitchStatement.Statements.Statements.Where(Item => Item is CParser.SwitchCaseStatement).Cast<CParser.SwitchCaseStatement>())
			foreach (var Statement in SwitchStatement.Statements.Statements)
			{
				var SwitchCaseStatement = Statement as CParser.SwitchCaseStatement;
				var SwitchDefaultStatement = Statement as CParser.SwitchDefaultStatement;

				if (SwitchCaseStatement != null)
				{
					var Value = SwitchCaseStatement.Value.GetConstantValue<int>();
					var CaseLabel = SafeILGenerator.DefineLabel("SwitchCase");
					SwitchCaseStatement.Tag = CaseLabel;
					//Console.WriteLine("Value: {0}", Value);
					MapLabels.Add(Value, CaseLabel);
				}
				else if (SwitchDefaultStatement != null)
				{
					SwitchDefaultStatement.Tag = DefaultLabel;
				}
			}

			Traverse(SwitchStatement.ReferenceExpression);
			SafeILGenerator.Switch(MapLabels, DefaultLabel);

			Scopable.RefScope(ref BreakableContext, new LabelContext(EndLabel), () =>
			{
				Traverse(SwitchStatement.Statements);
			});

			EndLabel.Mark();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ContinueStatement"></param>
		[CNodeTraverser]
		public void ContinueStatement(CParser.ContinueStatement ContinueStatement)
		{
			SafeILGenerator.BranchAlways(ContinuableContext.Label);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="BreakStatement"></param>
		[CNodeTraverser]
		public void BreakStatement(CParser.BreakStatement BreakStatement)
		{
			SafeILGenerator.BranchAlways(BreakableContext.Label);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="SwitchCaseStatement"></param>
		[CNodeTraverser]
		public void SwitchCaseStatement(CParser.SwitchCaseStatement SwitchCaseStatement)
		{
			(SwitchCaseStatement.Tag as SafeLabel).Mark();
			//SwitchCaseStatement.Value.GetConstantValue<int>();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="SwitchDefaultStatement"></param>
		[CNodeTraverser]
		public void SwitchDefaultStatement(CParser.SwitchDefaultStatement SwitchDefaultStatement)
		{
			(SwitchDefaultStatement.Tag as SafeLabel).Mark();
			//SwitchCaseStatement.Value.GetConstantValue<int>();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="LabelStatement"></param>
		[CNodeTraverser]
		public void LabelStatement(CParser.LabelStatement LabelStatement)
		{
			var LabelName = LabelStatement.IdentifierExpression.Identifier;
			var Label = GotoContext.GetLabel(LabelName);
			Label.Mark();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="LabelStatement"></param>
		[CNodeTraverser]
		public void GotoStatement(CParser.GotoStatement GotoStatement)
		{
			var LabelName = GotoStatement.LabelName;
			var Label = GotoContext.GetLabel(LabelName);
			SafeILGenerator.BranchAlways(Label);
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
		/// <param name="IfElseStatement"></param>
		[CNodeTraverser]
		public void IfElseStatement(CParser.IfElseStatement IfElseStatement)
		{
			Traverse(IfElseStatement.Condition);
			SafeILGenerator.MacroIfElse(() =>
			{
				Traverse(IfElseStatement.TrueStatement);
			}, () =>
			{
				Traverse(IfElseStatement.FalseStatement);
			});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ReturnStatement"></param>
		[CNodeTraverser]
		public void ReturnStatement(CParser.ReturnStatement ReturnStatement)
		{
			Traverse(ReturnStatement.Expression);
			SafeILGenerator.Return();
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ExpressionCommaList"></param>
		[CNodeTraverser]
		public void ExpressionCommaList(CParser.ExpressionCommaList ExpressionCommaList)
		{
			var Expressions = ExpressionCommaList.Expressions;

#if false
			Traverse(Expressions[Expressions.Length - 1]);
#else
			Traverse(Expressions[0]);

			foreach (var Expression in Expressions.Skip(1))
			{
				SafeILGenerator.PopLeft();
				Traverse(Expression);
			}
#endif
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="BaseWhileStatement"></param>
		/// <param name="ExecuteAtLeastOnce"></param>
		private void WhileBaseStatement(CParser.BaseWhileStatement BaseWhileStatement, bool ExecuteAtLeastOnce)
		{
			var IterationLabel = SafeILGenerator.DefineLabel("IterationLabel");
			var BreakLabel = SafeILGenerator.DefineLabel("BreakLabel");
			var ContinueLabel = SafeILGenerator.DefineLabel("ContinueLabel");
			var LoopCheckConditionLabel = SafeILGenerator.DefineLabel("LoopCheckConditionLabel");

			if (!ExecuteAtLeastOnce)
			{
				SafeILGenerator.BranchAlways(LoopCheckConditionLabel);
			}

			IterationLabel.Mark();

			Scopable.RefScope(ref BreakableContext, new LabelContext(BreakLabel), () =>
			{
				Scopable.RefScope(ref ContinuableContext, new LabelContext(ContinueLabel), () =>
				{
					Traverse(BaseWhileStatement.LoopStatements);
				});
			});

			ContinueLabel.Mark();
			LoopCheckConditionLabel.Mark();
			Traverse(BaseWhileStatement.Condition);
			SafeILGenerator.BranchIfTrue(IterationLabel);

			BreakLabel.Mark();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="WhileStatement"></param>
		[CNodeTraverser]
		public void WhileStatement(CParser.WhileStatement WhileStatement)
		{
			WhileBaseStatement(WhileStatement, ExecuteAtLeastOnce: false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="WhileStatement"></param>
		[CNodeTraverser]
		public void DoWhileStatement(CParser.DoWhileStatement DoWhileStatement)
		{
			WhileBaseStatement(DoWhileStatement, ExecuteAtLeastOnce: true);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ForStatement"></param>
		[CNodeTraverser]
		public void ForStatement(CParser.ForStatement ForStatement)
		{
			if (ForStatement.Init != null)
			{
				Traverse(ForStatement.Init);
				SafeILGenerator.PopLeft();
			}

			var IterationLabel = SafeILGenerator.DefineLabel("IterationLabel");
			var BreakLabel = SafeILGenerator.DefineLabel("BreakLabel");
			var ContinueLabel = SafeILGenerator.DefineLabel("ContinueLabel");
			var LoopCheckConditionLabel = SafeILGenerator.DefineLabel("LoopCheckConditionLabel");
			{
				SafeILGenerator.BranchAlways(LoopCheckConditionLabel, Short: true);

				IterationLabel.Mark();
				Scopable.RefScope(ref BreakableContext, new LabelContext(BreakLabel), () =>
				{
					Scopable.RefScope(ref ContinuableContext, new LabelContext(ContinueLabel), () =>
					{
						Traverse(ForStatement.LoopStatements);
					});
				});

				ContinueLabel.Mark();

				if (ForStatement.PostOperation != null)
				{
					Traverse(ForStatement.PostOperation);
					SafeILGenerator.PopLeft();
				}

				LoopCheckConditionLabel.Mark();
				if (ForStatement.Condition != null)
				{
					Traverse(ForStatement.Condition);
				}
				else
				{
					SafeILGenerator.Push(1);
				}
				SafeILGenerator.BranchIfTrue(IterationLabel, Short: true);
				BreakLabel.Mark();
			}
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
					throw (new Exception(String.Format("Unknown function '{0}'", IdentifierExpression.Identifier)));
				}
				Traverse(FunctionCallExpression.Parameters.Expressions);
				SafeILGenerator.Call(FunctionReference.MethodInfo, FunctionReference.SafeMethodTypeInfo);
				//SafeILGenerator.__ILGenerator.Emit(OpCodes.Call
				//throw (new NotImplementedException("Function: " + IdentifierExpression.Value));
			}
			else
			{
				throw(new NotImplementedException());
			}
		}

		class SizeProvider : ISizeProvider
		{
			int ISizeProvider.PointerSize
			{
				get { return 4; }
			}
		}

		SizeProvider ISizeProvider = new SizeProvider();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ArrayAccessExpression"></param>
		[CNodeTraverser]
		public void ArrayAccessExpression(CParser.ArrayAccessExpression ArrayAccessExpression)
		{
			var LeftExpression = ArrayAccessExpression.Left;
			var LeftType = (ArrayAccessExpression.Left.GetCType(this) as CBasePointerType);
			var ElementType = LeftType.ElementCType;
			var IndexExpression = ArrayAccessExpression.Index;

			if (GeneratingLeftValue)
			{
				DoGenerateLeftValue(false, () =>
				{
					Traverse(LeftExpression);
					Traverse(IndexExpression);
				});

				SafeILGenerator.Push(LeftType.GetSize(ISizeProvider));
				SafeILGenerator.BinaryOperation(SafeBinaryOperator.MultiplySigned);

				SafeILGenerator.BinaryOperation(SafeBinaryOperator.AdditionSigned);

				//SafeILGenerator.LoadElementFromArray<int>();
			}
			else
			{
#if true
				DoGenerateLeftValue(false, () =>
				{
					Traverse(LeftExpression);
					Traverse(IndexExpression);
				});

				SafeILGenerator.Push(LeftType.GetSize(ISizeProvider));
				SafeILGenerator.BinaryOperation(SafeBinaryOperator.MultiplySigned);

				SafeILGenerator.BinaryOperation(SafeBinaryOperator.AdditionSigned);
				SafeILGenerator.LoadIndirect(ConvertCTypeToType(ElementType));
#else
				Traverse(LeftExpression);
				Traverse(IndexExpression);

				SafeILGenerator.LoadElementFromArray<int>();
#endif
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Set"></param>
		/// <param name="Action"></param>
		private void DoGenerateLeftValue(bool Set, Action Action)
		{
			Scopable.RefScope(ref this.GeneratingLeftValue, Set, Action);
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
		/// <param name="TrinaryExpression"></param>
		[CNodeTraverser]
		public void TrinaryExpression(CParser.TrinaryExpression TrinaryExpression)
		{
			Traverse(TrinaryExpression.Condition);
			SafeILGenerator.MacroIfElse(() =>
			{
				Traverse(TrinaryExpression.TrueCond);
			}, () =>
			{
				Traverse(TrinaryExpression.FalseCond);
			});
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
			if (GeneratingLeftValue)
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
				case "-":
				case "*":
				case "/":
				case "%":
				case "<":
				case ">":
				case "<=":
				case ">=":
				case "==":
				case "!=":
					Traverse(BinaryExpression.Left);
					Traverse(BinaryExpression.Right);
					switch (BinaryExpression.Operator)
					{
						case "+": SafeILGenerator.BinaryOperation(SafeBinaryOperator.AdditionSigned); break;
						case "-": SafeILGenerator.BinaryOperation(SafeBinaryOperator.SubstractionSigned); break;
						case "*": SafeILGenerator.BinaryOperation(SafeBinaryOperator.MultiplySigned); break;
						case "/": SafeILGenerator.BinaryOperation(SafeBinaryOperator.DivideSigned); break;
						case "%": SafeILGenerator.BinaryOperation(SafeBinaryOperator.RemainingSigned); break;
						case "<": SafeILGenerator.CompareBinary(SafeBinaryComparison.LessThanSigned); break;
						case ">": SafeILGenerator.CompareBinary(SafeBinaryComparison.GreaterThanSigned); break;
						case "<=": SafeILGenerator.CompareBinary(SafeBinaryComparison.LessOrEqualSigned); break;
						case ">=": SafeILGenerator.CompareBinary(SafeBinaryComparison.GreaterOrEqualSigned); break;
						case "==": SafeILGenerator.CompareBinary(SafeBinaryComparison.Equals); break;
						case "!=": SafeILGenerator.CompareBinary(SafeBinaryComparison.NotEquals); break;
					}
					break;
				case "=":
					DoGenerateLeftValue(true, () =>
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="UnaryExpression"></param>
		[CNodeTraverser]
		public void UnaryExpression(CParser.UnaryExpression UnaryExpression)
		{
			switch (UnaryExpression.Operator)
			{
				case "-":
					if (UnaryExpression.OperatorPosition != CParser.OperatorPosition.Left) throw(new InvalidOperationException());
					Traverse(UnaryExpression.Right);
					SafeILGenerator.UnaryOperation(SafeUnaryOperator.Negate);
					break;
				case "++":
					Scopable.RefScope(ref GeneratingLeftValue, true, () =>
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Identifier"></param>
		/// <returns></returns>
		CType CParser.IIdentifierTypeResolver.ResolveIdentifierType(string Identifier)
		{
			//FunctionScope.Find(Identifier);
			var VariableReference = VariableScope.Find(Identifier);
			if (VariableReference != null)
			{
				return VariableReference.CSymbol.Type;
			}
			var FunctionReference = FunctionScope.Find(Identifier);
			if (FunctionReference != null)
			{
				throw new NotImplementedException();
				//return FunctionReference.CSymbol.Type;
			}
			throw new NotImplementedException();
		}
	}
}
