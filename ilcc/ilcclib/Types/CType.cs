using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Parser;
using System.Runtime.InteropServices;

namespace ilcclib.Types
{
	[Serializable]
	public sealed class CEnumType : CBaseStructType
	{
	}

	[Serializable]
	public sealed class CStructType : CBaseStructType
	{
	}

	[Serializable]
	public class CBaseStructType : CType
	{
		public List<CSymbol> Items { get; private set; }
		private Dictionary<string, CSymbol> ItemsDictionary;
		//public string Name;

		public CSymbol GetFieldByName(string Name)
		{
			return ItemsDictionary[Name];
		}

		public void AddItem(CSymbol CSymbol)
		{
			Items.Add(CSymbol);
			if (CSymbol.Name != null)
			{
				ItemsDictionary.Add(CSymbol.Name, CSymbol);
			}
		}

		public CBaseStructType(/*string Name*/)
		{
			//this.Name = Name;
			this.Items = new List<CSymbol>();
			this.ItemsDictionary = new Dictionary<string, CSymbol>();
		}

		public override string ToString()
		{
			return String.Format("{{ {0} }}", String.Join(", ", Items.Select(Item => Item.ToString())));
		}

		internal override int __InternalGetSize(ISizeProvider Context)
		{
			int MaxItemSize = 4;
			int Offset = 0;
			foreach (var Item in Items)
			{
				int Size = Item.CType.__InternalGetSize(Context);

				MaxItemSize = Math.Max(MaxItemSize, Size);

				while ((Offset % Size) != 0) Offset++;
				Offset += Size;
			}

			while ((Offset % MaxItemSize) != 0) Offset++;

			return Offset;
		}

		public override CSimpleType GetCSimpleType()
		{
			return new CSimpleType()
			{
				BasicType = CTypeBasic.ComplexType,
				ComplexType = this,
			};
		}

		public override IEnumerable<CType> GetChildTypes()
		{
			return this.Items.Select(Item => Item.CType);
		}
	}

	/*
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

		public override CSimpleType GetCSimpleType()
		{
			return CSymbol.Type.GetCSimpleType();
		}
	}
	*/

	[Serializable]
	public sealed class CFunctionType : CType
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

		public override string ToString()
		{
			return String.Format("{0} {1} ({2})", Return, Name, String.Join(", ", Parameters.Select(Item => Item.ToString()))).Trim();
		}

		internal override int __InternalGetSize(ISizeProvider Context)
		{
			return Context.PointerSize;
		}
		
		public override CSimpleType GetCSimpleType()
		{
			return new CSimpleType()
			{
				BasicType = CTypeBasic.ComplexType,
				ComplexType = this,
			};
		}

		public override IEnumerable<CType> GetChildTypes()
		{
			return new[] { Return };
		}
	}

	[Serializable]
	public enum CTypeStorage
	{
		/// <summary>
		/// Storage class: extern
		/// Lifetime: static
		/// Linkage: external (whole program)
		/// </summary>
		Extern,

		/// <summary>
		/// Storage class: static
		///	Lifetime: static
		///	Linkage: internal (translation unit only)
		/// </summary>
		Static,

		/// <summary>
		/// Storage class: auto
		/// Lifetime: function call
		/// Linkage: (none)
		/// </summary>
		Auto,

		/// <summary>
		/// Storage class: register
		/// Lifetime: function call
		/// Linkage: (none)
		/// </summary>
		Register,
	}

	[Serializable]
	public enum CTypeSign
	{
		Signed,
		Unsigned,
	}

	[Serializable]
	public enum CTypeBasic
	{
		Void,
		Char,
		Bool,
		Short,
		Int,
		//Long,
		Float,
		Double,
		ComplexType,
	}

#if false
	[Serializable]
	public sealed class CTypeDef : CType
	{
		public CType CType;

		public CTypeDef(CType CType)
		{
			this.CType = CType;
		}
	}
