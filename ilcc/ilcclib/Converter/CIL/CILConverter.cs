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
using System.Diagnostics.SymbolStore;

namespace ilcclib.Converter.CIL
{
	[CConverter(Id = "cil", Description = "Outputs .NET IL code (not fully implemented yet)")]
	unsafe public partial class CILConverter : TraversableCConverter, CParser.IIdentifierTypeResolver, ISizeProvider
	{
		public AssemblyBuilder AssemblyBuilder { get; private set; }
		ModuleBuilder ModuleBuilder;
		//TypeBuilder RootTypeBuilder;
		string OutFolder = ".";
		TypeBuilder CurrentClass;
		MethodBuilder CurrentMethod;
		public TypeBuilder RootTypeBuilder { get; private set; }
		SafeILGenerator SafeILGenerator;
		SafeILGenerator StaticInitializerSafeILGenerator;
		//bool HasEntryPoint = false;
		MethodInfo EntryPoint = null;
		bool GenerateAddress = false;
		AScope<VariableReference> VariableScope = new AScope<VariableReference>();
		AScope<FunctionReference> FunctionScope = new AScope<FunctionReference>();
		bool SaveAssembly;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Type"></param>
		/// <returns></returns>
		public IEnumerable<FunctionReference> GetFunctionReferencesFromType(Type Type)
		{
			var FunctionReferences = new List<FunctionReference>();

			// Exports Methods.
			foreach (var Method in Type.GetMethods(BindingFlags.Public | BindingFlags.Static))
			{
				if (Method.GetCustomAttributes(typeof(CExportAttribute), true).Length > 0)
				{
					FunctionReferences.Add(new FunctionReference(this, Method.Name, Method));
				}
			}

			return FunctionReferences;
		}

		public IEnumerable<VariableReference> GetGlobalVariableReferencesFromType(Type Type)
		{
			var VariableReferences = new List<VariableReference>();

			// Exports Methods.
			foreach (var Field in Type.GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				if (Field.GetCustomAttributes(typeof(CExportAttribute), true).Length > 0)
				{
					VariableReferences.Add(new VariableReference(Field.Name, ConvertTypeToCType(Field.FieldType), Field));
				}
			}

			return VariableReferences;
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
		}

		override public void Initialize(string OutputName)
		{
			base.Initialize(OutputName);
			RegisterLibrary<CLib>();
		}

		/// <summary>
		/// 
		/// </summary>
		private void RegisterLibrary<TType>()
		{
			foreach (var FunctionReference in GetFunctionReferencesFromType(typeof(TType)))
			{
#if false
				Console.WriteLine("Imported function<{0}>: {1}", typeof(TType), FunctionReference.Name);
#endif
				FunctionScope.Push(FunctionReference.Name, FunctionReference);
			}

			foreach (var VariableReference in GetGlobalVariableReferencesFromType(typeof(TType)))
			{
#if false
				Console.WriteLine("Imported variable<{0}>: {1}", typeof(TType), VariableReference.Name);
#endif
				VariableScope.Push(VariableReference.Name, VariableReference);
			}
		}

