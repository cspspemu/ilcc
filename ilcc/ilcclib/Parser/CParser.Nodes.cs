//#define USE_PROTO_BUF
//#define COMPRESS_SERIALIZATION

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ilcclib.Types;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.IO.Compression;
#if USE_PROTO_BUF
using ProtoBuf;
#endif

namespace ilcclib.Parser
{
	public partial class CParser
	{
		[Serializable]
		public class ParserNodeExpressionList : Node
		{
			public ParserNodeExpressionList()
				: base()
			{
			}
		}

		public interface IIdentifierTypeResolver
		{
			CType ResolveIdentifierType(string Identifier);
		}

		[Serializable]
		public sealed class FunctionDeclaration : Declaration
		{
			public CFunctionType CFunctionType { get; private set; }
			public Statement FunctionBody { get; private set; }

			public FunctionDeclaration(CFunctionType CFunctionType, Statement FunctionBody)
				: base(FunctionBody)
			{
				this.CFunctionType = CFunctionType;
				this.FunctionBody = FunctionBody;
			}

			protected override string GetParameter()
			{
				return String.Format("{0}", CFunctionType);
			}
		}

		[Serializable]
		public sealed class VariableDeclaration : Declaration
		{
			public CSymbol Symbol { get; private set; }
			public Expression InitialValue { get; private set; }

			public VariableDeclaration(CSymbol Symbol, Expression InitialValue)
				: base(InitialValue)
			{
				this.Symbol = Symbol;
				this.InitialValue = InitialValue;
			}

			protected override string GetParameter()
			{
				return Symbol.ToString();
			}
		}

		[Serializable]
		public sealed class TypeDeclaration : Declaration
		{
			public CSymbol Symbol { get; private set; }

			public TypeDeclaration(CSymbol Symbol)
				: base()
			{
				this.Symbol = Symbol;
			}

			protected override string GetParameter()
			{
				return Symbol.ToString();
			}
		}

		[Serializable]
		public sealed class DeclarationList : Declaration
		{
			public Declaration[] Declarations { get; private set; }

			public DeclarationList(params Declaration[] Childs)
				: base(Childs)
			{
				this.Declarations = Childs;
			}
		}

		[Serializable]
		abstract public class Declaration : Statement
		{
			public Declaration(params Node[] Childs)
				: base(Childs)
			{
			}
		}

		[Serializable]
		public sealed class SpecialIdentifierExpression : LiteralExpression
		{
			/// <summary>
			/// __func__
			/// </summary>
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
				return new CPointerType(new CSimpleType() { BasicType = CTypeBasic.Char });
			}
		}

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

		[Serializable]
		abstract public class LiteralExpression : Expression
		{
			public LiteralExpression()
				: base()
			{
			}
		}

		[Serializable]
		public sealed class TrinaryExpression : Expression
		{
			public Expression Condition { get; private set; }
			public Expression TrueCond { get; private set; }
			public Expression FalseCond { get; private set; }

			public TrinaryExpression(Expression Left, Expression TrueCond, Expression FalseCond)
				: base(Left, TrueCond, FalseCond)
			{
				this.Condition = Left;
				this.TrueCond = TrueCond;
				this.FalseCond = FalseCond;
			}

			public override object GetConstantValue()
			{
				throw new NotImplementedException();
			}

			public override CType GetCType(IIdentifierTypeResolver Resolver)
			{
				var TrueType = TrueCond.GetCType(Resolver);
				var FalseType = FalseCond.GetCType(Resolver);
				if (TrueType == FalseType)
				{
					return TrueType;
				}
				else
				{
					throw(new NotImplementedException());
				}
			}
		}

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

		[Serializable]
		public enum OperatorPosition
		{
			Left,
			Right
		}

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

		[Serializable]
		public sealed class FieldAccessExpression : Expression
		{
			public Expression LeftExpression { get; private set; }
			public string FieldName { get; private set; }

			public FieldAccessExpression(Expression Left, string FieldName)
				: base(Left)
			{
				this.LeftExpression = Left;
				this.FieldName = FieldName;
			}

			public override object GetConstantValue()
			{
				throw (new InvalidOperationException("A FieldAccessExpression is not a constant value"));
			}

