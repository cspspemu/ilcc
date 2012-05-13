using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Codegen;
using System.Reflection.Emit;
using System.Reflection;
using ilcc.Runtime;
using System.IO;

namespace ilcclib.Ast
{
	public partial class AstGenerateContext
	{
		AssemblyBuilder AssemblyBuilder;
		public ModuleBuilder ModuleBuilder { get; private set; }
		TypeBuilder MainClassTypeBuilder;

		public void GenerateIL(params AstNode[] AstNodes)
		{
			foreach (var AstNode in AstNodes) AstNode.GenerateIL(this);
		}

		public void BuildProgram(Action Action)
		{
			var DllPath = @".";
			var DllName = @"a.dll";

			AssemblyBuilder = SafeAssemblyUtils.CreateAssemblyBuilder("TestAssembly", DllPath);
			ModuleBuilder = AssemblyBuilder.CreateModuleBuilder(DllName);

			MainClassTypeBuilder = ModuleBuilder.DefineType(
				"CProgram",
				TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AutoClass
			);

			/*
			MainClassTypeBuilder.DefineField("Test", typeof(int), FieldAttributes.Public | FieldAttributes.Static);

			var MethodBuilder = MainClassTypeBuilder.DefineMethod("test", MethodAttributes.Static | MethodAttributes.Public);
			var ILGenerator = MethodBuilder.GetILGenerator();
			ILGenerator.Emit(OpCodes.Ret);

			{
				ConstructorBuilder pointCtor = MainClassTypeBuilder.DefineConstructor(
					MethodAttributes.Public,
					CallingConventions.Standard,
					new System.Type[] {}
				);
				var IL = pointCtor.GetILGenerator();
				IL.Emit(OpCodes.Ret);
			}
			*/

			MainClassTypeBuilder.AddCustomAttribute<CModuleAttribute>();

			try
			{
				Action();
			}
#if false
			catch (Exception Exception)
			{
				Console.Error.WriteLine(Exception);
			}
#endif
			finally
			{
			}

			/*
			Console.WriteLine(MainClassTypeBuilder.IsCreated());

			*/

			//ModuleBuilder.CreateGlobalFunctions();

			MainClassTypeBuilder.CreateType();

			AssemblyBuilder.Save(DllName);

			//SafeAssemblyBuilder.AssemblyBuilder.


			//Console.WriteLine(CProgram.Assembly.Location);

			//TypeBuilder.SetCustomAttribute(
		}
	}
}