		static public bool ThrowException = false;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="TranslationUnit"></param>
		[CNodeTraverser]
		public void TranslationUnit(CParser.TranslationUnit TranslationUnit)
		{
#if false
			try
			{
#endif
				PutDebugLine(TranslationUnit);

				try { File.Delete(OutFolder + "\\" + OutputName); }
				catch { }
				var ClassName = Path.GetFileNameWithoutExtension(OutputName);
				this.AssemblyBuilder = SafeAssemblyUtils.CreateAssemblyBuilder(ClassName, OutFolder);
				this.ModuleBuilder = this.AssemblyBuilder.CreateModuleBuilder(OutputName);
				this.RootTypeBuilder = this.ModuleBuilder.DefineType(ClassName, TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
				PendingTypesToCreate.Add(this.RootTypeBuilder);
				var InitializerBuilder = this.RootTypeBuilder.DefineTypeInitializer();
				var CurrentStaticInitializerSafeILGenerator = new SafeILGenerator(InitializerBuilder.GetILGenerator(), CheckTypes: false, DoDebug: false, DoLog: false);

				Scopable.RefScope(ref this.StaticInitializerSafeILGenerator, CurrentStaticInitializerSafeILGenerator, () =>
				{
					Scopable.RefScope(ref this.CurrentClass, this.RootTypeBuilder, () =>
					{
						AScope<VariableReference>.NewScope(ref this.VariableScope, () =>
						{
							Traverse(TranslationUnit.Declarations);
							this.StaticInitializerSafeILGenerator.Return(typeof(void));
							//RootTypeBuilder.CreateType();

							foreach (var FunctionReference in FunctionScope.GetAll())
							{
								if (!FunctionReference.BodyFinalized && FunctionReference.HasStartedBody)
								{
									Console.WriteLine("Function {0} without body", FunctionReference.Name);
									var FakeSafeILGenerator = new SafeILGenerator((FunctionReference.MethodInfo as MethodBuilder).GetILGenerator(), CheckTypes: true, DoDebug: true, DoLog: false);
									FakeSafeILGenerator.Push(String.Format("Not implemented '{0}'", FunctionReference.Name));
									FakeSafeILGenerator.NewObject(typeof(NotImplementedException).GetConstructor(new Type[] { typeof(string) }));
									FakeSafeILGenerator.Throw();
								}
							}

							foreach (var TypeToCreate in PendingTypesToCreate) TypeToCreate.CreateType();

							if (EntryPoint != null) this.AssemblyBuilder.SetEntryPoint(EntryPoint);
							if (SaveAssembly)
							{
								// Copy the runtime.
								var RuntimePath = typeof(CModuleAttribute).Assembly.Location;
								try
								{
									File.Copy(RuntimePath, OutFolder + "\\" + Path.GetFileName(RuntimePath), overwrite: true);
								}
								catch
								{
								}

								/*
								if (EntryPoint != null)
								{
									OutputName = Path.GetFileNameWithoutExtension(OutputName) + ".exe";
								}
								else
								{
									OutputName = Path.GetFileNameWithoutExtension(OutputName) + ".dll";
								}
								*/

								Console.WriteLine("Writting to {0}", OutputName);
								//this.AssemblyBuilder.Save(OutputName, PortableExecutableKinds.Required32Bit, ImageFileMachine.I386);
								this.AssemblyBuilder.Save(OutputName, PortableExecutableKinds.ILOnly, ImageFileMachine.I386);
							}
						});
					});
				});
#if false
			}
			catch (Exception Exception)
			{
				if (ThrowException) throw (Exception);

				while (Exception.InnerException != null) Exception = Exception.InnerException;
				Console.Error.WriteLine("");
				Console.Error.WriteLine("LastPosition: {0}", LastPositionInfo);
				Console.Error.WriteLine("{0} : '{1}'", Exception.TargetSite, Exception.Message);
				if (Exception.StackTrace != null)
				{
					Console.Error.WriteLine("{0}", String.Join("\n", Exception.StackTrace.Split('\n').Take(4)));
					Console.Error.WriteLine("   ...");
				}
			}
#endif
		}

		List<TypeBuilder> PendingTypesToCreate = new List<TypeBuilder>();

		private int anonymous_type_index = 0;

		/*
		virtual protected Type CreateTypeFromCType(CStructType CStructType)
		{
			throw (new NotImplementedException("Not implemented creating new types. This method must be extended."));
		}
		*/
		protected override Type CreateTypeFromCType(CType CType)
		{
			return DefineType(null, CType);
		}

		private Type DefineType(string Name, CType CType)
		{
			if (Name == null) Name = String.Format("__anonymous_type_{0}", anonymous_type_index++);

			var CUnionStructType = CType as CUnionStructType;

			if (CUnionStructType != null)
			{
				//var StructType = RootTypeBuilder.DefineNestedType(CSymbol.Name, TypeAttributes.NestedPublic | TypeAttributes.SequentialLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, typeof(ValueType), (PackingSize)4);
				var StructTypeAttributes = TypeAttributes.Public | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;
				StructTypeAttributes |= CUnionStructType.IsUnion ? TypeAttributes.ExplicitLayout : TypeAttributes.SequentialLayout;
				var StructType = ModuleBuilder.DefineType(Name, StructTypeAttributes, typeof(ValueType), (PackingSize)4);

				//StructType.StructLayoutAttribute = new StructLayoutAttribute(LayoutKind.Sequential);
				{
					foreach (var Item in CUnionStructType.Items)
					{
						var Field = StructType.DefineField(Item.Name, ConvertCTypeToType(Item.CType), FieldAttributes.Public);
						if (CUnionStructType.IsUnion)
						{
							Field.SetCustomAttribute(new CustomAttributeBuilder(typeof(FieldOffsetAttribute).GetConstructor(new Type[] { typeof(int) }), new object[] { 0 }));
						}
					}
					//Console.Error.WriteLine("Not implemented TypeDeclaration");
				}

				//PendingTypesToCreate.Add(StructType);
				StructType.CreateType();

				return StructType;
			}
			else
			{
				return ConvertCTypeToType(CType);
				//return null;
				//throw (new InvalidOperationException(String.Format("CStructType == null : {0}", CType)));
			}
		}

		private Type DefineType(CSymbol CSymbol)
		{
			return DefineType(CSymbol.Name, CSymbol.CType);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="EmptyDeclaration"></param>
		[CNodeTraverser]
		public void EmptyDeclaration(CParser.EmptyDeclaration EmptyDeclaration)
		{
			PutDebugLine(EmptyDeclaration);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="TypeDeclaration"></param>
		[CNodeTraverser]
		public void TypeDeclaration(CParser.TypeDeclaration TypeDeclaration)
		{
			PutDebugLine(TypeDeclaration);

			CustomTypeContext.SetTypeByCType(TypeDeclaration.Symbol.CType, DefineType(TypeDeclaration.Symbol));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="VariableDeclaration"></param>
		[CNodeTraverser]
		public void VariableDeclaration(CParser.VariableDeclaration VariableDeclaration)
		{
			PutDebugLine(VariableDeclaration);

			var VariableName = VariableDeclaration.Symbol.Name;
			var VariableCType = VariableDeclaration.Symbol.CType;

			//Console.WriteLine(VariableCType);

			// ??
			if (VariableCType == null)
			{
				Console.Error.WriteLine("Warning: Global variable '{0}' doesn't have type!!", VariableName);
				return;
			}

			var VariableType = ConvertCTypeToType(VariableCType);

			var IsExternVariable = (VariableCType.GetCSimpleType().Storage == CTypeStorage.Extern);
			var IsAlreadyDefined = (VariableScope.Find(VariableName) != null);


			if (IsAlreadyDefined)
			{
				if (!IsExternVariable)
				{
					Console.Error.WriteLine("Warning: Global variable '{0}' already defined but not defined as external", VariableName);
				}
				return;
			}

			if (VariableName == null || VariableName.Length == 0)
			{
				Console.Error.WriteLine("Variable doesn't have name!");
				return;
			}
			if (VariableCType is CFunctionType)
			{
				Console.Error.WriteLine("Variable is not a function!");
				return;
			}
			if (VariableType == typeof(void))
			{
				Console.Error.WriteLine("Variable has void type!");
				return;
			}

			VariableReference Variable;
			bool GlobalScope;

			// Global Scope
			if (this.SafeILGenerator == null)
			{
				GlobalScope = true;

				var Field = CurrentClass.DefineField(VariableName, VariableType, FieldAttributes.Static | FieldAttributes.Public);
				Variable = new VariableReference(VariableDeclaration.Symbol.Name, VariableDeclaration.Symbol.CType, Field);
			}
			// Local Scope
			else
			{
				GlobalScope = false;

				var Local = this.SafeILGenerator.DeclareLocal(VariableType, VariableName);
				Variable = new VariableReference(VariableDeclaration.Symbol.Name, VariableDeclaration.Symbol.CType, Local);
			}

			this.VariableScope.Push(VariableName, Variable);

			Action Initialize = () =>
			{
				Variable.LoadAddress(SafeILGenerator);
				SafeILGenerator.InitObject(VariableType);

				Traverse(VariableDeclaration.InitialValue);
				SafeILGenerator.PopLeft();
			};

			if (GlobalScope)
			{
				Scopable.RefScope(ref SafeILGenerator, StaticInitializerSafeILGenerator, () =>
				{
					Initialize();
				});
			}
			else
			{
				Initialize();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="FunctionDeclaration"></param>
		[CNodeTraverser]
		public void FunctionDeclaration(CParser.FunctionDeclaration FunctionDeclaration)
		{
			PutDebugLine(FunctionDeclaration);

			var FunctionName = FunctionDeclaration.CFunctionType.Name;
			var ReturnType = ConvertCTypeToType(FunctionDeclaration.CFunctionType.Return);
			var ParameterTypes = FunctionDeclaration.CFunctionType.Parameters.Select(Item => ConvertCTypeToType(Item.CType)).ToArray();
			var ParameterCSymbols = FunctionDeclaration.CFunctionType.Parameters;

			if (ParameterTypes.Length == 1 && ParameterTypes[0] == typeof(void)) ParameterTypes = new Type[0];
			var FunctionReference = FunctionScope.Find(FunctionName);

			if (FunctionReference == null)
			{
				var CurrentMethodLazy = new Lazy<MethodInfo>(() =>
				{
					var MethodBuilder = CurrentClass.DefineMethod(
						FunctionName,
						MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard,
						ReturnType,
						ParameterTypes
					);

					for (int n = 0; n < ParameterCSymbols.Length; n++)
					{
						MethodBuilder.DefineParameter(n, ParameterAttributes.None, ParameterCSymbols[n].Name);
					}

					return MethodBuilder;
				});

				FunctionReference = new FunctionReference(this, FunctionName, CurrentMethodLazy, new SafeMethodTypeInfo()
				{
					IsStatic = true,
					ReturnType = ReturnType,
					Parameters = ParameterTypes,
				})
				{
					BodyFinalized = false,
				};

				FunctionScope.Push(FunctionName, FunctionReference);
			}

			// Just declaration
			if (FunctionDeclaration.FunctionBody == null)
			{

			}
			// Has function body.
			else
			{
				var CurrentMethod = (FunctionReference.MethodInfo as MethodBuilder);

				if (FunctionName == "main")
				{
					//HasEntryPoint = true;

					var StartupMethod = CurrentClass.DefineMethod(
						"__startup",
						MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard,
						typeof(int),
						new Type[] { typeof(string[]) }
					);

					var StartupSafeILGenerator = new SafeILGenerator(StartupMethod.GetILGenerator(), CheckTypes: true, DoDebug: false, DoLog: false);
					var ArgsArgument = StartupSafeILGenerator.DeclareArgument(typeof(string[]), 0);

					StartupSafeILGenerator.Push(CurrentClass);
					StartupSafeILGenerator.Call((Func<RuntimeTypeHandle, Type>)Type.GetTypeFromHandle);
					StartupSafeILGenerator.LoadArgument(ArgsArgument);
					StartupSafeILGenerator.Call((Func<Type, string[], int>)CLibUtils.RunTypeMain);
					//StartupSafeILGenerator.Call((Func<Type, string[], int>)CLibUtils.RunTypeMain);
					StartupSafeILGenerator.Return(typeof(int));

					EntryPoint = StartupMethod;
					//EntryPoint = CurrentMethod;
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
								ushort ArgumentIndex = 0;
								foreach (var Parameter in FunctionDeclaration.CFunctionType.Parameters)
								{
									var Argument = SafeILGenerator.DeclareArgument(ConvertCTypeToType(Parameter.CType), ArgumentIndex);
									this.VariableScope.Push(Parameter.Name, new VariableReference(Parameter.Name, Parameter.CType, Argument));
									ArgumentIndex++;
								}

								Traverse(FunctionDeclaration.FunctionBody);


								if (FunctionDeclaration.FunctionBody.Statements.Length == 0 || !(FunctionDeclaration.FunctionBody.Statements.Last() is CParser.ReturnStatement))
								//if (true)
								{
									if (CurrentMethod.ReturnType != typeof(void))
									{
										SafeILGenerator.Push((int)0);
									}
									SafeILGenerator.Return(CurrentMethod.ReturnType);
								}
							});
#if SHOW_INSTRUCTIONS
						Console.WriteLine("Code for '{0}':", FunctionName);
						foreach (var Instruction in CurrentSafeILGenerator.GetEmittedInstructions()) Console.WriteLine("  {0}", Instruction);
#endif
						});
					});
				});

				FunctionReference.BodyFinalized = true;
			}
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
			PutDebugLine(SwitchStatement);

