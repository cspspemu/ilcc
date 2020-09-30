﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Types;

namespace ilcclib.Parser
{
	public partial class CParser
	{
		public interface IConstantResolver
		{
			object GetConstantIdentifier(string Name);
		}

		//public bool WarningBinaryNoCast = false;

		/// <summary>
		/// A special identifier.
		/// </summary>
		/// <example>__func__</example>
		[Serializable]
		public sealed class SpecialIdentifierExpression : LiteralExpression
		{
			public string Value { get; private set; }

			public SpecialIdentifierExpression(string Value)
				: base()
			{
				this.Value = Value;
			}

			protected override string GetParameter()
			{
				return String.Format("{0}", Value);
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				throw new NotImplementedException();
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				throw (new NotImplementedException());
				//return new CPointerType(new CSimpleType() { BasicType = CTypeBasic.Char });
			}
		}

		/// <summary>
		/// Identifier
		/// </summary>
		/// <example>varname</example>
		[Serializable]
		public sealed class IdentifierExpression : LiteralExpression
		{
			public string Identifier { get; private set; }

			public IdentifierExpression(string Identifier)
				: base()
			{
				this.Identifier = Identifier;
			}

			protected override string GetParameter()
			{
				return String.Format("{0}", Identifier);
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				if (IConstantResolver == null) return null;
				//var Value = Scope.FindSymbol(Identifier).ConstantValue;
				var Value = IConstantResolver.GetConstantIdentifier(Identifier);
				if (Value == null)
				{
					//throw (new InvalidOperationException("A IdentifierExpression is not a constant value"));
					//return null;
				}
				return Value;
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				if (Resolver == null) return null;
				return Resolver.ResolveIdentifierType(Identifier);
			}
		}

		/// <summary>
		/// A string literal.
		/// </summary>
		/// <example>"string"</example>
		[Serializable]
		public sealed class StringExpression : LiteralExpression
		{
			public string String { get; private set; }

			public StringExpression(string String)
				: base()
			{
				this.String = String;
			}

			protected override string GetParameter()
			{
				return String.Format("{0}", String);
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				return String;
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				return new CPointerType(new CSimpleType() { BasicType = CTypeBasic.Char });
			}
		}

		/// <summary>
		/// Expression that calculates the size of an expression.
		/// </summary>
		/// <example>sizeof(vv[0])</example>
		[Serializable]
		public sealed class SizeofExpressionExpression : Expression
		{
			public Expression Expression { get; private set; }

			public SizeofExpressionExpression(Expression Expression)
				: base(Expression)
			{
				this.Expression = Expression;
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				return new CSimpleType() { BasicType = CTypeBasic.Int };
			}

			protected override string GetParameter()
			{
				return String.Format("");
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				//return CSimpleType.GetSize();
				//throw new NotImplementedException();
				var CType = this.Expression.GetCachedCType(null);
				return (CType != null) ? CType.GetSize(null) : null;
			}
		}

		/// <summary>
		/// Expression that calculates the size of a type.
		/// </summary>
		/// <example>sizeof(int)</example>
		[Serializable]
		public sealed class SizeofTypeExpression : Expression
		{
			public CType CType { get; private set; }

			public SizeofTypeExpression(CType CType)
				: base()
			{
				this.CType = CType;
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				return new CSimpleType() { BasicType = CTypeBasic.Int };
			}

