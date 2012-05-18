using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Types;

namespace ilcclib.Parser
{
	public partial class CParser
	{
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

			public override object GetConstantValue()
			{
				throw new NotImplementedException();
			}

			public override CType GetCType(IIdentifierTypeResolver Resolver)
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

			public override object GetConstantValue()
			{
				throw (new InvalidOperationException("A IdentifierExpression is not a constant value"));
			}

			public override CType GetCType(IIdentifierTypeResolver Resolver)
			{
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

			public override object GetConstantValue()
			{
				return String;
			}

			public override CType GetCType(IIdentifierTypeResolver Resolver)
			{
				return new CPointerType(new CSimpleType() { BasicType = CTypeBasic.Char });
			}
		}

		/// <summary>
		/// Expression that calculates the size of a type.
		/// </summary>
		/// <example>sizeof(int)</example>
		[Serializable]
		public sealed class SizeofExpression : Expression
		{
			public CSimpleType CSimpleType { get; private set; }

			public SizeofExpression(CSimpleType CSimpleType)
				: base()
			{
				this.CSimpleType = CSimpleType;
			}

			public override CType GetCType(IIdentifierTypeResolver Resolver)
			{
				return new CSimpleType() { BasicType = CTypeBasic.Int };
			}

			protected override string GetParameter()
			{
				return String.Format("{0}", CSimpleType);
			}

			public override object GetConstantValue()
			{
				//return CSimpleType.GetSize();
				throw new NotImplementedException();
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

			public override object GetConstantValue()
			{
				return Value;
			}

			public override CType GetCType(IIdentifierTypeResolver Resolver)
			{
				return new CSimpleType() { BasicType = CTypeBasic.Float };
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

			public override object GetConstantValue()
			{
				return Value;
			}

			public override CType GetCType(IIdentifierTypeResolver Resolver)
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

			public override object GetConstantValue()
			{
				throw new NotImplementedException();
			}

			public override CType GetCType(IIdentifierTypeResolver Resolver)
			{
				var TrueType = TrueExpression.GetCType(Resolver);
				var FalseType = FalseExpression.GetCType(Resolver);
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

			public override object GetConstantValue()
			{
				throw new NotImplementedException();
			}

			public override CType GetCType(IIdentifierTypeResolver Resolver)
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

			public override object GetConstantValue()
			{
				var RightValue = Right.GetConstantValue();

				switch (Operator)
				{
					case "-": return -((dynamic)RightValue);
					default:
						throw (new NotImplementedException(String.Format("Not implemented constant unary operator '{0}'", Operator)));
				}
			}

			public override CType GetCType(IIdentifierTypeResolver Resolver)
			{
				return Right.GetCType(Resolver);
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

			public override object GetConstantValue()
			{
				throw (new InvalidOperationException("A FieldAccessExpression is not a constant value"));
			}

			public override CType GetCType(IIdentifierTypeResolver Resolver)
			{
				var LeftCType = LeftExpression.GetCType(Resolver);
				var CStructType = LeftCType.GetCStructType();
				return CStructType.GetFieldByName(FieldName).Type;
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

			public override object GetConstantValue()
			{
				throw (new InvalidOperationException("An ArrayAccessExpression is not a constant value"));
			}

			public override CType GetCType(IIdentifierTypeResolver Resolver)
			{
				//return (Left.GetCType(Resolver) as CBasePointerType).GetCSimpleType();
				return (Left.GetCType(Resolver) as CBasePointerType).GetChildTypes().First();
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

			public override object GetConstantValue()
			{
				var LeftValue = Left.GetConstantValue();
				var RightValue = Right.GetConstantValue();
				switch (Operator)
				{
					case "+": return (object)((dynamic)LeftValue + (dynamic)RightValue);
					case "-": return (object)((dynamic)LeftValue - (dynamic)RightValue);
					case "*": return (object)((dynamic)LeftValue * (dynamic)RightValue);
					default:
						throw (new NotImplementedException(String.Format("Not implemented constant binary operator '{0}'", Operator)));
				}
			}

			public override CType GetCType(IIdentifierTypeResolver Resolver)
			{
				var LeftCType = Left.GetCType(Resolver);
				var RightCType = Right.GetCType(Resolver);

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
						Console.Error.WriteLine("BinaryExpression.Type (II) : Left != Right : {0} != {1}", LeftCType, RightCType);
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

			public override object GetConstantValue()
			{
				throw (new InvalidOperationException("A FunctionCallExpression is not a constant value"));
			}

			public override CType GetCType(IIdentifierTypeResolver Resolver)
			{
				return (Function.GetCType(Resolver) as CFunctionType).Return;
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

			public override object GetConstantValue()
			{
#if true
				throw (new InvalidOperationException("A ExpressionCommaList is not a constant value"));
#else
				return Expressions.Last().GetConstantValue();
#endif
			}

			public override CType GetCType(IIdentifierTypeResolver Resolver)
			{
				return Expressions.Last().GetCType(Resolver);
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

			abstract public CType GetCType(IIdentifierTypeResolver Resolver);

			abstract public object GetConstantValue();
			public TType GetConstantValue<TType>()
			{
				return (TType)GetConstantValue();
			}
		}
	}
}
