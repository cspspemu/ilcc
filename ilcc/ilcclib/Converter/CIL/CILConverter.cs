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
		public MethodInfo MethodInfo { get { return _MethodInfoLazy.Value; } }
		public SafeMethodTypeInfo SafeMethodTypeInfo { get; private set; }
		public CFunctionType CFunctionType;
		Lazy<MethodInfo> _MethodInfoLazy;
		public bool BodyFinalized;
		public bool HasStartedBody { get { return _MethodInfoLazy.IsValueCreated; } }

		public FunctionReference(CILConverter CILConverter, string Name, Lazy<MethodInfo> MethodInfoLazy, SafeMethodTypeInfo SafeMethodTypeInfo = null)
		{
			Initialize(CILConverter, Name, MethodInfoLazy, SafeMethodTypeInfo);
		}

		public FunctionReference(CILConverter CILConverter, string Name, MethodInfo MethodInfo, SafeMethodTypeInfo SafeMethodTypeInfo = null)
		{
			var MethodInfoLazy = new Lazy<MethodInfo>(() => { return MethodInfo; });
			var MethodInfoCreated = MethodInfoLazy.Value;
			Initialize(CILConverter, Name, MethodInfoLazy, SafeMethodTypeInfo);
		}

		private void Initialize(CILConverter CILConverter, string Name, Lazy<MethodInfo> MethodInfoLazy, SafeMethodTypeInfo SafeMethodTypeInfo = null)
		{
			this.Name = Name;
			this._MethodInfoLazy = MethodInfoLazy;
			this.SafeMethodTypeInfo = SafeMethodTypeInfo;

			Type ReturnType;
			Type[] ParametersType;

			BodyFinalized = MethodInfoLazy.IsValueCreated;

			if (SafeMethodTypeInfo != null)
			{
				ReturnType = SafeMethodTypeInfo.ReturnType;
				ParametersType = SafeMethodTypeInfo.Parameters;
			}
			else
			{
				ReturnType = this.MethodInfo.ReturnType;
				ParametersType = this.MethodInfo.GetParameters().Select(Item => Item.ParameterType).ToArray();
			}

			var ReturnCType = CILConverter.ConvertTypeToCType(ReturnType);
			var ParametersCType = new List<CType>();

			foreach (var ParameterType in ParametersType)
			{
				ParametersCType.Add(CILConverter.ConvertTypeToCType(ParameterType));
			}

			this.CFunctionType = new CFunctionType(
				ReturnCType,
				Name,
				ParametersCType.Select(Item => new CSymbol() { CType = Item }).ToArray()
			);
		}
	}

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
				throw(new Exception("Invalid Variable Reference"));
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

	[CConverter(Id = "cil", Description = "Outputs .NET IL code (not fully implemented yet)")]
	unsafe public class CILConverter : TraversableCConverter, CParser.IIdentifierTypeResolver
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="TranslationUnit"></param>
		[CNodeTraverser]
		public void TranslationUnit(CParser.TranslationUnit TranslationUnit)
		{
			try { File.Delete(OutFolder + "\\" + OutputName); } catch { }
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
							this.AssemblyBuilder.Save(OutputName);
						}
					});
				});
			});
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

			CStructType CStructType = CType.GetCStructType();
			if (CStructType == null) return ConvertCTypeToType(CType);

			/*
			if (CType is CSimpleType)
			{
				var CSimpleType = CType as CSimpleType;
				CStructType = (CSimpleType != null) ? (CSimpleType.ComplexType as CStructType) : null;
			}
			*/

			if (CStructType != null)
			{
				//var StructType = RootTypeBuilder.DefineNestedType(CSymbol.Name, TypeAttributes.NestedPublic | TypeAttributes.SequentialLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, typeof(ValueType), (PackingSize)4);
				var StructType = ModuleBuilder.DefineType(Name, TypeAttributes.Public | TypeAttributes.SequentialLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, typeof(ValueType), (PackingSize)4);

				//StructType.StructLayoutAttribute = new StructLayoutAttribute(LayoutKind.Sequential);
				{
					foreach (var Item in CStructType.Items)
					{
						StructType.DefineField(Item.Name, ConvertCTypeToType(Item.CType), FieldAttributes.Public);
					}
					//Console.Error.WriteLine("Not implemented TypeDeclaration");
				}

				//PendingTypesToCreate.Add(StructType);
				StructType.CreateType();

				return StructType;
			}
			else
			{
				//return null;
				throw (new InvalidOperationException(String.Format("CStructType == null : {0}", CType)));
			}
		}

		private Type DefineType(CSymbol CSymbol)
		{
			return DefineType(CSymbol.Name, CSymbol.CType);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="TypeDeclaration"></param>
		[CNodeTraverser]
		public void TypeDeclaration(CParser.TypeDeclaration TypeDeclaration)
		{
			CustomTypeContext.SetTypeByCType(TypeDeclaration.Symbol.CType, DefineType(TypeDeclaration.Symbol));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="VariableDeclaration"></param>
		[CNodeTraverser]
		public void VariableDeclaration(CParser.VariableDeclaration VariableDeclaration)
		{
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

				/*
				if (VariableDeclaration.InitialValue != null)
				{

					// Vector initialization.
					if (VariableDeclaration.InitialValue is CParser.VectorInitializationExpression)
					{
						if (VariableCType is CArrayType)
						{
							Console.Error.WriteLine("Not implemented: Array initialize!");
						}
						else
						{
							Console.Error.WriteLine("Not implemented: Struct initialize!");
						}
					}
					// Normal initialization.
					else
					{
						Variable.LoadAddress(SafeILGenerator);
						Traverse(VariableDeclaration.InitialValue);
						SafeILGenerator.StoreIndirect(VariableType);
					}
				}
				else
				{
					Variable.LoadAddress(SafeILGenerator);
					SafeILGenerator.InitObject(VariableType);
				}
				*/
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
		/// <param name="VectorInitializationExpression"></param>
		[CNodeTraverser]
		public void VectorInitializationExpression(CParser.VectorInitializationExpression VectorInitializationExpression)
		{
			Traverse(VectorInitializationExpression.Expressions);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="CastExpression"></param>
		[CNodeTraverser]
		public void CastExpression(CParser.CastExpression CastExpression)
		{
			Traverse(CastExpression.Right);
			SafeILGenerator.ConvertTo(GetRealType(ConvertCTypeToType(CastExpression.CastType)));
		}

		/*
		private MethodInfo CreateFunction(CFunctionType CFunctionType)
		{
			//FunctionDeclaration.CFunctionType
		}

		private void FinalizeFunction(CFunctionType CFunctionType)
		{
		}
		*/

		/// <summary>
		/// 
		/// </summary>
		/// <param name="FunctionDeclaration"></param>
		[CNodeTraverser]
		public void FunctionDeclaration(CParser.FunctionDeclaration FunctionDeclaration)
		{
			var FunctionName = FunctionDeclaration.CFunctionType.Name;
			var ReturnType = ConvertCTypeToType(FunctionDeclaration.CFunctionType.Return);
			var ParameterTypes = FunctionDeclaration.CFunctionType.Parameters.Select(Item => ConvertCTypeToType(Item.CType)).ToArray();

			if (ParameterTypes.Length == 1 && ParameterTypes[0] == typeof(void)) ParameterTypes = new Type[0];
			var FunctionReference = FunctionScope.Find(FunctionName);

			if (FunctionReference == null)
			{
				var CurrentMethodLazy = new Lazy<MethodInfo>(() =>
				{
					return CurrentClass.DefineMethod(
						FunctionName,
						MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard,
						ReturnType,
						ParameterTypes
					);
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
								int ArgumentIndex = 0;
								foreach (var Parameter in FunctionDeclaration.CFunctionType.Parameters)
								{
									var Argument = SafeILGenerator.DeclareArgument(ConvertCTypeToType(Parameter.CType), ArgumentIndex);
									this.VariableScope.Push(Parameter.Name, new VariableReference(Parameter.Name, Parameter.CType, Argument));
									ArgumentIndex++;
								}

								Traverse(FunctionDeclaration.FunctionBody);
								if (CurrentMethod.ReturnType != typeof(void))
								{
									SafeILGenerator.Push((int)0);
								}
								SafeILGenerator.Return(CurrentMethod.ReturnType);
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ExpressionStatement"></param>
		[CNodeTraverser]
		public void ExpressionStatement(CParser.ExpressionStatement ExpressionStatement)
		{
			//DoRequireYieldResult(false, () =>
			{
				Traverse(ExpressionStatement.Expression);
			}
			//);
			SafeILGenerator.PopLeft();
		}

		/*
		bool RequireYieldResult = true;

		private void DoRequireYieldResult(bool Value, Action Action)
		{
			Scopable.RefScope(ref this.RequireYieldResult, Value, Action);
		}
		*/

		/// <summary>
		/// 
		/// </summary>
		/// <param name="SizeofExpressionExpression"></param>
		[CNodeTraverser]
		public void SizeofExpressionExpression(CParser.SizeofExpressionExpression SizeofExpressionExpression)
		{
			var Type = ConvertCTypeToType(SizeofExpressionExpression.Expression.GetCType(this));
			SafeILGenerator.Sizeof(Type);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="SizeofTypeExpression"></param>
		[CNodeTraverser]
		public void SizeofTypeExpression(CParser.SizeofTypeExpression SizeofTypeExpression)
		{
			var Type = ConvertCTypeToType(SizeofTypeExpression.CType);
			SafeILGenerator.Sizeof(Type);
		}

		private Type GetRealType(Type Type)
		{
			if (!Type.IsPointer)
			{
				var FixedArrayAttributes = Type.GetCustomAttributes(typeof(FixedArrayAttribute), true);
				if ((FixedArrayAttributes != null) && (FixedArrayAttributes.Length > 0))
				{
					return Type.GetField("FirstElement").FieldType.MakePointerType();
				}
			}
			return Type;
		}

#if false
		private void ConvertTo(Type Type)
		{
			var FixedArrayAttributes = Type.GetCustomAttributes(typeof(FixedArrayAttribute), true);
			if ((FixedArrayAttributes != null) && (FixedArrayAttributes.Length > 0))
			{
				SafeILGenerator.ConvertTo(Type.GetField("FirstElement").FieldType.MakePointerType());
			}
			else
			{
				SafeILGenerator.ConvertTo(Type);
			}
		}
#endif

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
				var FunctionName = IdentifierExpression.Identifier;
				var ParametersExpressions = FunctionCallExpression.Parameters.Expressions;

				// Special functions.
				switch (FunctionName)
				{
					// Alloca Special Function.
					case "alloca":
						{
#if true
							// alloca requires the stack to be empty after calling it?
							var Stack = SafeILGenerator.StackSave();
							var AllocaAddressLocal = SafeILGenerator.DeclareLocal(typeof(void*));
							{
								Traverse(ParametersExpressions);
								SafeILGenerator.StackAlloc();
							}
							SafeILGenerator.StoreLocal(AllocaAddressLocal);
							SafeILGenerator.StackRestore(Stack);
							SafeILGenerator.LoadLocal(AllocaAddressLocal);
#else
							var AllocaLocal = SafeILGenerator.DeclareLocal(typeof(void*), "AllocaLocal");
							Traverse(FunctionCallExpression.Parameters.Expressions);
							//SafeILGenerator.ConvertTo(typeof(void*));
							SafeILGenerator.StackAlloc();
							SafeILGenerator.ConvertTo(typeof(void*));
							SafeILGenerator.StoreLocal(AllocaLocal);
							SafeILGenerator.LoadLocal(AllocaLocal);
							//throw(new NotImplementedException("Currently this does not work!"));
#endif
						}
						break;
					
					// Normal plain function.
					default:
						{
							var VariableReference = VariableScope.Find(IdentifierExpression.Identifier);
							var FunctionReference = FunctionScope.Find(IdentifierExpression.Identifier);
							if (VariableReference != null)
							{
								var CFunctionType = VariableReference.CType.GetSpecifiedCType<CFunctionType>();
								var ReturnType = ConvertCTypeToType(CFunctionType.Return);
								var ParameterTypes = CFunctionType.Parameters.Select(Item => ConvertCTypeToType(Item.CType)).ToArray();

								Traverse(ParametersExpressions);
								Traverse(IdentifierExpression);
								SafeILGenerator.CallManagedFunction(CallingConventions.Standard, ReturnType, ParameterTypes, null);
							}
							else if (FunctionReference != null)
							{
								Type[] ParameterTypes;

								if (FunctionReference.SafeMethodTypeInfo == null)
								{
									if (FunctionReference.MethodInfo.CallingConvention == CallingConventions.VarArgs)
									{
										ParameterTypes = FunctionCallExpression.Parameters.Expressions.Select(Expression => ConvertCTypeToType(Expression.GetCType(this))).ToArray();
									}
									else
									{
										ParameterTypes = FunctionReference.MethodInfo.GetParameters().Select(Parameter => Parameter.ParameterType).ToArray();
									}
								}
								else
								{
									ParameterTypes = FunctionReference.SafeMethodTypeInfo.Parameters;
								}

								if (ParameterTypes.Length != ParametersExpressions.Length)
								{
									throw (new Exception(String.Format(
										"Function parameter count mismatch {0} != {1} calling function '{2}'",
										ParameterTypes.Length, ParametersExpressions.Length, FunctionName
									)));
								}

								ParameterTypes = ParameterTypes.Select(Item => GetRealType(Item)).ToArray();

								for (int n = 0; n < ParametersExpressions.Length; n++)
								{
									var Expression = ParametersExpressions[n];
									var ExpressionCType = Expression.GetCType(this);
									var ExpressionType = ConvertCTypeToType(ExpressionCType);
									var ParameterType = GetRealType(ParameterTypes[n]);
									Traverse(Expression);

									// Expected a string. Convert it!
									if (ParameterType == typeof(string))
									{
										if (ExpressionType == typeof(sbyte*))
										{
											SafeILGenerator.ConvertTo(typeof(sbyte*));
											SafeILGenerator.Call((CLibUtils.PointerToStringDelegate)CLibUtils.GetStringFromPointer);
										}
										else
										{
											throw (new NotImplementedException(String.Format("Invalid string expression {0}", ExpressionType)));
										}
									}
									else
									{
										SafeILGenerator.ConvertTo(ParameterType);
									}
								}

								if (FunctionReference.SafeMethodTypeInfo == null && FunctionReference.MethodInfo.CallingConvention == CallingConventions.VarArgs)
								{
									//SafeILGenerator.LoadFunctionPointer(FunctionReference.MethodInfo, IsVirtual: false);
									//SafeILGenerator.CallManagedFunction(CallingConventions.VarArgs, FunctionReference.MethodInfo.ReturnType, ParameterTypes, null);
									SafeILGenerator.Call(FunctionReference.MethodInfo, FunctionReference.SafeMethodTypeInfo, ParameterTypes);
								}
								else
								{
									SafeILGenerator.Call(FunctionReference.MethodInfo, FunctionReference.SafeMethodTypeInfo);
								}
							}
							else
							{
								throw (new Exception(String.Format("Unknown function '{0}'", IdentifierExpression.Identifier)));
							}

							//SafeILGenerator.__ILGenerator.Emit(OpCodes.Call
							//throw (new NotImplementedException("Function: " + IdentifierExpression.Value));
						}
						break;
				}

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
			var LeftCType = (LeftExpression.GetCType(this) as CBasePointerType);
			var LeftType = ConvertCTypeToType(LeftCType);
			var ElementCType = LeftCType.ElementCType;
			var ElementType = ConvertCTypeToType(ElementCType);
			var IndexExpression = ArrayAccessExpression.Index;

#if false
			var ArrayAccessGenerateAddress = false;
			if (LeftCType is CArrayType) ArrayAccessGenerateAddress = true;
			DoGenerateAddress(ArrayAccessGenerateAddress, () => { Traverse(LeftExpression); });
			DoGenerateAddress(false, () => { Traverse(IndexExpression); });
#else

#if false
			// Temporal hack!
			if (LeftExpression is CParser.DereferenceExpression)
			{
				DoGenerateAddress(true, () =>
				{
					Traverse((LeftExpression as CParser.DereferenceExpression).Expression);
				});
			}
			else
#endif
			DoGenerateAddress(false, () =>
			{
				Traverse(LeftExpression);
			});
			DoGenerateAddress(false, () =>
			{
				Traverse(IndexExpression);
			});
#endif

			SafeILGenerator.Sizeof(ElementType);
			SafeILGenerator.BinaryOperation(SafeBinaryOperator.MultiplySigned);
			SafeILGenerator.BinaryOperation(SafeBinaryOperator.AdditionSigned);

			if (!GenerateAddress)
			{
				SafeILGenerator.LoadIndirect(ConvertCTypeToType(ElementCType));
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Set"></param>
		/// <param name="Action"></param>
		private void DoGenerateAddress(bool Set, Action Action)
		{
			Scopable.RefScope(ref this.GenerateAddress, Set, Action);
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
		/// <param name="FloatExpression"></param>
		[CNodeTraverser]
		public void FloatExpression(CParser.FloatExpression FloatExpression)
		{
			SafeILGenerator.Push(FloatExpression.Value);
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
		/// <example>Condition ? TrueExpression : FalseExpression</example>
		[CNodeTraverser]
		public void TrinaryExpression(CParser.TrinaryExpression TrinaryExpression)
		{
			var Condition = TrinaryExpression.Condition;
			var TrueExpression = TrinaryExpression.TrueExpression;
			var FalseExpression = TrinaryExpression.FalseExpression;

			var CommonCType = CType.CommonType(TrueExpression.GetCType(this), FalseExpression.GetCType(this));
			var CommonType = GetRealType(ConvertCTypeToType(CommonCType));

			var TrinaryTempLocal = SafeILGenerator.DeclareLocal(CommonType, "TrinaryTempLocal");

			// Condition.
			Traverse(TrinaryExpression.Condition);

			// Check the value and store the result in the temp local.
			SafeILGenerator.MacroIfElse(() =>
			{
				Traverse(TrinaryExpression.TrueExpression);
				SafeILGenerator.ConvertTo(CommonType);
				SafeILGenerator.StoreLocal(TrinaryTempLocal);
			}, () =>
			{
				Traverse(TrinaryExpression.FalseExpression);
				SafeILGenerator.ConvertTo(CommonType);
				SafeILGenerator.StoreLocal(TrinaryTempLocal);
			});

			// Load temp local.
			SafeILGenerator.LoadLocal(TrinaryTempLocal);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="IdentifierExpression"></param>
		[CNodeTraverser]
		public void IdentifierExpression(CParser.IdentifierExpression IdentifierExpression)
		{
			var Variable = VariableScope.Find(IdentifierExpression.Identifier);
			var Function = FunctionScope.Find(IdentifierExpression.Identifier);

			if (Variable != null)
			{
				// For fixed array types, get always the address?
				if (Variable.CType is CArrayType)
				{
					Variable.LoadAddress(SafeILGenerator);
				}
				else if (GenerateAddress)
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
			else if (Function != null)
			{
				//SafeILGenerator.Push(Function.MethodInfo);
				SafeILGenerator.LoadFunctionPointer(Function.MethodInfo, false);
				SafeILGenerator.ConvertTo(typeof(void*));
			}
			else
			{
				throw(new Exception(string.Format("Not variable or function for identifier {0}", IdentifierExpression)));
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="FieldAccessExpression"></param>
		[CNodeTraverser]
		public void FieldAccessExpression(CParser.FieldAccessExpression FieldAccessExpression)
		{
			var FieldName = FieldAccessExpression.FieldName;
			var LeftExpression = FieldAccessExpression.LeftExpression;
			var LeftCType = LeftExpression.GetCType(this);
			var LeftType = ConvertCTypeToType(LeftCType);
			CType FieldCType = LeftCType.GetCStructType().GetFieldByName(FieldName).CType;
			//Console.WriteLine(LeftCType.GetType());
			//= LeftCType.GetFieldByName(FieldName).CType;
			FieldInfo FieldInfo;

			if (LeftType.IsPointer)
			{
				if (FieldAccessExpression.Operator != "->") throw(new InvalidOperationException("A pointer structure should be accesses with the '->' operator"));
				FieldInfo = LeftType.GetElementType().GetField(FieldName);
			}
			else
			{
				if (FieldAccessExpression.Operator != ".") throw (new InvalidOperationException("A non-pointer structure should be accesses with the '.' operator"));
				FieldInfo = LeftType.GetField(FieldName);
			}

			if (FieldInfo == null)
			{
				throw (new Exception(String.Format("Can't find field name {0}.{1}", LeftType, FieldName)));
			}
			//Console.WriteLine(FieldInfo);

			DoGenerateAddress(true, () =>
			{
				Traverse(FieldAccessExpression.LeftExpression);
			});

			//Console.WriteLine(FieldCType);

			// For fixed array types, get always the address?
			if (GenerateAddress || FieldCType is CArrayType)
			{
				SafeILGenerator.LoadFieldAddress(FieldInfo);
			}
			else
			{
				SafeILGenerator.LoadField(FieldInfo);
			}
			//SafeILGenerator.LoadField
			//throw(new NotImplementedException());
		}

		private void _DoBinaryOperation(string Operator)
		{
			switch (Operator)
			{
				case "+": SafeILGenerator.BinaryOperation(SafeBinaryOperator.AdditionSigned); break;
				case "-": SafeILGenerator.BinaryOperation(SafeBinaryOperator.SubstractionSigned); break;
				case "*": SafeILGenerator.BinaryOperation(SafeBinaryOperator.MultiplySigned); break;
				case "/": SafeILGenerator.BinaryOperation(SafeBinaryOperator.DivideSigned); break;
				case "%": SafeILGenerator.BinaryOperation(SafeBinaryOperator.RemainingSigned); break;

				case "&": SafeILGenerator.BinaryOperation(SafeBinaryOperator.And); break;
				case "|": SafeILGenerator.BinaryOperation(SafeBinaryOperator.Or); break;
				case "^": SafeILGenerator.BinaryOperation(SafeBinaryOperator.Xor); break;

				case "<<": SafeILGenerator.BinaryOperation(SafeBinaryOperator.ShiftLeft); break;
				case ">>": SafeILGenerator.BinaryOperation(SafeBinaryOperator.ShiftRightUnsigned); break;

				case "&&": SafeILGenerator.BinaryOperation(SafeBinaryOperator.And); break;
				case "||": SafeILGenerator.BinaryOperation(SafeBinaryOperator.Or); break;

				case "<": SafeILGenerator.CompareBinary(SafeBinaryComparison.LessThanSigned); break;
				case ">": SafeILGenerator.CompareBinary(SafeBinaryComparison.GreaterThanSigned); break;
				case "<=": SafeILGenerator.CompareBinary(SafeBinaryComparison.LessOrEqualSigned); break;
				case ">=": SafeILGenerator.CompareBinary(SafeBinaryComparison.GreaterOrEqualSigned); break;
				case "==": SafeILGenerator.CompareBinary(SafeBinaryComparison.Equals); break;
				case "!=": SafeILGenerator.CompareBinary(SafeBinaryComparison.NotEquals); break;

				default: throw (new NotImplementedException(String.Format("Operator {0} not implemented", Operator)));
			}
		}

		private void _DoBinaryLeftRightPost(string Operator)
		{
			switch (Operator)
			{
				case "&&":
				case "||":
					SafeILGenerator.ConvertTo<bool>();
					break;
			}
		}

		private void DoBinaryOperation(string Operator, CParser.Expression Left, CParser.Expression Right)
		{
			DoGenerateAddress(false, () =>
			{
				Traverse(Left);
				_DoBinaryLeftRightPost(Operator);
				Traverse(Right);
				_DoBinaryLeftRightPost(Operator);
			});

			_DoBinaryOperation(Operator);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="BinaryExpression"></param>
		[CNodeTraverser]
		public void BinaryExpression(CParser.BinaryExpression BinaryExpression)
		{
			var Operator = BinaryExpression.Operator;
	
			var Left = BinaryExpression.Left;
			var LeftCType = Left.GetCType(this);
			var LeftType = ConvertCTypeToType(LeftCType);

			var Right = BinaryExpression.Right;
			var RightCType = Right.GetCType(this);
			var RightType = ConvertCTypeToType(RightCType);

			switch (Operator)
			{
				case "<<=":
				case ">>=":
				case "&=":
				case "|=":
				case "^=":
				case "*=":
				case "/=":
				case "%=":
				case "-=":
				case "+=":
				case "=":
					{
						LocalBuilder LeftValueLocal = null;
						LocalBuilder LeftPointerAddressLocal = null;

						//Console.WriteLine(LeftType);

						//if (RequireYieldResult)
						{
							
							LeftValueLocal = SafeILGenerator.DeclareLocal(LeftType, "TempLocal");
							LeftPointerAddressLocal = SafeILGenerator.DeclareLocal(LeftType.MakePointerType(), "LeftTempLocal");
						}

						//DoRequireYieldResult(true, () =>
						{
							DoGenerateAddress(true, () =>
							{
								Traverse(Left);
							});

							SafeILGenerator.Duplicate();
							SafeILGenerator.StoreLocal(LeftPointerAddressLocal);

						//SafeILGenerator.ConvertTo(ConvertCTypeToType(Left.GetCType(this)));

							if (Operator == "=")
							{
								DoGenerateAddress(false, () =>
								{
									Traverse(Right);
								});
							}
							else
							{
								SafeILGenerator.LoadLocal(LeftPointerAddressLocal);
								SafeILGenerator.LoadIndirect(LeftType);
								DoGenerateAddress(false, () =>
								{
									Traverse(Right);
								});
								_DoBinaryOperation(Operator.Substring(0, Operator.Length - 1));
							}
						}
						//);

						SafeILGenerator.ConvertTo(LeftType);

						if (LeftValueLocal != null)
						{
							SafeILGenerator.Duplicate();
							SafeILGenerator.StoreLocal(LeftValueLocal);
						}

						SafeILGenerator.StoreIndirect(LeftType);

						if (LeftValueLocal != null)
						{
							SafeILGenerator.LoadLocal(LeftValueLocal);
						}
					}
					return;
				default:
					{
						// Pointer operations.
						if (LeftType.IsPointer || RightType.IsPointer)
						{
							switch (Operator)
							{
								case "+":
									DoGenerateAddress(false, () =>
									{
										Traverse(Left);
										Traverse(Right);
									});
									SafeILGenerator.Sizeof(LeftType.GetElementType());
									SafeILGenerator.BinaryOperation(SafeBinaryOperator.MultiplyUnsigned);
									SafeILGenerator.BinaryOperation(SafeBinaryOperator.AdditionUnsigned);
									break;
								case "-":
									// TODO: Check both types?!
									DoGenerateAddress(false, () =>
									{
										Traverse(Left);
										Traverse(Right);
									});
									SafeILGenerator.BinaryOperation(SafeBinaryOperator.SubstractionSigned);
									SafeILGenerator.Sizeof(LeftType.GetElementType());
									SafeILGenerator.BinaryOperation(SafeBinaryOperator.DivideUnsigned);
									break;
								case ">=":
								case "<=":
								case ">":
								case "<":
								case "==":
								case "!=":
								case "&&":
								case "||":
									DoGenerateAddress(false, () =>
									{
										Traverse(Left);
										Traverse(Right);
									});
									_DoBinaryOperation(Operator);
									break;
								default:
									Console.Error.WriteLine("Not supported operator '{0}' for pointer aritmetic types : {1}, {2}", Operator, LeftType, RightType);
									throw(new NotImplementedException(String.Format("Not supported operator '{0}' for pointer aritmetic types : {1}, {2}", Operator, LeftType, RightType)));
							}
						}
						// Non-pointer operations
						else
						{
							DoBinaryOperation(Operator, Left, Right);
						}
					}
					break;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ReferenceExpression"></param>
		[CNodeTraverser]
		public void ReferenceExpression(CParser.ReferenceExpression ReferenceExpression)
		{
			DoGenerateAddress(true, () =>
			{
				Traverse(ReferenceExpression.Expression);
			});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="DereferenceExpression"></param>
		[CNodeTraverser]
		public void DereferenceExpression(CParser.DereferenceExpression DereferenceExpression)
		{
			var Expression = DereferenceExpression.Expression;
			var ExpressionType = ConvertCTypeToType(Expression.GetCType(this));

			DoGenerateAddress(false, () =>
			{
				Traverse(Expression);
			});

			//SafeILGenerator.LoadIndirect(ExpressionType);

			//Console.WriteLine("{0}, {1}", ExpressionType, ElementType);

			if (!GenerateAddress)
			{
				var ElementType = ExpressionType.GetElementType();
				SafeILGenerator.LoadIndirect(ElementType);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="UnaryExpression"></param>
		[CNodeTraverser]
		public void UnaryExpression(CParser.UnaryExpression UnaryExpression)
		{
			var Operator = UnaryExpression.Operator;
			var OperatorPosition = UnaryExpression.OperatorPosition;
			var Right = UnaryExpression.Right;
			var RightCType = Right.GetCType(this);
			var RightType = ConvertCTypeToType(RightCType);

			switch (Operator)
			{
				case "~":
					{
						if (OperatorPosition != CParser.OperatorPosition.Left) throw (new InvalidOperationException());
						DoGenerateAddress(false, () => { Traverse(Right); });
						SafeILGenerator.UnaryOperation(SafeUnaryOperator.Not);
					}
					break;
				case "!":
					{
						if (OperatorPosition != CParser.OperatorPosition.Left) throw (new InvalidOperationException());
						DoGenerateAddress(false, () => { Traverse(Right); });
						SafeILGenerator.ConvertTo<bool>();
						SafeILGenerator.UnaryOperation(SafeUnaryOperator.Not);
					}
					break;
				case "-":
					{
						if (OperatorPosition != CParser.OperatorPosition.Left) throw (new InvalidOperationException());
						DoGenerateAddress(false, () => { Traverse(Right); });
						SafeILGenerator.UnaryOperation(SafeUnaryOperator.Negate);
					}
					break;
				case "+":
					{
						if (OperatorPosition != CParser.OperatorPosition.Left) throw (new InvalidOperationException());
						DoGenerateAddress(false, () => { Traverse(Right); });
					}
					break;
				case "++":
				case "--":
					{
						LocalBuilder VariableToIncrementAddressLocal = SafeILGenerator.DeclareLocal(typeof(IntPtr));
						LocalBuilder InitialVariableToIncrementValueLocal = null;
						LocalBuilder PostVariableToIncrementValueLocal = null;

						// Load address.
						DoGenerateAddress(true, () => { Traverse(Right); });

						//Console.WriteLine("DEBUG: {0}", RightType);

						// Store.
						{
							// Store initial address
							SafeILGenerator.Duplicate();
							SafeILGenerator.StoreLocal(VariableToIncrementAddressLocal);
						}

						// Load Value
						SafeILGenerator.Duplicate();
						SafeILGenerator.LoadIndirect(RightType);

						if (OperatorPosition == CParser.OperatorPosition.Right)
						{
							// Store initial value
							InitialVariableToIncrementValueLocal = SafeILGenerator.DeclareLocal(RightType);
							SafeILGenerator.Duplicate();
							SafeILGenerator.StoreLocal(InitialVariableToIncrementValueLocal);
						}

						// Increment/Decrement by 1 the value.
						if (RightType.IsPointer)
						{
							SafeILGenerator.Sizeof(RightType.GetElementType());
						}
						else
						{
							SafeILGenerator.Push(1);
						}

						SafeILGenerator.BinaryOperation((Operator == "++") ? SafeBinaryOperator.AdditionSigned : SafeBinaryOperator.SubstractionSigned);

						if (OperatorPosition == CParser.OperatorPosition.Left)
						{
							// Store the post value
							PostVariableToIncrementValueLocal = SafeILGenerator.DeclareLocal(RightType);
							SafeILGenerator.Duplicate();
							SafeILGenerator.StoreLocal(PostVariableToIncrementValueLocal);
						}

						// Store the updated value.
						SafeILGenerator.StoreIndirect(RightType);

						/*
						if (GenerateAddress)
						{
							//throw(new NotImplementedException("Can't generate address for a ++ or -- expression"));
							SafeILGenerator.LoadLocal(VariableToIncrementAddressLocal);
						}
						else
						*/
						{
							if (OperatorPosition == CParser.OperatorPosition.Left)
							{
								SafeILGenerator.LoadLocal(PostVariableToIncrementValueLocal);
							}
							else if (OperatorPosition == CParser.OperatorPosition.Right)
							{
								SafeILGenerator.LoadLocal(InitialVariableToIncrementValueLocal);
							}
						}
					}
					break;
				default:
					throw (new NotImplementedException(String.Format("Unimplemented unray operator '{0}'", Operator)));
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

		protected override Type ConvertCTypeToType_GetFixedArrayType(Type ElementType, int ArrayFixedLength)
		{
			//var StructType = ModuleBuilder.DefineType(CSymbol.Name, TypeAttributes.Public | TypeAttributes.SequentialLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, typeof(ValueType), (PackingSize)4);
			var TypeName = "FixedArrayType_" + ElementType.Name.Replace("*", "Pointer") + "_" + ArrayFixedLength;

			var ReusedType = ModuleBuilder.GetType(TypeName);
			if (ReusedType != null) return ReusedType;

			// HACK! This way we get the size of the structue on the compiling platform, not the real platform. Pointers have distinct sizes.
			int ElementSize = Marshal.SizeOf(ElementType);

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
	}
}