			protected override string GetParameter()
			{
				return String.Format("{0}", CType);
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				if (CType is CSimpleType)
				{
					return (CType as CSimpleType).GetSize(null);
				}
				//return CSimpleType.GetSize();
				return null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		[Serializable]
		public sealed class DoubleExpression : LiteralExpression
		{
			public double Value { get; private set; }

			public DoubleExpression(double Value)
				: base()
			{
				this.Value = Value;
			}

			protected override string GetParameter()
			{
				return String.Format("{0}", Value);
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				return Value;
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				return new CSimpleType() { BasicType = CTypeBasic.Float };
			}
		}

		/// <summary>
		/// A float literal.
		/// </summary>
		/// <example>10.777</example>
		[Serializable]
		public sealed class FloatExpression : LiteralExpression
		{
			public float Value { get; private set; }

			public FloatExpression(float Value)
				: base()
			{
				this.Value = Value;
			}

			protected override string GetParameter()
			{
				return String.Format("{0}", Value);
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				return Value;
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				return new CSimpleType() { BasicType = CTypeBasic.Float };
			}
		}

		/// <summary>
		/// 
		/// </summary>
		[Serializable]
		public sealed class CharExpression : LiteralExpression
		{
			public char Value { get; private set; }

			public CharExpression(char Value)
				: base()
			{
				this.Value = Value;
			}

			protected override string GetParameter()
			{
				return String.Format("'{0}'", Value);
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				return (int)Value;
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				return new CSimpleType() { BasicType = CTypeBasic.Char };
			}
		}

		/// <summary>
		/// 
		/// </summary>
		[Serializable]
		public sealed class LongExpression : LiteralExpression
		{
			public long Value { get; private set; }

			public LongExpression(long Value)
				: base()
			{
				this.Value = Value;
			}

			protected override string GetParameter()
			{
				return String.Format("{0}", Value);
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				return Value;
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				return new CSimpleType() { BasicType = CTypeBasic.Int };
			}
		}

		/// <summary>
		/// An integer literal.
		/// </summary>
		/// <example>1234</example>
		[Serializable]
		public sealed class IntegerExpression : LiteralExpression
		{
			public int Value { get; private set; }

			public IntegerExpression(int Value)
				: base()
			{
				this.Value = Value;
			}

			protected override string GetParameter()
			{
				return String.Format("{0}", Value);
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				return Value;
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				return new CSimpleType() { BasicType = CTypeBasic.Int };
			}
		}

		/// <summary>
		/// A Literal.
		/// </summary>
		[Serializable]
		abstract public class LiteralExpression : Expression
		{
			public LiteralExpression()
				: base()
			{
			}
		}

		/// <summary>
		/// Condition ? TrueExpression : FalseExpression
		/// </summary>
		/// <example>(var == 1) ? true : false</example>
		[Serializable]
		public sealed class TrinaryExpression : Expression
		{
			public Expression Condition { get; private set; }
			public Expression TrueExpression { get; private set; }
			public Expression FalseExpression { get; private set; }

			public TrinaryExpression(Expression Left, Expression TrueCond, Expression FalseCond)
				: base(Left, TrueCond, FalseCond)
			{
				this.Condition = Left;
				this.TrueExpression = TrueCond;
				this.FalseExpression = FalseCond;
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				throw new NotImplementedException();
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				var TrueType = TrueExpression.GetCachedCType(Resolver);
				var FalseType = FalseExpression.GetCachedCType(Resolver);
				if (TrueType == FalseType)
				{
					return TrueType;
				}
				else
				{
					var TrueSize = TrueType.GetSize(null);
					var FalseSize = FalseType.GetSize(null);
					if (TrueSize > FalseSize) return TrueType;
					if (FalseSize > TrueSize) return FalseType;
					throw (new NotImplementedException());
				}
			}
		}

		/// <summary>
		/// Cast an expression into a type.
		/// </summary>
		/// <example>(int)1.2</example>
		[Serializable]
		public sealed class CastExpression : Expression
		{
			public CType CastType { get; private set; }
			public Expression Right { get; private set; }

			public CastExpression(CType CastType, Expression Right)
			{
				this.CastType = CastType;
				this.Right = Right;
			}

			protected override string GetParameter()
			{
				return String.Format("{0}", CastType);
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				throw new NotImplementedException();
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				return CastType;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		[Serializable]
		public enum OperatorPosition
		{
			Left,
			Right
		}

		[Serializable]
		public sealed class ReferenceExpression : Expression
		{
			public Expression Expression { get; set; }

			public ReferenceExpression(Expression Expression)
				: base(Expression)
			{
				this.Expression = Expression;
			}

			protected override string GetParameter()
			{
				return "&";
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				return new CPointerType(Expression.GetCachedCType(Resolver));
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				throw new NotImplementedException();
			}
		}

		[Serializable]
		public sealed class DereferenceExpression : Expression
		{
			public Expression Expression { get; set; }

			public DereferenceExpression(Expression Expression)
				: base(Expression)
			{
				this.Expression = Expression;
			}

			protected override string GetParameter()
			{
				return "*";
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				var CBasePointerType = Expression.GetCachedCType(Resolver) as CBasePointerType;
				if (CBasePointerType == null) throw(new Exception("Trying to dereference a non-pointer type"));
				return CBasePointerType.ElementCType;
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				//throw new NotImplementedException();
				return null;
			}
		}

		/// <summary>
		/// An unary operation.
		/// </summary>
		/// <example>-123</example>
		[Serializable]
		public sealed class UnaryExpression : Expression
		{
			public string Operator { get; private set; }
			public Expression Right { get; private set; }
			public OperatorPosition OperatorPosition { get; private set; }

			public UnaryExpression(string Operator, Expression Right, OperatorPosition OperatorPosition = OperatorPosition.Left)
				: base(Right)
			{
				this.Operator = Operator;
				this.Right = Right;
				this.OperatorPosition = OperatorPosition;
			}

			protected override string GetParameter()
			{
				return String.Format("{0} ({1})", Operator, OperatorPosition);
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				var RightValue = Right.GetCachedConstantValue(IConstantResolver);

				switch (Operator)
				{
					case "-": return -((dynamic)RightValue);
					default:
						throw (new NotImplementedException(String.Format("Not implemented constant unary operator '{0}'", Operator)));
				}
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				return Right.GetCachedCType(Resolver);
			}
		}

		/// <summary>
		/// Struct.Field
		/// </summary>
		/// <example>point.x</example>
		[Serializable]
		public sealed class FieldAccessExpression : Expression
		{
			public string Operator { get; private set; }
			public Expression LeftExpression { get; private set; }
			public string FieldName { get; private set; }

			public FieldAccessExpression(string Operator, Expression Left, string FieldName)
				: base(Left)
			{
				this.Operator = Operator;
				this.LeftExpression = Left;
				this.FieldName = FieldName;
			}

			protected override string GetParameter()
			{
				return String.Format("{0}{1}", Operator, FieldName);
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				//throw (new InvalidOperationException("A FieldAccessExpression is not a constant value"));
				return null;
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				var LeftCType = LeftExpression.GetCachedCType(Resolver);
				var CUnionStructType = LeftCType.GetCUnionStructType();

				if (CUnionStructType != null)
				{
					return CUnionStructType.GetFieldByName(FieldName).CType;
				}
				else
				{
					throw (new NotImplementedException(String.Format("Invalid CType '{0}', {1}", LeftCType, LeftCType.GetType())));
				}
			}
		}

		/// <summary>
		/// Array[IndexExpression]
		/// </summary>
		/// <example>array[3]</example>
		[Serializable]
		public sealed class ArrayAccessExpression : Expression
		{
			public Expression Left { get; private set; }
			public Expression Index { get; private set; }

			public ArrayAccessExpression(Expression Left, Expression Index)
				: base(Left, Index)
			{
				this.Left = Left;
				this.Index = Index;
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				throw (new InvalidOperationException("An ArrayAccessExpression is not a constant value"));
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				if (Resolver == null) return null;

				var LeftCType = Left.GetCachedCType(Resolver);

				if (LeftCType == null)
				{
					Console.Error.WriteLine("ArrayAccessExpression.GetCType : LeftCType is null");
					throw (new NullReferenceException("LeftCType is null"));
				}

				var CBasePointerType = (LeftCType as CBasePointerType);

				if (CBasePointerType == null)
				{
					Console.Error.WriteLine("ArrayAccessExpression.GetCType : CBasePointerType is null");
					Console.Error.WriteLine("LeftCType: {0} : {1}", LeftCType.GetType(), LeftCType);
					throw (new NullReferenceException("CBasePointerType is null"));
				}

				//return (Left.GetCType(Resolver) as CBasePointerType).GetCSimpleType();
				return CBasePointerType.GetChildTypes().First();
			}
		}

		/// <summary>
		/// Left op Right
		/// </summary>
		/// <example>1 + 2</example>
		[Serializable]
		public sealed class BinaryExpression : Expression
		{
			public Expression Left { get; private set; }
			public string Operator { get; private set; }
			public Expression Right { get; private set; }

			public BinaryExpression(Expression Left, string Operator, Expression Right)
				: base(Left, Right)
			{
				this.Left = Left;
				this.Operator = Operator;
				this.Right = Right;
			}

			protected override string GetParameter()
			{
				return String.Format("{0}", Operator);
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				var LeftValue = Left.GetCachedConstantValue(IConstantResolver);
				var RightValue = Right.GetCachedConstantValue(IConstantResolver);

				if (LeftValue == null) return null;
				if (RightValue == null) return null;

				switch (Operator)
				{
					case "+": return (object)((dynamic)LeftValue + (dynamic)RightValue);
					case "-": return (object)((dynamic)LeftValue - (dynamic)RightValue);
					case "*": return (object)((dynamic)LeftValue * (dynamic)RightValue);
					case "/": return (object)((dynamic)LeftValue / (dynamic)RightValue);
					case "%": return (object)((dynamic)LeftValue % (dynamic)RightValue);
					case "==": return (bool)((dynamic)LeftValue == (dynamic)RightValue);
					case "!=": return (bool)((dynamic)LeftValue != (dynamic)RightValue);
					case "<": return (bool)((dynamic)LeftValue < (dynamic)RightValue);
					case ">": return (bool)((dynamic)LeftValue > (dynamic)RightValue);
					case "<=": return (bool)((dynamic)LeftValue <= (dynamic)RightValue);
					case ">=": return (bool)((dynamic)LeftValue >= (dynamic)RightValue);
					case "&&": return (bool)((bool)LeftValue && (bool)RightValue);
					case "||": return (bool)((bool)LeftValue || (bool)RightValue);
					default:
						throw (new NotImplementedException(String.Format("Not implemented constant binary operator '{0}'", Operator)));
				}
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				var LeftCType = Left.GetCachedCType(Resolver);
				var RightCType = Right.GetCachedCType(Resolver);

				if (LeftCType == RightCType)
				{
					return LeftCType;
				}
				else
				{
					var DoubleCType = new CSimpleType() { BasicType = CTypeBasic.Double };
					//var FloatCType = new CSimpleType() { BasicType = CTypeBasic.Float };

					if (CType.ContainsOne(LeftCType, RightCType, DoubleCType)) return DoubleCType;
					//if (CType.ContainsOne(LeftCType, RightCType, FloatCType)) return FloatCType;

					if (LeftCType is CSimpleType && RightCType is CSimpleType)
					{
						var LeftSimpleCType = LeftCType as CSimpleType;
						var RightSimpleCType = RightCType as CSimpleType;
						
						var LeftSize = LeftSimpleCType.GetSize(null);
						var RightSize = RightSimpleCType.GetSize(null);
						
						if (LeftSize > RightSize) return LeftSimpleCType;
						if (RightSize > LeftSize) return RightSimpleCType;

						// Same type size but distinct!
						//if (WarningBinaryNoCast)
						if (false)
						{
							Console.Error.WriteLine("BinaryExpression.Type (II) : Left != Right : {0} != {1}", LeftCType, RightCType);
						}

						return LeftSimpleCType;
					}

					if (LeftCType is CPointerType && RightCType is CSimpleType) return LeftCType;
					if (LeftCType is CPointerType && RightCType is CPointerType) return LeftCType;

					Console.Error.WriteLine("BinaryExpression.Type (I) : Left != Right : {0} != {1}", LeftCType, RightCType);
					return LeftCType;
				}
			}
		}

		/// <summary>
		/// Function(Parameters)
		/// </summary>
		/// <example>printf("Hello World %d%d!", 123, 456)</example>
		[Serializable]
		public sealed class FunctionCallExpression : Expression
		{
			public Expression Function { get; private set; }
			public ExpressionCommaList Parameters { get; private set; }

			public FunctionCallExpression(Expression Function, ExpressionCommaList Parameters)
				: base(Function, Parameters)
			{
				this.Function = Function;
				this.Parameters = Parameters;
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				//throw (new InvalidOperationException("A FunctionCallExpression is not a constant value"));
				return null;
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				var CFunctionType = Function.GetCachedCType(Resolver).GetSpecifiedCType<CFunctionType>();
				return (Function.GetCachedCType(Resolver) as CFunctionType).Return;
				//return CFunctionType;
			}
		}

		/// <summary>
		/// ... Expression, Expression ...
		/// </summary>
		/// <example>n = 0, y = 0</example>
		[Serializable]
		public sealed class ExpressionCommaList : Expression
		{
			public Expression[] Expressions { get; private set; }

			public ExpressionCommaList(params Expression[] Expressions)
				: base(Expressions)
			{
				this.Expressions = Expressions;
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
#if true
				throw (new InvalidOperationException("A ExpressionCommaList is not a constant value"));
#else
				return Expressions.Last().GetConstantValue();
#endif
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				return Expressions.Last().GetCachedCType(Resolver);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <example>{ 1, 2, 3, 4, 5 }</example>
		[Serializable]
		public sealed class VectorInitializationExpression : Expression
		{
			public CType CType;
			public Expression[] Expressions { get; private set; }

			public VectorInitializationExpression(CType CType, params Expression[] Expressions)
				: base(Expressions)
			{
				this.CType = CType;
				this.Expressions = Expressions;
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				return CType;
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				throw new NotImplementedException();
			}
		}

		class DummyConstantResolver : IConstantResolver
		{
			static public DummyConstantResolver Default = new DummyConstantResolver();

			public object GetConstantIdentifier(string Name)
			{
				throw new NotImplementedException();
			}
		}

		class DummyIdentifierTypeResolver : IIdentifierTypeResolver
		{
			static public DummyIdentifierTypeResolver Default = new DummyIdentifierTypeResolver();

			public CType ResolveIdentifierType(string Identifier)
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Expression
		/// </summary>
		/// <example>1 + 2 * 3</example>
		[Serializable]
		abstract public class Expression : Node
		{
			public Expression(params Node[] Childs)
				: base(Childs)
			{
			}

			public void CheckLeftValue()
			{
				// TODO:
				//throw (new NotImplementedException());
			}

			private IIdentifierTypeResolver __Cached_IIdentifierTypeResolver = DummyIdentifierTypeResolver.Default;
			private CType __Cached_IIdentifierTypeResolver_CType;

			public CType GetCachedCType(IIdentifierTypeResolver Resolver)
			{
				if (Resolver == null) return null;
				if (__Cached_IIdentifierTypeResolver != Resolver)
				{
					__Cached_IIdentifierTypeResolver = Resolver;
					__Cached_IIdentifierTypeResolver_CType = __GetCType(Resolver);
				}
				return __Cached_IIdentifierTypeResolver_CType;
			}

			private IConstantResolver __Cached_IConstantResolver = DummyConstantResolver.Default;
			private object __Cached_IConstantResolver_ConstantValue;

			public object GetCachedConstantValue(IConstantResolver Resolver)
			{
				if (__Cached_IConstantResolver != Resolver)
				{
					__Cached_IConstantResolver = Resolver;
					__Cached_IConstantResolver_ConstantValue = __GetConstantValue(Resolver);
				}
				return __Cached_IConstantResolver_ConstantValue;
			}

			public TType GetConstantValue<TType>(IConstantResolver IConstantResolver)
			{
				var Value = __GetConstantValue(IConstantResolver);
				if (Value == null)
				{
					Console.Error.WriteLine("Can't cast ConstantValue [{0}] to <{1}>", this.GetType(), typeof(TType));
					return default(TType);
				}
				return (TType)Value;
			}

			abstract protected CType __GetCType(IIdentifierTypeResolver Resolver);
			abstract protected object __GetConstantValue(IConstantResolver IConstantResolver);
		}
	}
}
