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
		public interface IIdentifierTypeResolver
		{
			CType ResolveIdentifierType(string Identifier);
		}

		[Serializable]
		public sealed class FunctionDeclaration : Declaration
		{
			public CFunctionType CFunctionType { get; private set; }
			public CompoundStatement FunctionBody { get; private set; }

			public FunctionDeclaration(PositionInfo PositionInfo, CFunctionType CFunctionType, CompoundStatement FunctionBody)
				: base(PositionInfo, FunctionBody)
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
		public sealed class EmptyDeclaration : Declaration
		{
			public EmptyDeclaration(PositionInfo PositionInfo)
				: base(PositionInfo)
			{
			}
		}

		[Serializable]
		public sealed class StatementsWithExpression : Expression
		{
			public Expression Expression { get; private set; }
			public Statement Statement { get; private set; }

			public StatementsWithExpression(Statement Statement, Expression Expression)
				: base(Statement, Expression)
			{
				this.Statement = Statement;
				this.Expression = Expression;
			}

			protected override CType __GetCType(IIdentifierTypeResolver Resolver)
			{
				return Expression.GetCachedCType(Resolver);
			}

			protected override object __GetConstantValue(IConstantResolver IConstantResolver)
			{
				return Expression.GetCachedConstantValue(IConstantResolver);
			}
		}

		[Serializable]
		public sealed class VariableDeclaration : Declaration
		{
			public CSymbol Symbol { get; private set; }
			public Expression InitialValue { get; private set; }

			public VariableDeclaration(PositionInfo PositionInfo, CSymbol Symbol, Expression InitialValue)
				: base(PositionInfo, InitialValue)
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

			public TypeDeclaration(PositionInfo PositionInfo, CSymbol Symbol)
				: base(PositionInfo)
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

			public DeclarationList(PositionInfo PositionInfo, params Declaration[] Childs)
				: base(PositionInfo, Childs)
			{
				this.Declarations = Childs;
			}
		}

		[Serializable]
		abstract public class Declaration : Statement
		{
			public Declaration(PositionInfo PositionInfo, params Node[] Childs)
				: base(PositionInfo, Childs)
			{
			}
		}

		[Serializable]
		//[ProtoContract]
		public sealed class TranslationUnit : Statement
		{
			//[ProtoMember(1)]
			public Declaration[] Declarations { get; private set; }

			public TranslationUnit(PositionInfo PositionInfo, params Declaration[] Declarations)
				: base(PositionInfo, Declarations)
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

			public CompoundStatement(PositionInfo PositionInfo, params Statement[] Statements)
				: base(PositionInfo, Statements)
			{
				this.Statements = Statements;
			}
		}

		[Serializable]
		public sealed class LabelStatement : Statement
		{
			public IdentifierExpression IdentifierExpression { get; private set; }

			public LabelStatement(PositionInfo PositionInfo, IdentifierExpression IdentifierExpression)
				: base(PositionInfo, IdentifierExpression)
			{
				this.IdentifierExpression = IdentifierExpression;
			}
		}

		[Serializable]
		public sealed class ExpressionStatement : Statement
		{
			public Expression Expression { get; private set; }

			public ExpressionStatement(PositionInfo PositionInfo, Expression Expression)
				: base(PositionInfo, Expression)
			{
				this.Expression = Expression;
			}
		}

		[Serializable]
		public sealed class ReturnStatement : Statement
		{
			public Expression Expression { get; private set; }

			public ReturnStatement(PositionInfo PositionInfo, Expression Expression)
				: base(PositionInfo, Expression)
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

			public ForStatement(PositionInfo PositionInfo, ExpressionStatement Init, Expression Condition, ExpressionStatement PostOperation, Statement LoopStatements)
				: base(PositionInfo, Init, Condition, PostOperation, LoopStatements)
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
			public ContinueStatement(PositionInfo PositionInfo)
				: base(PositionInfo)
			{
			}
		}

		[Serializable]
		public sealed class BreakStatement : Statement
		{
			public BreakStatement(PositionInfo PositionInfo)
				: base(PositionInfo)
			{
			}
		}

		[Serializable]
		public sealed class SwitchDefaultStatement : Statement
		{
			public SwitchDefaultStatement(PositionInfo PositionInfo)
				: base(PositionInfo)
			{
			}
		}

		[Serializable]
		public sealed class GotoStatement : Statement
		{
			public string LabelName { get; private set; }

			public GotoStatement(PositionInfo PositionInfo, string LabelName)
				: base(PositionInfo)
			{
				this.LabelName = LabelName;
			}
		}

		[Serializable]
		public sealed class SwitchCaseStatement : Statement
		{
			public Expression Value { get; private set; }

			public SwitchCaseStatement(PositionInfo PositionInfo, Expression Value)
				: base(PositionInfo, Value)
			{
				this.Value = Value;
			}
		}

		[Serializable]
		public sealed class DoWhileStatement : BaseWhileStatement
		{
			public DoWhileStatement(PositionInfo PositionInfo, Expression Condition, Statement Statements)
				: base(PositionInfo, Condition, Statements)
			{
			}
		}

		[Serializable]
		public sealed class WhileStatement : BaseWhileStatement
		{
			public WhileStatement(PositionInfo PositionInfo, Expression Condition, Statement Statements)
				: base(PositionInfo, Condition, Statements)
			{
			}
		}

		[Serializable]
		abstract public class BaseWhileStatement : Statement
		{
			public Expression Condition { get; private set; }
			public Statement LoopStatements { get; private set; }

			public BaseWhileStatement(PositionInfo PositionInfo, Expression Condition, Statement Statements)
				: base(PositionInfo, Condition, Statements)
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

			public SwitchStatement(PositionInfo PositionInfo, Expression ReferenceExpression, CompoundStatement Statements)
				: base(PositionInfo, ReferenceExpression, Statements)
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

			public IfElseStatement(PositionInfo PositionInfo, Expression Condition, Statement TrueStatement, Statement FalseStatement)
				: base(PositionInfo, Condition, TrueStatement, FalseStatement)
			{
				this.Condition = Condition;
				this.TrueStatement = TrueStatement;
				this.FalseStatement = FalseStatement;
			}
		}

		public class PositionInfo
		{
			public string File;
			public int LineStart;
			public int ColumnStart;
			public int ColumnEnd;
			public int LineEnd;

			public override string ToString()
			{
				return String.Format("{0}:{1}", File, LineStart);
			}
		}

		[Serializable]
		abstract public class Statement : Node
		{
			public PositionInfo PositionInfo { get; private set; }

			public Statement(PositionInfo PositionInfo, params Node[] Childs)
				: base(Childs)
			{
				this.PositionInfo = PositionInfo;
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
				//this.NodeChilds = Childs.Where(Child => Child != null).ToArray();
				this.NodeChilds = Childs;
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

				var Indentation0 = String.Concat(Enumerable.Repeat(Separator, Indent));
				var Indentation1 = Indentation0 + Separator;
				var Name = GetType().Name;
				var Parameters = GetParameter();

				yield return String.Format("{0}- {1}: {2}", Indentation0, Name, Parameters).TrimEnd();

				for (int n = 0; n < NodeChilds.Length; n++)
				{
					var Child = NodeChilds[n];
					//if (Child == null) throw(new Exception("Child can't be null"));
					if (Child == null)
					{
						yield return String.Format("{0}- (null)", Indentation1);
					}
					else
					{
						foreach (var Line in Child.ToYamlLines(Indent + 1))
						{
							yield return Line.TrimEnd();
						}
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
