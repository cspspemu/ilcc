using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Parser;

namespace ilcclib.Types
{
	public enum CBasicTypeType
	{
		Void,
		Char,
		Short,
		Int,
		Unsigned,
		Signed,
		Long,
		Bool,
		Float,
		Double,
		Const,
		Volatile,
		Extern,
		Static,
		Typedef,
		Inline,
	}

	public class CEnumType : CBaseStructType
	{
	}

	public class CStructType : CBaseStructType
	{
	}

	public class CBaseStructType : CType
	{
		protected List<CSymbol> Items { get; private set; }
		protected Dictionary<string, CSymbol> ItemsDictionary { get; private set; }

		public void AddItem(CSymbol CSymbol)
		{
			Items.Add(CSymbol);
			if (CSymbol.Name != null)
			{
				ItemsDictionary.Add(CSymbol.Name, CSymbol);
			}
		}

		public CBaseStructType()
		{
			Items = new List<CSymbol>();
			ItemsDictionary = new Dictionary<string, CSymbol>();
		}

		public override string ToString()
		{
			return String.Format("{{ {0} }}", String.Join(", ", Items.Select(Item => Item.ToString())));
		}

		internal override int __InternalGetSize(CParser.Context Context)
		{
			int MaxItemSize = 4;
			int Offset = 0;
			foreach (var Item in Items)
			{
				int Size = Item.Type.__InternalGetSize(Context);

				MaxItemSize = Math.Max(MaxItemSize, Size);

				while ((Offset % Size) != 0) Offset++;
				Offset += Size;
			}

			while ((Offset % MaxItemSize) != 0) Offset++;

			return Offset;
		}
	}

	public class CTypedefType : CType
	{
		CSymbol CSymbol;

		public CTypedefType(CSymbol CSymbol)
		{
			this.CSymbol = CSymbol;
		}

		public override string ToString()
		{
			return String.Format("{0}", CSymbol.Name);
		}

		internal override int __InternalGetSize(CParser.Context Context)
		{
			return CSymbol.Type.__InternalGetSize(Context);
		}
	}

	public class CFunctionType : CType
	{
		public CType Return { get; private set; }
		public string Name;
		public CSymbol[] Parameters { get; private set; }

		public CFunctionType(CType Return, string Name, CSymbol[] Parameters)
		{
			this.Return = Return;
			this.Name = Name;
			this.Parameters = Parameters;
		}

		public override bool HasAttribute(CBasicTypeType Attribute)
		{
			if (Return == null) return false;
			return Return.HasAttribute(Attribute);
		}

		public override string ToString()
		{
			return String.Format("{0} {1} ({2})", Return, Name, String.Join(", ", Parameters.Select(Item => Item.ToString()))).Trim();
		}

		internal override int __InternalGetSize(CParser.Context Context)
		{
			return Context.Config.PointerSize;
		}
	}

	public class CCompoundType : CType
	{
		CType[] Types;

		public CCompoundType(params CType[] Types)
		{
			this.Types = Types;
		}

		public override bool HasAttribute(CBasicTypeType Attribute)
		{
			return Types.Any(Item => Item != null && Item.HasAttribute(Attribute));
		}

		public override string ToString()
		{
			if (Types == null) return "";
			return String.Join(" ", Types.Select(Type => (Type != null) ? Type.ToString() : ""));
		}

