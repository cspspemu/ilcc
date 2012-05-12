using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcc.Runtime;
using System.Reflection;

namespace ilcclib.Ast.Declaration
{
	static public class MethodInfoExtensions
	{
		static public string ToMethodCallString(this MethodInfo MethodInfo)
		{
			return MethodInfo.DeclaringType.Name + "." + MethodInfo.Name;
		}
	}

	unsafe delegate sbyte* StringToSbytePtr(string Text);

	unsafe public class ProgramAstNode : AstNode
	{
		AstNode Child;

		public ProgramAstNode(AstNode Child)
		{
			this.Child = Child;
		}

		public override void GenerateCSharp(AstGenerateContext Context)
		{
			Context.Write("using System;"); Context.NewLine();
			Context.Write("using ilcc.Runtime;"); Context.NewLine();
			Context.NewLine();

			Context.Write("[CModule]"); Context.NewLine();
			Context.Write("unsafe public class CProgram {");
			Context.PushTypeContext(() =>
			{
				Context.Indent(() =>
				{
					Context.SetIdentifier(
						"printf",
						new AstFunctionType(new AstPrimitiveType("void"), new AstPrimitiveType("sbyte").Pointer(), new AstEllipsisType()),
						String.Format("CLib.printf")
					);

					foreach (var Item in Context.StringLiterals)
					{
						Context.Write("static public readonly sbyte* ");
						Context.Write(Item.Key);
						Context.Write(" = " + ((StringToSbytePtr)CLibUtils.GetLiteralStringPointer).Method.ToMethodCallString() + "(");
						Context.Write(Item.Value);
						Context.Write(");");
						Context.NewLine();
						Context.SetIdentifier(Item.Key, new AstPrimitiveType("sbyte").Pointer(), String.Format("CProgram.{0}", Item.Key));
					}
					if (Context.StringLiterals.Count > 0)
					{
						Context.NewLine();
					}

					Context.Write(this.Child);
				});
			});
			Context.Write("}");
		}

		public override void Analyze(AstGenerateContext Context)
		{
			Context.Analyze(Child);
		}

		public override void GenerateIL(AstGenerateContext Context)
		{
			throw new NotImplementedException();
		}
	}
}
