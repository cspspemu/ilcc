using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ilcclib.Parser;
using System.Diagnostics;
using System.Reflection;

namespace ilcclib.Converter
{
	public class CNodeTraverserAttribute : Attribute
	{
	}
	public class CNodeTraverser
	{
		public Dictionary<Type, MethodInfo> Map = new Dictionary<Type, MethodInfo>();
		object TargetObject;

		private void AddMap<TType>(Action<TType> Callback) where TType : CParser.Node
		{
			AddMap(typeof(TType), Callback.Method);
		}

		private void AddMap(Type Type, MethodInfo Callback)
		{
			Map.Add(Type, Callback);
		}

		[DebuggerHidden]
		public void Traverse(params CParser.Node[] Nodes)
		{
			foreach (var Node in Nodes) Traverse(Node);
		}

		[DebuggerHidden]
		public void Traverse(CParser.Node Node)
		{
			var NodeType = Node.GetType();
			if (Map.ContainsKey(NodeType))
			{
				Map[NodeType].Invoke(TargetObject, new object[] { Node });
			}
			else
			{
				throw (new NotImplementedException(String.Format("Not implemented {0}", Node.GetType())));
			}
		}

		public void AddClassMap(object Object)
		{
			this.TargetObject = Object;
			var Type = Object.GetType();
			foreach (var Method in Type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
			{
				if (Method.GetCustomAttributes(typeof(CNodeTraverserAttribute), true).Length == 0) continue;
				if (Method.ReturnParameter.ParameterType != typeof(void)) continue;
				if (Method.GetParameters().Length != 1) continue;
				var ParameterType = Method.GetParameters()[0].ParameterType;
				if (!typeof(CParser.Node).IsAssignableFrom(ParameterType)) continue;
				//Console.WriteLine(Method);
				AddMap(ParameterType, Method);
			}
		}
	}
}