		internal override int __InternalGetSize(CParser.Context Context)
		{
			int? Size = null;

			foreach (var CType in Types)
			{
				if (CType is CBasicType)
				{
					var BasicType = CType as CBasicType;
					switch (BasicType.CBasicTypeType)
					{
						case CBasicTypeType.Double:
							if (Size != null) throw (new Exception("Too many basic types"));
							Size = Context.Config.DoubleSize;
							break;
						case CBasicTypeType.Float:
							if (Size != null) throw (new Exception("Too many basic types"));
							Size = Context.Config.FloatSize;
							break;
						case CBasicTypeType.Int:
							if (Size != null) throw (new Exception("Too many basic types"));
							var LongCount = Types.Where(Item => Item is CBasicType).Cast<CBasicType>().Count(Item => Item.CBasicTypeType == CBasicTypeType.Long);
							if (LongCount > 3) throw (new Exception("Too many long"));

							if (LongCount == 2) Size = Context.Config.LongLongSize;
							else if (LongCount == 1) Size = Context.Config.LongSize;
							else if (LongCount == 0) Size = Context.Config.IntSize;
							break;
						case CBasicTypeType.Short:
							if (Size != null) throw (new Exception("Too many basic types"));
							Size = Context.Config.ShortSize;
							break;
						case CBasicTypeType.Char:
							if (Size != null) throw (new Exception("Too many basic types"));
							Size = Context.Config.CharSize;
							break;
						case CBasicTypeType.Bool:
							if (Size != null) throw (new Exception("Too many basic types"));
							Size = Context.Config.BoolSize;
							break;
					}
				}
				else
				{
					//Console.WriteLine("aaaaaaaaaaaaaa : {0}", CType.GetType());
					Size = CType.__InternalGetSize(Context);
				}
			}

			return (Size.HasValue) ? Size.Value : 4;
		}
	}

	public class CEllipsisType : CType
	{
		public override string ToString()
		{
			return "...";
		}

		internal override int __InternalGetSize(CParser.Context Context)
		{
			throw new NotImplementedException();
		}
	}

	public class CBasicType : CType
	{
		public CBasicTypeType CBasicTypeType { get; private set; }

		public CBasicType(CBasicTypeType CBasicTypeType)
		{
			this.CBasicTypeType = CBasicTypeType;
		}

		public override bool HasAttribute(CBasicTypeType Attribute)
		{
			return this.CBasicTypeType == Attribute;
		}

		public override string ToString()
		{
			return CBasicTypeType.ToString().ToLowerInvariant();
		}

		internal override int __InternalGetSize(CParser.Context Context)
		{
			throw new NotImplementedException();
		}
	}

	public class CArrayType : CType
	{
		CType CType;
		int Size;

		public CArrayType(CType CType, int Size)
		{
			this.CType = CType;
			this.Size = Size;
		}

		public override bool HasAttribute(CBasicTypeType Attribute)
		{
			if (CType == null) return false;
			return CType.HasAttribute(Attribute);
		}

		public override string ToString()
		{
			if (CType == null) return "$unknown[" + Size + "]";
			return CType.ToString() + "[" + Size + "]";
		}

		internal override int __InternalGetSize(CParser.Context Context)
		{
			return CType.GetSize(Context) * Size;
		}
	}

	public class CPointerType : CType
	{
		CType CType;
		string[] Qualifiers;

		public CPointerType(CType CType, string[] Qualifiers = null)
		{
			this.CType = CType;
			this.Qualifiers = Qualifiers;
		}

		public override bool HasAttribute(CBasicTypeType Attribute)
		{
			if (CType == null) return false;
			return CType.HasAttribute(Attribute);
		}

		public override string ToString()
		{
			string Output = "";
			Output += (CType != null) ? CType.ToString() : "#ERROR#";
			Output += " * ";
			if (Qualifiers != null)
			{
				Output += String.Join(" ", Qualifiers.Where(Qualifier => Qualifier != null));
			}
			return Output.TrimEnd();
		}

		internal override int __InternalGetSize(CParser.Context Context)
		{
			return Context.Config.PointerSize;
		}
	}

	abstract public class CType
	{
		public virtual bool HasAttribute(CBasicTypeType Attribute)
		{
			return false;
		}

		abstract internal int __InternalGetSize(CParser.Context Context);

		public int GetSize(CParser.Context Context)
		{
			return __InternalGetSize(Context);
		}
	}
}
