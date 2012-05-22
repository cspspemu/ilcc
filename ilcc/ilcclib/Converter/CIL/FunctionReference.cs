using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Codegen;
using System.Reflection;
using ilcclib.Types;

namespace ilcclib.Converter.CIL
{
	public class FunctionReference
	{
		public string Name { get; private set; }
		public MethodInfo MethodInfo { get { return _MethodInfoLazy.Value; } }
		public SafeMethodTypeInfo SafeMethodTypeInfo { get; private set; }
		public CFunctionType CFunctionType;
		Lazy<MethodInfo> _MethodInfoLazy;
		public bool BodyFinalized;
		public bool HasStartedBody { get { return _MethodInfoLazy.IsValueCreated; } }

		public FunctionReference(CILConverter CILConverter, string Name, Lazy<MethodInfo> MethodInfoLazy, SafeMethodTypeInfo SafeMethodTypeInfo = null)
		{
			Initialize(CILConverter, Name, MethodInfoLazy, SafeMethodTypeInfo);
		}

		public FunctionReference(CILConverter CILConverter, string Name, MethodInfo MethodInfo, SafeMethodTypeInfo SafeMethodTypeInfo = null)
		{
			var MethodInfoLazy = new Lazy<MethodInfo>(() => { return MethodInfo; });
			var MethodInfoCreated = MethodInfoLazy.Value;
			Initialize(CILConverter, Name, MethodInfoLazy, SafeMethodTypeInfo);
		}

		private void Initialize(CILConverter CILConverter, string Name, Lazy<MethodInfo> MethodInfoLazy, SafeMethodTypeInfo SafeMethodTypeInfo = null)
		{
			this.Name = Name;
			this._MethodInfoLazy = MethodInfoLazy;
			this.SafeMethodTypeInfo = SafeMethodTypeInfo;

			Type ReturnType;
			Type[] ParametersType;

			BodyFinalized = MethodInfoLazy.IsValueCreated;

			if (SafeMethodTypeInfo != null)
			{
				ReturnType = SafeMethodTypeInfo.ReturnType;
				ParametersType = SafeMethodTypeInfo.Parameters;
			}
			else
			{
				ReturnType = this.MethodInfo.ReturnType;
				ParametersType = this.MethodInfo.GetParameters().Select(Item => Item.ParameterType).ToArray();
			}

			var ReturnCType = CILConverter.ConvertTypeToCType(ReturnType);
			var ParametersCType = new List<CType>();

			foreach (var ParameterType in ParametersType)
			{
				ParametersCType.Add(CILConverter.ConvertTypeToCType(ParameterType));
			}

			this.CFunctionType = new CFunctionType(
				ReturnCType,
				Name,
				ParametersCType.Select(Item => new CSymbol() { CType = Item }).ToArray()
			);
		}
	}
}
