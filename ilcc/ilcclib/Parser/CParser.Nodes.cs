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

		public sealed class DeclarationList : Declaration
		{
			public Declaration[] Declarations { get; private set; }

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
			public string Identifier;

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
		}

		public class StringExpression : LiteralExpression
		{
			public string String;

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

		public class CastExpression : Expression
		{
			private CType CastType;
			private Expression Right;

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
		}

		public enum OperatorPosition
		{
			Left,
			Right
		}

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
					default:
						throw (new NotImplementedException(String.Format("Not implemented constant unary operator '{0}'", Operator)));
				}
			}
		}

		public class FieldAccessExpression : Expression
		{
			Expression Left;
			string FieldName;

			public FieldAccessExpression(Expression Left, string FieldName)
				: base(Left)
			{
				this.Left = Left;
				this.FieldName = FieldName;
			}

			public override object GetConstantValue()
			{
				throw (new InvalidOperationException("A FieldAccessExpression is not a constant value"));
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
				throw (new InvalidOperationException("An ArrayAccessExpression is not a constant value"));
			}
		}

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
		}

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
		}

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

		public sealed class Program : Statement
		{
			public Declaration[] Declarations { get; private set; }

			public Program(params Declaration[] Declarations)
				: base(Declarations)
			{
				this.Declarations = Declarations;
			}
		}

		public class CompoundStatement : Statement
		{
			public Statement[] Statements { get; private set; }

			public CompoundStatement(params Statement[] Statements)
				: base(Statements)
			{
				this.Statements = Statements;
			}
		}

		public class LabelStatement : Statement
		{
			Expression Expression;

			public LabelStatement(Expression Expression)
				: base(Expression)
			{
				this.Expression = Expression;
			}
		}

		public sealed class ExpressionStatement : Statement
		{
			public Expression Expression { get; private set; }

			public ExpressionStatement(Expression Expression)
				: base(Expression)
			{
				this.Expression = Expression;
			}
		}

		public sealed class ReturnStatement : Statement
		{
			public Expression Expression { get; private set; }

			public ReturnStatement(Expression Expression)
				: base (Expression)
			{
				this.Expression = Expression;
			}
		}

		public sealed class ForStatement : Statement
		{
			public Expression Init { get; private set; }
			public Expression Condition { get; private set; }
			public Expression PostOperation { get; private set; }
			public Statement LoopStatements { get; private set; }

			public ForStatement(Expression Init, Expression Condition, Expression PostOperation, Statement LoopStatements)
				: base(Init, Condition, PostOperation, LoopStatements)
			{
				this.Init = Init;
				this.Condition = Condition;
				this.PostOperation = PostOperation;
				this.LoopStatements = LoopStatements;
			}
		}

		public sealed class ContinueStatement : Statement
		{
			public ContinueStatement()
				: base()
			{
			}
		}

		public sealed class BreakStatement : Statement
		{
			public BreakStatement()
				: base()
			{
			}
		}

		public sealed class SwitchDefaultStatement : Statement
		{
			public SwitchDefaultStatement()
				: base()
			{
			}
		}

		public sealed class GotoStatement : Statement
		{
			string LabelName;

			public GotoStatement(string LabelName)
				: base()
			{
				this.LabelName = LabelName;
			}
		}

		public sealed class SwitchCaseStatement : Statement
		{
			public Expression Value { get; private set; }

			public SwitchCaseStatement(Expression Value)
				: base(Value)
			{
				this.Value = Value;
			}
		}

		public sealed class DoWhileStatement : BaseWhileStatement
		{
			public DoWhileStatement(Expression Condition, Statement Statements)
				: base(Condition, Statements)
			{
			}
		}

		public sealed class WhileStatement : BaseWhileStatement
		{
			public WhileStatement(Expression Condition, Statement Statements)
				: base(Condition, Statements)
			{
			}
		}

		abstract public class BaseWhileStatement : Statement
		{
			public Expression Condition { get; private set; }
			public Statement Statements { get; private set; }

			public BaseWhileStatement(Expression Condition, Statement Statements)
				: base(Condition, Statements)
			{
				this.Condition = Condition;
				this.Statements = Statements;
			}
		}

		public sealed class SwitchStatement : Statement
		{
			public Expression ReferenceExpression { get; private set; }
			public Statement Statements { get; private set; }

			public SwitchStatement(Expression ReferenceExpression, Statement Statements)
				: base(ReferenceExpression, Statements)
			{
				this.ReferenceExpression = ReferenceExpression;
				this.Statements = Statements;
			}
		}

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

		abstract public class Statement : Node
		{
			public Statement(params Node[] Childs)
				: base(Childs)
			{
			}
		}

		public class Node
		{
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
