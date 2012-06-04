using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Parser;
using Codegen;
using System.Reflection;
using ilcclib.Types;
using ilcclib.Utils;
using ilcc.Runtime;
using System.Reflection.Emit;

namespace ilcclib.Converter.CIL
{
	unsafe public partial class CILConverter
	{
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
				var FixedArrayAttributes = Type.GetCustomAttributes(typeof(CFixedArrayAttribute), true);
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
				throw (new NotImplementedException());
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

			DoGenerateAddress(false, () =>
			{
				Traverse(LeftExpression);
			});
			DoGenerateAddress(false, () =>
			{
				Traverse(IndexExpression);
			});

			SafeILGenerator.Sizeof(ElementType);
			SafeILGenerator.BinaryOperation(SafeBinaryOperator.MultiplySigned);
			SafeILGenerator.BinaryOperation(SafeBinaryOperator.AdditionSigned);

			// For fixed array types, get always the address?
			if (ElementCType is CArrayType && (ElementCType as CArrayType).Size != 0)
			{
			}
			else if (!GenerateAddress)
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
				this.StaticInitializerSafeILGenerator.Call((CLibUtils.StringToPointerDelegate)CLibUtils.GetLiteralStringPointer);
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
		/// <param name="LongExpression"></param>
		[CNodeTraverser]
		public void LongExpression(CParser.LongExpression LongExpression)
		{
			SafeILGenerator.Push(LongExpression.Value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="CharExpression"></param>
		[CNodeTraverser]
		public void CharExpression(CParser.CharExpression CharExpression)
		{
			//SafeILGenerator.Push((char)CharExpression.Value);
			SafeILGenerator.Push((sbyte)CharExpression.Value);
			//SafeILGenerator.ConvertTo<sbyte>();
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
				if (Variable.CType is CArrayType && (Variable.CType as CArrayType).Size != 0)
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
				throw (new Exception(string.Format("Not variable or function for identifier {0}", IdentifierExpression)));
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

			var CUnionStructType = LeftCType.GetCUnionStructType();

			CType FieldCType;

			if (CUnionStructType != null)
			{
				FieldCType = CUnionStructType.GetFieldByName(FieldName).CType;
			}
			else
			{
				throw (new NotImplementedException(String.Format("Unknown CTYpe {0}", LeftCType)));
			}

			//Console.WriteLine(LeftCType.GetType());
			//= LeftCType.GetFieldByName(FieldName).CType;
			FieldInfo FieldInfo;
			MethodInfo MethodInfo;
			Type FinalStructType;

			if (LeftType.IsPointer)
			{
				if (FieldAccessExpression.Operator != "->") throw (new InvalidOperationException("A pointer structure should be accesses with the '->' operator"));
				FinalStructType = LeftType.GetElementType();
			}
			else
			{
				if (FieldAccessExpression.Operator != ".") throw (new InvalidOperationException("A non-pointer structure should be accesses with the '.' operator"));
				FinalStructType = LeftType;
			}

			FieldInfo = FinalStructType.GetField(FieldName);
			MethodInfo = FinalStructType.GetMethod("get_" + FieldName);

			if (FieldInfo == null && MethodInfo == null)
			{
				throw (new Exception(String.Format("Can't find field name {0}.{1}", LeftType, FieldName)));
			}

			if (MethodInfo != null && GenerateAddress)
			{
				throw(new InvalidOperationException("Can't generate address for a property"));
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
				if (FieldInfo != null)
				{
					SafeILGenerator.LoadFieldAddress(FieldInfo);
				}
				else
				{
					throw(new InvalidOperationException());
				}
			}
			else
			{
				if (FieldInfo != null)
				{
					SafeILGenerator.LoadField(FieldInfo);
				}
				else if (MethodInfo != null)
				{
					SafeILGenerator.Call(MethodInfo);
				}
				else
				{
					throw (new InvalidOperationException());
				}
			}
			//SafeILGenerator.LoadField
			//throw(new NotImplementedException());
		}

		private void _DoBinaryOperation(string Operator, CTypeSign Signed)
		{
			switch (Operator)
			{
				case "+": SafeILGenerator.BinaryOperation(Signed == CTypeSign.Signed ? SafeBinaryOperator.AdditionSigned : SafeBinaryOperator.AdditionUnsigned); break;
				case "-": SafeILGenerator.BinaryOperation(Signed == CTypeSign.Signed ? SafeBinaryOperator.SubstractionSigned : SafeBinaryOperator.SubstractionUnsigned); break;
				case "*": SafeILGenerator.BinaryOperation(Signed == CTypeSign.Signed ? SafeBinaryOperator.MultiplySigned : SafeBinaryOperator.MultiplyUnsigned); break;
				case "/": SafeILGenerator.BinaryOperation(Signed == CTypeSign.Signed ? SafeBinaryOperator.DivideSigned : SafeBinaryOperator.DivideUnsigned); break;
				case "%": SafeILGenerator.BinaryOperation(Signed == CTypeSign.Signed ? SafeBinaryOperator.RemainingSigned : SafeBinaryOperator.RemainingUnsigned); break;

				case "&": SafeILGenerator.BinaryOperation(SafeBinaryOperator.And); break;
				case "|": SafeILGenerator.BinaryOperation(SafeBinaryOperator.Or); break;
				case "^": SafeILGenerator.BinaryOperation(SafeBinaryOperator.Xor); break;

				case "<<": SafeILGenerator.BinaryOperation(SafeBinaryOperator.ShiftLeft); break;
				case ">>": SafeILGenerator.BinaryOperation(Signed == CTypeSign.Signed ? SafeBinaryOperator.ShiftRightSigned : SafeBinaryOperator.ShiftRightUnsigned); break;

				case "&&": SafeILGenerator.BinaryOperation(SafeBinaryOperator.And); break;
				case "||": SafeILGenerator.BinaryOperation(SafeBinaryOperator.Or); break;

				case "<": SafeILGenerator.CompareBinary(Signed == CTypeSign.Signed ? SafeBinaryComparison.LessThanSigned : SafeBinaryComparison.LessThanUnsigned); break;
				case ">": SafeILGenerator.CompareBinary(Signed == CTypeSign.Signed ? SafeBinaryComparison.GreaterThanSigned : SafeBinaryComparison.GreaterThanUnsigned); break;
				case "<=": SafeILGenerator.CompareBinary(Signed == CTypeSign.Signed ? SafeBinaryComparison.LessOrEqualSigned : SafeBinaryComparison.LessOrEqualUnsigned); break;
				case ">=": SafeILGenerator.CompareBinary(Signed == CTypeSign.Signed ? SafeBinaryComparison.GreaterOrEqualSigned : SafeBinaryComparison.GreaterOrEqualUnsigned); break;
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

			var LeftCType = Left.GetCType(this).GetCSimpleType();
			var RightCType = Right.GetCType(this).GetCSimpleType();

			_DoBinaryOperation(Operator, LeftCType.Sign);
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

			// Assignments
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

						if (RequireYieldResult)
						{
							LeftValueLocal = SafeILGenerator.DeclareLocal(LeftType, "TempLocal");
						}
		
						var LeftFieldAccess = Left as CParser.FieldAccessExpression;
						FieldInfo FieldToStore = null;
						MethodInfo MethodInfoToCallSet = null;
						MethodInfo MethodInfoToCallGet = null;

						// This is a field? Instead of loading the address try to perform a StoreField.
						if (LeftFieldAccess != null && LeftFieldAccess.Operator == ".")
						{
							var StructureCType = LeftFieldAccess.LeftExpression.GetCType(this);
							var StructureType = ConvertCTypeToType(StructureCType);

							FieldToStore = StructureType.GetField(LeftFieldAccess.FieldName);
							MethodInfoToCallSet = StructureType.GetMethod("set_" + LeftFieldAccess.FieldName);
							MethodInfoToCallGet = StructureType.GetMethod("get_" + LeftFieldAccess.FieldName);

							if (FieldToStore == null && MethodInfoToCallSet == null)
							{
								throw(new InvalidOperationException("Null"));
							}

							DoGenerateAddress(true, () =>
							{
								Traverse(LeftFieldAccess.LeftExpression);
							});

						}
						// Other kind, get the address and later it will perform a StoreIndirect.
						else
						{
							DoGenerateAddress(true, () =>
							{
								Traverse(Left);
							});
						}

						// Just store.
						if (Operator == "=")
						{
							DoGenerateAddress(false, () =>
							{
								Traverse(Right);
							});
						}
						// Store the value modified.
						else
						{
							SafeILGenerator.Duplicate();

							if (MethodInfoToCallGet != null)
							{
								SafeILGenerator.Call(MethodInfoToCallGet);
							}
							else
							{
								SafeILGenerator.LoadIndirect(LeftType);
							}

							DoGenerateAddress(false, () =>
							{
								Traverse(Right);
							});
							_DoBinaryOperation(Operator.Substring(0, Operator.Length - 1), LeftCType.GetCSimpleType().Sign);
						}

						// Convert the value to the LeftType.
						SafeILGenerator.ConvertTo(LeftType);

						// Stores the value into the temp variable without poping it.
						if (LeftValueLocal != null)
						{
							SafeILGenerator.Duplicate();
							SafeILGenerator.StoreLocal(LeftValueLocal);
						}

						// Stores the result
						if (FieldToStore != null)
						{
							SafeILGenerator.StoreField(FieldToStore);
						}
						else if (MethodInfoToCallSet != null)
						{
							SafeILGenerator.Call(MethodInfoToCallSet);
						}
						else
						{
							SafeILGenerator.StoreIndirect(LeftType);
						}

						// Yields the result.
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
									_DoBinaryOperation(Operator, Left.GetCType(this).GetCSimpleType().Sign);
									break;
								default:
									Console.Error.WriteLine("Not supported operator '{0}' for pointer aritmetic types : {1}, {2}", Operator, LeftType, RightType);
									throw (new NotImplementedException(String.Format("Not supported operator '{0}' for pointer aritmetic types : {1}, {2}", Operator, LeftType, RightType)));
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
						SafeILGenerator.Push(0);
						SafeILGenerator.CompareBinary(SafeBinaryComparison.Equals);
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
						LocalBuilder VariableToIncrementAddressLocal = null;
						LocalBuilder InitialVariableToIncrementValueLocal = null;
						LocalBuilder PostVariableToIncrementValueLocal = null;

						//RequireYieldResult = true;

						if (RequireYieldResult)
						{
							VariableToIncrementAddressLocal = SafeILGenerator.DeclareLocal(typeof(IntPtr));
						}

						// Load address.
						DoGenerateAddress(true, () => { Traverse(Right); });

						//Console.WriteLine("DEBUG: {0}", RightType);

						// Store.
						if (VariableToIncrementAddressLocal != null)
						{
							// Store initial address
							SafeILGenerator.Duplicate();
							SafeILGenerator.StoreLocal(VariableToIncrementAddressLocal);
						}

						// Load Value
						SafeILGenerator.Duplicate();
						SafeILGenerator.LoadIndirect(RightType);

						if (RequireYieldResult && (OperatorPosition == CParser.OperatorPosition.Right))
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

						if (RequireYieldResult && (OperatorPosition == CParser.OperatorPosition.Left))
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
						if (RequireYieldResult)
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
					throw (new NotImplementedException(String.Format("Unimplemented unary operator '{0}'", Operator)));
			}
		}

	}
}
