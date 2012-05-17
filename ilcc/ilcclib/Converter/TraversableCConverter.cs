﻿using System;
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

		public class TypeContext
		{
			Dictionary<CType, Type> Types = new Dictionary<CType, Type>();

			public void SetTypeByCType(CType CType, Type Type)
			{
				Types.Add(CType, Type);
			}

			public Type GetTypeByCType(CType CType)
			{
				return Types[CType];
			}
		}

		protected TypeContext CustomTypeContext = new TypeContext();

		public CType ConvertTypeToCType(Type Type)
		{
			if (Type.IsPointer) return new CPointerType(ConvertTypeToCType(Type.GetElementType()));
			if (Type.IsArray && Type.GetElementType() == typeof(object)) return new CEllipsisType();

			if (Type == typeof(void)) return new CSimpleType() { BasicType = CTypeBasic.Void };
	
			// Neutral Types
			if (Type == typeof(bool)) return new CSimpleType() { BasicType = CTypeBasic.Bool, Sign = CTypeSign.Signed };
			if (Type == typeof(float)) return new CSimpleType() { BasicType = CTypeBasic.Float };
			if (Type == typeof(double)) return new CSimpleType() { BasicType = CTypeBasic.Double };

			// Signed Types
			if (Type == typeof(sbyte)) return new CSimpleType() { BasicType = CTypeBasic.Char, Sign = CTypeSign.Signed };
			if (Type == typeof(short)) return new CSimpleType() { BasicType = CTypeBasic.Short, Sign = CTypeSign.Signed };
			if (Type == typeof(int)) return new CSimpleType() { BasicType = CTypeBasic.Int, Sign = CTypeSign.Signed };
			if (Type == typeof(long)) return new CSimpleType() { BasicType = CTypeBasic.Int, LongCount = 2, Sign = CTypeSign.Signed };

			// Unsigned Types
			if (Type == typeof(byte)) return new CSimpleType() { BasicType = CTypeBasic.Char, Sign = CTypeSign.Unsigned };
			if (Type == typeof(ushort)) return new CSimpleType() { BasicType = CTypeBasic.Short, Sign = CTypeSign.Unsigned };
			if (Type == typeof(uint)) return new CSimpleType() { BasicType = CTypeBasic.Int, Sign = CTypeSign.Unsigned };
			if (Type == typeof(ulong)) return new CSimpleType() { BasicType = CTypeBasic.Int, LongCount = 2, Sign = CTypeSign.Unsigned };

			Console.Error.WriteLine("ConvertTypeToCType {0}", Type);
			throw (new NotImplementedException("ConvertTypeToCType"));
		}

		protected string ConvertCTypeToTypeString(CType CType)
		{
			if (CType is CPointerType) return ConvertCTypeToTypeString((CType as CPointerType).ElementCType) + "*";
			if (CType is CArrayType)
			{
				var CArrayType = CType as CArrayType;
				return ConvertCTypeToTypeString(CArrayType.ElementCType) + "[" + CArrayType.Size + "]";
			}

			var Type = ConvertCTypeToType(CType);

			if (Type == typeof(void)) return "void";

			if (Type == typeof(sbyte)) return "sbyte";
			if (Type == typeof(byte)) return "byte";

			if (Type == typeof(short)) return "short";
			if (Type == typeof(ushort)) return "ushort";

			if (Type == typeof(int)) return "int";
			if (Type == typeof(uint)) return "uint";

			if (Type == typeof(long)) return "long";
			if (Type == typeof(ulong)) return "ulong";

			//return Type.Name;
			return Type.ToString();
		}

		public Type ConvertCTypeToType(CType CType)
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
			else if (CType is CArrayType)
			{
				Console.Error.WriteLine("ConvertCTypeToType Unimplemented Type '{0}'", CType);
				return ConvertCTypeToType((CType as CArrayType).ElementCType).MakePointerType();
			}
			else if (CType is CStructType)
			{
				return CustomTypeContext.GetTypeByCType((CType as CStructType));
			}
			else
			{
				Console.Error.WriteLine("ConvertCTypeToType Unimplemented Type '{0}'", CType);
				return typeof(int);
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

		public virtual void Initialize()
		{
		}
	}
}