#endif

	[Serializable]
	public sealed class CNativeType : CType
	{
		public Type Type { get; private set; }

		public CNativeType(Type Type)
		{
			this.Type = Type;
		}

		internal override int __InternalGetSize(ISizeProvider Context)
		{
			// TODO: Fake to get the higher size a pointer would get on x64.
			if (this.Type == null) return 0;
			if (this.Type.IsPointer) return 8;

			return Marshal.SizeOf(this.Type);
		}

		public override IEnumerable<CType> GetChildTypes()
		{
			return new CType[0];
		}

		public override CSimpleType GetCSimpleType()
		{
			return new CSimpleType()
			{
				BasicType = CTypeBasic.ComplexType,
				ComplexType = this,
			};
		}
	}

	[Serializable]
	public sealed class CSimpleType : CType
	{
		public bool IsSet { get; private set; }

		private bool _Typedef = false;
		private bool _Inline = false;
		private bool _Const = false;
		private bool _Volatile = false;
		private CTypeSign _Sign = CTypeSign.Signed;
		private CTypeStorage _Storage = CTypeStorage.Auto;
		private CTypeBasic _BasicType = CTypeBasic.Int;
		private int _LongCount = 0;
		private CType _ComplexType = null;

		/// <summary>
		/// 
		/// </summary>
		public bool Typedef { get { return _Typedef; } set { IsSet = true; _Typedef = value; } }
		
		/// <summary>
		/// 
		/// </summary>
		public bool Inline { get { return _Inline; } set { IsSet = true; _Inline = value; } }
		
		/// <summary>
		/// 
		/// </summary>
		public bool Const { get { return _Const; } set { IsSet = true; _Const = value; } }
		
		/// <summary>
		/// 
		/// </summary>
		public bool Volatile { get { return _Volatile; } set { IsSet = true; _Volatile = value; } }
		
		/// <summary>
		/// 
		/// </summary>
		public CTypeSign Sign { get { return _Sign; } set { IsSet = true; _Sign = value; } }
		
		/// <summary>
		/// 
		/// </summary>
		public CTypeStorage Storage { get { return _Storage; } set { IsSet = true; _Storage = value; } }

		/// <summary>
		/// 
		/// </summary>
		public CTypeBasic BasicType { get { return _BasicType; } set { IsSet = true; _BasicType = value; } }

		/// <summary>
		/// 
		/// </summary>
		public int LongCount { get { return _LongCount; } set { IsSet = true; _LongCount = value; } }

		/// <summary>
		/// If BasicType == StructEnumUnion
		/// </summary>
		public CType ComplexType { get { return _ComplexType; } set { IsSet = true; _ComplexType = value; } }

		public override string ToString()
		{
			var Parts = new List<string>();
			if (Typedef) Parts.Add("typedef");
			if (Const) Parts.Add("const");
			if (Volatile) Parts.Add("volatile");
			if (Sign == CTypeSign.Unsigned) Parts.Add("unsigned");
			if (Storage != CTypeStorage.Auto) Parts.Add(Storage.ToString().ToLower());
			for (int n = 0; n < LongCount; n++) Parts.Add("long");
			if (BasicType != CTypeBasic.ComplexType)
			{
				Parts.Add(BasicType.ToString().ToLower());
			}
			else
			{
				if (ComplexType != null)
				{
					Parts.Add(ComplexType.ToString());
				}
			}
			return String.Join(" ", Parts);
		}

		internal override int __InternalGetSize(ISizeProvider Context)
		{
			switch (BasicType)
			{
				case CTypeBasic.Void: return 0;
				case CTypeBasic.Bool: return 1;
				case CTypeBasic.Char: return 1;
				case CTypeBasic.Short: return 2;
				case CTypeBasic.Int: return (LongCount < 2) ? 4 : 8;
				case CTypeBasic.Float: return 4;
				case CTypeBasic.Double: return 8;
				case CTypeBasic.ComplexType: return ComplexType.GetSize(Context);
				default: throw(new NotImplementedException());
			}
		}

		public override CSimpleType GetCSimpleType()
		{
			return this;
		}

		public override IEnumerable<CType> GetChildTypes()
		{
			if (ComplexType != null)
			{
				return new CType[] { ComplexType };
			}
			else
			{
				return new CType[] { };
			}
		}
	}

	[Serializable]
	public sealed class CEllipsisType : CType
	{
		public override string ToString()
		{
			return "...";
		}

		internal override int __InternalGetSize(ISizeProvider Context)
		{
			throw new NotImplementedException();
		}

		public override CSimpleType GetCSimpleType()
		{
			return new CSimpleType()
			{
				BasicType = CTypeBasic.ComplexType,
				ComplexType = this,
			};
		}

		public override IEnumerable<CType> GetChildTypes()
		{
			return new CType[0];
		}
	}

	[Serializable]
	public sealed class CArrayType : CBasePointerType
	{
		//public int Size { get; private set; }
		public int Size;

		public CArrayType(CType CType, int Size)
			: base (CType, "const")
		{
			this.Size = Size;
		}

		public override string ToString()
		{
			string Output = "";
			Output += (ElementCType != null) ? ElementCType.ToString() : "#ERROR#";
			Output += "[" + Size + "]";
			return Output.TrimEnd();
		}
	}

	[Serializable]
	public sealed class CPointerType : CBasePointerType
	{
		public CPointerType(CType CType, string[] Qualifiers = null)
			: base (CType, Qualifiers)
		{
		}
	}

	[Serializable]
	abstract public class CBasePointerType : CType
	{
		public CType ElementCType { get; private set; }
		public string[] Qualifiers { get; private set; }

		public CBasePointerType(CType CType, params string[] Qualifiers)
		{
			this.ElementCType = CType;
			this.Qualifiers = Qualifiers;
		}

		public override string ToString()
		{
			string Output = "";
			Output += (ElementCType != null) ? ElementCType.ToString() : "#ERROR#";
			Output += " * ";
			if (Qualifiers != null)
			{
				Output += String.Join(" ", Qualifiers.Where(Qualifier => Qualifier != null));
			}
			return Output.TrimEnd();
		}

		public override IEnumerable<CType> GetChildTypes()
		{
			return new[] { ElementCType };
		}

		internal override int __InternalGetSize(ISizeProvider Context)
		{
			if (Context == null) return 4;
			return Context.PointerSize;
		}

		public override CSimpleType GetCSimpleType()
		{
			if (ElementCType == null) return new CSimpleType() { BasicType = CTypeBasic.Void };
			return ElementCType.GetCSimpleType();
		}
	}

	public interface ISizeProvider
	{
		int PointerSize { get; }
	}

	[Serializable]
	abstract public class CType
	{
		abstract internal int __InternalGetSize(ISizeProvider Context);

		public int GetSize(ISizeProvider Context)
		{
			return __InternalGetSize(Context);
		}

		abstract public IEnumerable<CType> GetChildTypes();

		public TType GetSpecifiedCType<TType>() where TType : CType
		{
			if (this is TType) return this as TType;
			foreach (var Item in this.GetChildTypes())
			{
				var Result = Item.GetSpecifiedCType<TType>();
				if (Result != null) return Result;
			}
			return null;
		}

		abstract public CSimpleType GetCSimpleType();
		
		public CStructType GetCStructType()
		{
			return GetSpecifiedCType<CStructType>();
		}

		static public bool ContainsPair(CType Left, CType Right, CType A, CType B)
		{
			if (Left == A && Right == B) return true;
			if (Left == B && Right == A) return true;
			return false;
		}

		static public bool ContainsOne(CType Left, CType Right, CType A)
		{
			if (Left == A) return true;
			if (Right == A) return true;
			return false;
		}

		public override int GetHashCode()
		{
			return this.ToString().GetHashCode();
		}

		static public bool operator !=(CType Left, CType Right)
		{
			return !(Left == Right);
		}

		static public bool operator ==(CType Left, CType Right)
		{
			if (((object)Left) == ((object)Right)) return true;
			if (((object)Left) == null) return (((object)Right) == null);
			if (((object)Right) == null) return (((object)Left) == null);
			return Left.ToString() == Right.ToString();
		}

		internal static CType CommonType(CType A, CType B)
		{
			if (A == B) return A;
			throw new NotImplementedException();
		}
	}
}