			public override CType GetCType(IIdentifierTypeResolver Resolver)
			{
				return (LeftExpression.GetCType(Resolver) as CStructType).ItemsDictionary[FieldName].Type;
			}
		}

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
				return (Left.GetCType(Resolver) as CBasePointerType).GetCSimpleType();
			}
		}

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
						throw(new NotImplementedException(String.Format("Not implemented constant binary operator '{0}'", Operator)));
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
					if (LeftCType == DoubleCType || RightCType == DoubleCType) return DoubleCType;

					throw (new NotImplementedException(String.Format("BinaryExpression.Type : Left != Right : {0} != {1}", LeftCType, RightCType)));
				}
			}
		}

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

		[Serializable]
		//[ProtoContract]
		public sealed class TranslationUnit : Statement
		{
			//[ProtoMember(1)]
			public Declaration[] Declarations { get; private set; }

			public TranslationUnit(params Declaration[] Declarations)
				: base(Declarations)
			{
				this.Declarations = Declarations;
			}

			public void Serialize(Stream Stream)
			{
#if COMPRESS_SERIALIZATION
				using (Stream = new DeflateStream(Stream, CompressionMode.Compress, leaveOpen: true))
#endif
				{
#if USE_PROTO_BUF
					ProtoBuf.Serializer.Serialize(Stream, this);
#else
					var BinaryFormatter = new BinaryFormatter();
					BinaryFormatter.Serialize(Stream, this);
				}
#endif
			}
		}

		[Serializable]
		public sealed class CompoundStatement : Statement
		{
			public Statement[] Statements { get; private set; }

			public CompoundStatement(params Statement[] Statements)
				: base(Statements)
			{
				this.Statements = Statements;
			}
		}

		[Serializable]
		public sealed class LabelStatement : Statement
		{
			public IdentifierExpression IdentifierExpression { get; private set; }

			public LabelStatement(IdentifierExpression IdentifierExpression)
				: base(IdentifierExpression)
			{
				this.IdentifierExpression = IdentifierExpression;
			}
		}

		[Serializable]
		public sealed class ExpressionStatement : Statement
		{
			public Expression Expression { get; private set; }

			public ExpressionStatement(Expression Expression)
				: base(Expression)
			{
				this.Expression = Expression;
			}
		}

		[Serializable]
		public sealed class ReturnStatement : Statement
		{
			public Expression Expression { get; private set; }

			public ReturnStatement(Expression Expression)
				: base (Expression)
			{
				this.Expression = Expression;
			}
		}

		[Serializable]
		public sealed class ForStatement : Statement
		{
			public ExpressionStatement Init { get; private set; }
			public Expression Condition { get; private set; }
			public ExpressionStatement PostOperation { get; private set; }
			public Statement LoopStatements { get; private set; }

			public ForStatement(ExpressionStatement Init, Expression Condition, ExpressionStatement PostOperation, Statement LoopStatements)
				: base(Init, Condition, PostOperation, LoopStatements)
			{
				this.Init = Init;
				this.Condition = Condition;
				this.PostOperation = PostOperation;
				this.LoopStatements = LoopStatements;
			}
		}

		[Serializable]
		public sealed class ContinueStatement : Statement
		{
			public ContinueStatement()
				: base()
			{
			}
		}

		[Serializable]
		public sealed class BreakStatement : Statement
		{
			public BreakStatement()
				: base()
			{
			}
		}

		[Serializable]
		public sealed class SwitchDefaultStatement : Statement
		{
			public SwitchDefaultStatement()
				: base()
			{
			}
		}

		[Serializable]
		public sealed class GotoStatement : Statement
		{
			public string LabelName { get; private set; }

			public GotoStatement(string LabelName)
				: base()
			{
				this.LabelName = LabelName;
			}
		}

		[Serializable]
		public sealed class SwitchCaseStatement : Statement
		{
			public Expression Value { get; private set; }

			public SwitchCaseStatement(Expression Value)
				: base(Value)
			{
				this.Value = Value;
			}
		}

		[Serializable]
		public sealed class DoWhileStatement : BaseWhileStatement
		{
			public DoWhileStatement(Expression Condition, Statement Statements)
				: base(Condition, Statements)
			{
			}
		}

		[Serializable]
		public sealed class WhileStatement : BaseWhileStatement
		{
			public WhileStatement(Expression Condition, Statement Statements)
				: base(Condition, Statements)
			{
			}
		}

		[Serializable]
		abstract public class BaseWhileStatement : Statement
		{
			public Expression Condition { get; private set; }
			public Statement LoopStatements { get; private set; }

			public BaseWhileStatement(Expression Condition, Statement Statements)
				: base(Condition, Statements)
			{
				this.Condition = Condition;
				this.LoopStatements = Statements;
			}
		}

		[Serializable]
		public sealed class SwitchStatement : Statement
		{
			public Expression ReferenceExpression { get; private set; }
			public CompoundStatement Statements { get; private set; }

			public SwitchStatement(Expression ReferenceExpression, CompoundStatement Statements)
				: base(ReferenceExpression, Statements)
			{
				this.ReferenceExpression = ReferenceExpression;
				this.Statements = Statements;
			}
		}

		[Serializable]
		public sealed class IfElseStatement : Statement
		{
			public Expression Condition { get; private set; }
			public Statement TrueStatement { get; private set; }
			public Statement FalseStatement { get; private set; }

			public IfElseStatement(Expression Condition, Statement TrueStatement, Statement FalseStatement)
				: base(Condition, TrueStatement, FalseStatement)
			{
				this.Condition = Condition;
				this.TrueStatement = TrueStatement;
				this.FalseStatement = FalseStatement;
			}
		}

		[Serializable]
		abstract public class Statement : Node
		{
			public Statement(params Node[] Childs)
				: base(Childs)
			{
			}
		}

		[Serializable]
		public class Node
		{
			public object Tag;
			//public Node[] NodeChilds { get; private set; }
			private Node[] NodeChilds;

			/*
			public Node(params Node[] Nodes)
			{
				this.Nodes = Nodes;
			}
			*/

			public Node(params Node[] Childs)
			{
				this.NodeChilds = Childs.Where(Child => Child != null).ToArray();
			}

			protected virtual string GetParameter()
			{
				return "";
			}

			public XElement AsXml()
			{
				return new XElement(
					GetType().Name,
					new XAttribute("value", GetParameter()),
					NodeChilds.Select(Item => Item.AsXml())
				);
			}

			public IEnumerable<string> ToYamlLines(int Indent = 0)
			{
				//const string Separator = "|   ";
				const string Separator = "   ";

				var Indentation = String.Concat(Enumerable.Repeat(Separator, Indent));
				var Name = GetType().Name;
				var Parameters = GetParameter();

				yield return String.Format("{0}- {1}: {2}", Indentation, Name, Parameters).TrimEnd();

				for (int n = 0; n < NodeChilds.Length; n++)
				{
					var Child = NodeChilds[n];
					if (Child == null) throw(new Exception("Child can't be null"));
					foreach (var Line in Child.ToYamlLines(Indent + 1))
					{
						yield return Line.TrimEnd();
					}
				}
			}

			public string ToYaml()
			{
				return String.Join("\r\n", this.ToYamlLines());
			}
		}
	}
}
