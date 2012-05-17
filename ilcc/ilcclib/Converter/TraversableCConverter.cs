using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using ilcclib.Parser;
using ilcclib.Compiler;
using ilcclib.Types;

namespace ilcclib.Converter
{
	public abstract class TraversableCConverter : ICConverter
	{
		private CNodeTraverser __CNodeTraverser = new CNodeTraverser();
		protected CCompiler CCompiler { get; private set; }

		static protected Type ConvertCTypeToType(CType CType)
		{
			if (CType is CSimpleType)
			{
				var CSimpleType = CType as CSimpleType;
				switch (CSimpleType.BasicType)
				{
					case CTypeBasic.Void: return typeof(void);
					case CTypeBasic.Bool: return typeof(byte);
					case CTypeBasic.Char: return (CSimpleType.Sign == CTypeSign.Signed) ? typeof(sbyte) : typeof(byte);
					case CTypeBasic.Short: return (CSimpleType.Sign == CTypeSign.Signed) ? typeof(short) : typeof(ushort);
					case CTypeBasic.Int:
						if (CSimpleType.LongCount >= 2)
						{
							return (CSimpleType.Sign == CTypeSign.Signed) ? typeof(long) : typeof(ulong);
						}
						else
						{
							return (CSimpleType.Sign == CTypeSign.Signed) ? typeof(int) : typeof(uint);
						}
					case CTypeBasic.Float: return typeof(float);
					case CTypeBasic.Double: return typeof(double);
					case CTypeBasic.ComplexType: return ConvertCTypeToType(CSimpleType.ComplexType);
					default: throw(new NotImplementedException());
				}
			}
			else if (CType is CPointerType)
			{
				return ConvertCTypeToType((CType as CPointerType).ElementCType).MakePointerType();
			}
			else
			{
				Console.Error.WriteLine("ConvertCTypeToType Unimplemented Type '{0}'", CType);
				return typeof(void);
			}
		}


		public TraversableCConverter()
		{
			__CNodeTraverser.AddClassMap(this);
		}

		[DebuggerHidden]
		protected void Traverse(params CParser.Node[] Nodes)
		{
			__CNodeTraverser.Traverse(Nodes);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="CCompiler"></param>
		/// <param name="Program"></param>
		void ICConverter.ConvertTranslationUnit(CCompiler CCompiler, CParser.TranslationUnit Program)
		{
			this.CCompiler = CCompiler;
			Traverse(Program);
		}
	}
}
