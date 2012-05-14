using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ilcclib.Types;

namespace ilcclib.Parser
{
	public partial class CParser
	{
		public class ParserNodeExpressionList : Node
		{
			public ParserNodeExpressionList()
				: base()
			{
			}
		}

		public class FunctionDeclaration : Declaration
		{
			private CFunctionType CFunctionType;
			private Statement FunctionBody;

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

		public class VariableDeclaration : Declaration
		{
			CSymbol Symbol;
			Expression InitialValue;

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

		public class TypeDeclaration : Declaration
		{
			CSymbol Symbol;

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

		public class DeclarationList : Declaration
		{
			Declaration[] Declarations;

			public DeclarationList(params Declaration[] Childs)
				: base(Childs)
			{
				this.Declarations = Childs;
			}
		}

		abstract public class Declaration : Statement
		{
			public Declaration(params Node[] Childs)
				: base(Childs)
			{
			}
		}

		public class SpecialIdentifierExpression : LiteralExpression
		{
			public string Value;

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
		}

		public class IdentifierExpression : LiteralExpression
		{
			public string Value;

			public IdentifierExpression(string Value)
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
				throw (new InvalidOperationException("A IdentifierExpression is not a constant value"));
			}
		}

		public class StringExpression : LiteralExpression
		{
			public string Value;

			public StringExpression(string Value)
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
		}

		public class IntegerExpression : LiteralExpression
		{
			public int Value;

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
		}

		abstract public class LiteralExpression : Expression
		{
			public LiteralExpression()
				: base()
			{
			}
		}

		public class TrinaryExpression : Expression
		{
			Expression Condition;
			Expression TrueCond;
			Expression FalseCond;

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
		}

		public enum OperatorPosition
		{
			Left,
			Right
		}

		public class UnaryExpression : Expression
		{
			string Operator;
			Expression Right;
			OperatorPosition OperatorPosition;

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
					default:
						throw (new NotImplementedException(String.Format("Not implemented constant unary operator '{0}'", Operator)));
				}
			}
		}

		public class ArrayAccessExpression : Expression
		{
			Expression Left;
			Expression Index;

			public ArrayAccessExpression(Expression Left, Expression Index)
				: base(Left, Index)
			{
				this.Left = Left;
				this.Index = Index;
			}

			public override object GetConstantValue()
			{
				throw (new InvalidOperationException("A ArrayAccessExpression is not a constant value"));
			}
		}

		public class BinaryExpression : Expression
		{
			Expression Left;
			string Operator;
			Expression Right;

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
					default:
						throw(new NotImplementedException(String.Format("Not implemented constant binary operator '{0}'", Operator)));
				}
			}
		}

		public class FunctionCallExpression : Expression
		{
			Expression Function;
			ExpressionCommaList Parameters;
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
		}

		public class ExpressionCommaList : Expression
		{
			Expression[] Expressions;

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
		}

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

			abstract public object GetConstantValue();
			public TType GetConstantValue<TType>()
			{
				return (TType)GetConstantValue();
			}
		}

		public class CompoundStatement : Statement
		{
			public CompoundStatement(params Statement[] Childs)
				: base(Childs)
			{
			}
		}

		public class ExpressionStatement : Statement
		{
			Expression Expression;

			public ExpressionStatement(Expression Expression)
				: base(Expression)
			{
				this.Expression = Expression;
			}
		}

		public class IfElseStatement : Statement
		{
			Expression Condition;
			Statement TrueStatement;
			Statement FalseStatement;

			public IfElseStatement(Expression Condition, Statement TrueStatement, Statement FalseStatement)
				: base(Condition, TrueStatement, FalseStatement)
			{
				this.Condition = Condition;
				this.TrueStatement = TrueStatement;
				this.FalseStatement = FalseStatement;
			}
		}

		abstract public class Statement : Node
		{
			public Statement(params Node[] Childs)
				: base(Childs)
			{
			}
		}

		public class Node
		{
			Node[] Childs;

			/*
			public Node(params Node[] Nodes)
			{
				this.Nodes = Nodes;
			}
			*/

			public Node(params Node[] Childs)
			{
				this.Childs = Childs.Where(Child => Child != null).ToArray();
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
					Childs.Select(Item => Item.AsXml())
				);
			}

			public IEnumerable<string> ToYamlLines(int Indent = 0)
			{
				//const string Separator = "|   ";
				const string Separator = "   ";
				yield return String.Format("{0}- {1}: {2}", String.Concat(Enumerable.Repeat(Separator, Indent)), GetType().Name, GetParameter()).TrimEnd();
				for (int n = 0; n < Childs.Length; n++)
				{
					var Child = Childs[n];
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