			SafeILGenerator.SaveRestoreTypeStack(() =>
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

				if (!DefaultLabel.Marked) DefaultLabel.Mark();

				EndLabel.Mark();
			});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ContinueStatement"></param>
		[CNodeTraverser]
		public void ContinueStatement(CParser.ContinueStatement ContinueStatement)
		{
			PutDebugLine(ContinueStatement);

			SafeILGenerator.BranchAlways(ContinuableContext.Label);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="BreakStatement"></param>
		[CNodeTraverser]
		public void BreakStatement(CParser.BreakStatement BreakStatement)
		{
			PutDebugLine(BreakStatement);

			SafeILGenerator.BranchAlways(BreakableContext.Label);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="SwitchCaseStatement"></param>
		[CNodeTraverser]
		public void SwitchCaseStatement(CParser.SwitchCaseStatement SwitchCaseStatement)
		{
			PutDebugLine(SwitchCaseStatement);

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
			PutDebugLine(SwitchDefaultStatement);

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
			PutDebugLine(LabelStatement);

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
			PutDebugLine(GotoStatement);

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
			PutDebugLine(CompoundStatement);

			Traverse(CompoundStatement.Statements);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="IfElseStatement"></param>
		[CNodeTraverser]
		public void IfElseStatement(CParser.IfElseStatement IfElseStatement)
		{
			PutDebugLine(IfElseStatement);

			SafeILGenerator.SaveRestoreTypeStack(() =>
			{
				Traverse(IfElseStatement.Condition);

				if (IfElseStatement.FalseStatement != null)
				{
					SafeILGenerator.MacroIfElse(() =>
					{
						Traverse(IfElseStatement.TrueStatement);
					}, () =>
					{
						Traverse(IfElseStatement.FalseStatement);
					});
				}
				else
				{
					SafeILGenerator.MacroIf(() =>
					{
						Traverse(IfElseStatement.TrueStatement);
					});
				}
			});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ReturnStatement"></param>
		[CNodeTraverser]
		public void ReturnStatement(CParser.ReturnStatement ReturnStatement)
		{
			PutDebugLine(ReturnStatement);

			Traverse(ReturnStatement.Expression);
			SafeILGenerator.Return(CurrentMethod.ReturnType);
			//SafeILGenerator.PopLeft();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="CompoundStatement"></param>
		[CNodeTraverser]
		public void DeclarationList(CParser.DeclarationList DeclarationList)
		{
			PutDebugLine(DeclarationList);

			Traverse(DeclarationList.Declarations);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="BaseWhileStatement"></param>
		/// <param name="ExecuteAtLeastOnce"></param>
		private void BaseWhileStatement(CParser.BaseWhileStatement BaseWhileStatement, bool ExecuteAtLeastOnce)
		{
			PutDebugLine(BaseWhileStatement);

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
			PutDebugLine(WhileStatement);

			BaseWhileStatement(WhileStatement, ExecuteAtLeastOnce: false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="WhileStatement"></param>
		[CNodeTraverser]
		public void DoWhileStatement(CParser.DoWhileStatement DoWhileStatement)
		{
			PutDebugLine(DoWhileStatement);

			BaseWhileStatement(DoWhileStatement, ExecuteAtLeastOnce: true);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ForStatement"></param>
		[CNodeTraverser]
		public void ForStatement(CParser.ForStatement ForStatement)
		{
			PutDebugLine(ForStatement);

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
				SafeILGenerator.BranchAlways(LoopCheckConditionLabel);

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
				SafeILGenerator.BranchIfTrue(IterationLabel);
				BreakLabel.Mark();
			}
		}

		Dictionary<string, ISymbolDocumentWriter> DebugDocuments = new Dictionary<string, ISymbolDocumentWriter>();
		private CParser.PositionInfo LastPositionInfo;

		private void PutDebugLine(CParser.Statement Statement)
		{
			var PositionInfo = Statement.PositionInfo;
			LastPositionInfo = PositionInfo;

			if (ModuleBuilder != null)
			{
				if (!DebugDocuments.ContainsKey(PositionInfo.File))
				{
					string FullPath = "";
					try { FullPath = Path.GetFullPath(PositionInfo.File); }
					catch { }

					if (FullPath.Length > 0 && File.Exists(FullPath))
					{
						DebugDocuments[PositionInfo.File] = ModuleBuilder.DefineDocument(
							FullPath,
							SymDocumentType.Text,
							SymLanguageType.C,
							SymLanguageVendor.Microsoft
						);
					}
					else
					{
						DebugDocuments[PositionInfo.File] = null;
					}

					//DebugDocuments[PositionInfo.File].SetSource(File.ReadAllBytes(PositionInfo.File));
					//ModuleBuilder.debug
					//DebugEmittedVersion
				}

				if (SafeILGenerator != null)
				{
					if (DebugDocuments[PositionInfo.File] != null)
					{
						SafeILGenerator.__ILGenerator.MarkSequencePoint(
							DebugDocuments[PositionInfo.File],
							PositionInfo.LineStart,
							PositionInfo.ColumnStart + 1,
							PositionInfo.LineStart,
							PositionInfo.ColumnEnd + 1
						);
					}
				}
				//ilGenerator.MarkSequencePoint(doc, 6, 1, 6, 100);
				//DebugDocuments[PositionInfo.File].SetSource(
			}
		}

		bool RequireYieldResult = true;

		protected override void TraverseHook(Action Action, CParser.Node ParentNode, CParser.Node Node)
		{
			// TODO: Reenable and run tests.
#if true
			RequireYieldResult = !(ParentNode is CParser.ExpressionStatement);
#endif

			//Console.WriteLine("{0} -> {1} : {2}", ParentNode, Node, RequireYieldResult);
			Action();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ExpressionStatement"></param>
		[CNodeTraverser]
		public void ExpressionStatement(CParser.ExpressionStatement ExpressionStatement)
		{
			PutDebugLine(ExpressionStatement);
			Traverse(ExpressionStatement.Expression);
			SafeILGenerator.PopLeft();
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
				return VariableReference.CType;
			}
			var FunctionReference = FunctionScope.Find(Identifier);
			if (FunctionReference != null)
			{
				//throw new NotImplementedException();
				return FunctionReference.CFunctionType;
			}
			throw new Exception(String.Format("Can't find identifier '{0}'", Identifier));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ElementType"></param>
		/// <param name="ArrayFixedLength"></param>
		/// <returns></returns>
		protected override Type ConvertCTypeToType_GetFixedArrayType(CType ElementCType, Type ElementType, int ArrayFixedLength)
		{
			//var StructType = ModuleBuilder.DefineType(CSymbol.Name, TypeAttributes.Public | TypeAttributes.SequentialLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, typeof(ValueType), (PackingSize)4);
			var TypeName = "FixedArrayType_" + ElementType.Name.Replace("*", "Pointer") + "_" + ArrayFixedLength;

			var ReusedType = ModuleBuilder.GetType(TypeName);
			if (ReusedType != null) return ReusedType;

			int ElementSize = 4;

			if (ElementType is TypeBuilder)
			{
				Console.Error.WriteLine("!(ElementType is RuntimeType) :: {0}", ElementType.GetType());
				ElementSize = (ElementType as TypeBuilder).Size;
				
				if (ElementSize == 0)
				{
					ElementSize = ElementCType.GetSize(this);
					if (ElementSize == 0)
					{
						throw (new NotImplementedException(String.Format("ElementSize = 0 : {0}", ElementSize)));
					}
				}
			}
			else
			{
				// TODO: HACK! This way we get the size of the structue on the compiling platform, not the real platform. Pointers have distinct sizes.
				ElementSize = (ElementType != null) ? Marshal.SizeOf(ElementType) : 8;
			}

			// TODO: Fake to get the higher size a pointer would get on x64.
			if (ElementType.IsPointer) ElementSize = 8;

			var TempStruct = ModuleBuilder.DefineType(
				TypeName,
				TypeAttributes.Public | TypeAttributes.SequentialLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
				typeof(ValueType),
				PackingSize.Unspecified,
				ArrayFixedLength * ElementSize
			);

			TempStruct.AddCustomAttribute<FixedArrayAttribute>();

			TempStruct.DefineField("FirstElement", ElementType, FieldAttributes.Public);

			TempStruct.CreateType();

			return TempStruct;
		}

		int ISizeProvider.PointerSize
		{
			get { return 8; }
		}
	}
}
