using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Globalization;
using System.Reflection;
using System.Diagnostics;
using ilcc.Runtime.C;

namespace ilcc.Runtime
{
	static internal class TextReaderExtensions
	{
		static public bool HasMore(this TextReader TextReader)
		{
			return TextReader.Peek() >= 0;
		}

		static public char ReadChar(this TextReader TextReader)
		{
			return (char)TextReader.Read();
		}
	}

	[CModule]
	unsafe public class CLibUtils
	{
		static public Encoding DefaultEncoding = Encoding.GetEncoding(1252);

		static public IntPtr ConvertToIntPtr(object Object)
		{
			if (Object is IntPtr) return (IntPtr)Object;
			if (Object is UIntPtr) return new IntPtr(((UIntPtr)Object).ToPointer());
			if (Object is int) return new IntPtr((int)Object);
			if (Object is long) return new IntPtr((long)Object);
			throw(new Exception(String.Format("Can't cast {0} into {1}", Object.GetType(), typeof(IntPtr))));
		}

		/*
		static public TType Cast<TType>(object Value)
		{
			if (typeof(TType) == typeof(int)) return (TType)(dynamic)Convert.ToInt32(Value);
			if (typeof(TType) == typeof(uint)) return (TType)(dynamic)Convert.ToUInt32(Value);

			if (Value.GetType() == typeof(int)) return (TType)(dynamic)(int)Value;
			if (Value.GetType() == typeof(uint)) return (TType)(dynamic)(uint)Value;
			return (TType)(dynamic)Value;
		}
		*/

		static public object[] GetObjectsFromArgsIterator(ArgIterator ArgIterator)
		{
			var Params = new object[ArgIterator.GetRemainingCount()];
			for (int n = 0; n < Params.Length; n++)
			{
				Params[n] = TypedReference.ToObject(ArgIterator.GetNextArg());
			}
			ArgIterator.End();
			return Params;
		}

		static private CultureInfo NeutralCultureInfo = new CultureInfo("en-US");

		static public string sprintf_hl(string Format, params object[] Params)
		{
			var Out = "";
			var FormatReader = new StringReader(Format);
			var ParamsQueue = new Queue<object>(Params);
			while (FormatReader.HasMore())
			{
				var Char = FormatReader.ReadChar();
				switch (Char)
				{
					case '%':
						{
							int LongCount = 0;
							string LeftString = "";
							string DecimalString = "";
							int PaddingDirection = +1;
							bool ReadingDecimalDigits = false;
							string NumberOfIntegerDigitsString = "";
							string NumberOfDecimalDigitsString = "";
							char PaddingChar = ' ';
							while (FormatReader.HasMore())
							{
								Char = FormatReader.ReadChar();
								if (IsNumber(Char))
								{
									if (ReadingDecimalDigits)
									{
										NumberOfDecimalDigitsString += Char;
									}
									else
									{
										if (NumberOfIntegerDigitsString.Length == 0 && Char == '0')
										{
											PaddingChar = Char;
										}
										else
										{
											NumberOfIntegerDigitsString += Char;
										}
									}

								}
								else if (Char == '-')
								{
									PaddingDirection = -1;
								}
								else if (Char == '.')
								{
									ReadingDecimalDigits = true;
								}
								else if (Char == 'l')
								{
									LongCount++;
								}
								else if (IsAlpha(Char))
								{
									switch (Char)
									{
										case 'c':
											{
												LeftString = "" + (char)(int)ParamsQueue.Dequeue();
											}
											goto EndFormat;
										case 'x':
										case 'X':
											{
												LeftString = Convert.ToString(Convert.ToInt64(ParamsQueue.Dequeue()), 16);
												if (Char == 'X')
												{
													LeftString = LeftString.ToUpperInvariant();
												}
												else if (Char == 'x')
												{
													LeftString = LeftString.ToLowerInvariant();
												}
											}
											goto EndFormat;
										case 'u':
											{
												LeftString = Convert.ToString(Convert.ToUInt32(ParamsQueue.Dequeue()), NeutralCultureInfo);
											}
											goto EndFormat;
										case 'd':
											{
												LeftString = Convert.ToString(Convert.ToInt32(ParamsQueue.Dequeue()), NeutralCultureInfo);
											}
											goto EndFormat;
										case 's':
											{
												LeftString = GetStringFromPointer((sbyte *)ConvertToIntPtr(ParamsQueue.Dequeue()).ToPointer());
											}
											goto EndFormat;
										case 'f':
											var Parts = Convert.ToString(ParamsQueue.Dequeue(), NeutralCultureInfo).Split('.');
											LeftString = Parts[0];
											if (Parts.Length > 1)
											{
												DecimalString = Parts[1];
											}
											goto EndFormat;
										default:
											throw (new NotImplementedException(String.Format("Unknown '{0}' in '{1}'", Char, Format)));
									}
								}
								else
								{
									PaddingChar = Char;
								}
							}
						EndFormat: ;

							{
								var NumberOfIntegerDigits = (NumberOfIntegerDigitsString.Length > 0) ? int.Parse(NumberOfIntegerDigitsString) : 0;
								var NumberOfDecimalDigits = (NumberOfDecimalDigitsString.Length > 0) ? int.Parse(NumberOfDecimalDigitsString) : 0;

								Out += LeftString;
								if (NumberOfDecimalDigits > 0)
								{
									if (DecimalString.Length > NumberOfDecimalDigits) DecimalString = DecimalString.Substring(0, NumberOfDecimalDigits);
									Out += ".";
									Out += DecimalString.PadRight(NumberOfDecimalDigits, '0');
								}
								else if (DecimalString.Length > 0)
								{
									Out += ".";
									Out += DecimalString;
								}
								else
								{
									if (PaddingDirection < 0)
									{
										Out = Out.PadRight(NumberOfIntegerDigits, PaddingChar);
									}
									else if (PaddingDirection > 0)
									{
										Out = Out.PadLeft(NumberOfIntegerDigits, PaddingChar);
									}
								}
							}
						}
						break;
					default:
						Out += Char;
						break;
				}
			}
			return Out;
		}

		static public bool IsNumber(char Char)
		{
			if (Char >= '0' && Char <= '9') return true;
			return false;
		}

		static public bool IsSpace(char Char)
		{
			if (Char == ' ') return true;
			if (Char == '\t') return true;
			if (Char == '\n') return true;
			if (Char == '\r') return true;
			return false;
		}

		static public bool IsAlpha(char Char)
		{
			if (Char >= 'a' && Char <= 'z') return true;
			if (Char >= 'A' && Char <= 'Z') return true;
			//if (Char == '_') return true;
			return false;
		}

		/*
		static public void* TypedReferenceToPointer(TypedReference TypedReference)
		{
			return ((UIntPtr)TypedReference.ToObject(TypedReference)).ToPointer();
		}

		static public string TypedReferenceToString(TypedReference TypedReference)
		{
			return Marshal.PtrToStringAnsi(new IntPtr(TypedReferenceToPointer(TypedReference)));
		}
		*/

		static public sbyte* GetLiteralStringPointer(string Text)
		{
			var Bytes = DefaultEncoding.GetBytes(Text);
			var Pointer = (sbyte*)Marshal.AllocHGlobal(Bytes.Length + 1).ToPointer();
			Marshal.Copy(Bytes, 0, new IntPtr(Pointer), Bytes.Length);
			Pointer[Bytes.Length] = 0;
			return Pointer;
		}

		static public string GetStringFromPointer(IntPtr Pointer)
		{
			//if (Pointer.ToInt64() < 10000) return "#INVALID#";
			//RoutingAddress.IsValidAddress
			return Marshal.PtrToStringAnsi(Pointer);
		}

		static public string GetStringFromPointer(UIntPtr Pointer)
		{
			return GetStringFromPointer(new IntPtr(Pointer.ToPointer()));
		}

		static public byte[] GetBytesFromPointer(sbyte* Pointer, int Count)
		{
			var Data = new byte[Count];
			Marshal.Copy(new IntPtr(Pointer), Data, 0, Count);
			return Data;
		}

		static public void PutBytesToPointer(sbyte* Pointer, int Count, byte[] Data)
		{
			Marshal.Copy(Data, 0, new IntPtr(Pointer), Count);
		}

		public delegate string PointerToStringDelegate(sbyte* Pointer);
		public delegate sbyte* StringToPointerDelegate(string Pointer);

		static public string GetStringFromPointer(sbyte* Pointer)
		{
			return GetStringFromPointer(new IntPtr(Pointer));
		}

		static public string GetStringFromPointerWide(char* Pointer)
		{
			return Marshal.PtrToStringUni(new IntPtr(Pointer));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Type"></param>
		/// <param name="Args"></param>
		static public int RunTypeMain(Type Type, string[] Args)
		{
			try
			{
				//Marshal.PrelinkAll(Type);

				Args = new string[] { Process.GetCurrentProcess().MainModule.FileName }.Concat(Args).ToArray();

				var MainMethod = Type.GetMethod("main");
				var MainParameters = MainMethod.GetParameters();
				object Result = null;
				if (MainParameters.Length == 0)
				{
					Result = MainMethod.Invoke(null, new object[] { });
				}
				else if (MainParameters.Length == 2)
				{
					if (MainParameters[0].ParameterType == typeof(int) && MainParameters[1].ParameterType == typeof(sbyte**))
					{
						var ArgArray = new sbyte*[Args.Length];
						fixed (sbyte** ArgArrayPointer = ArgArray)
						{
							for (int n = 0; n < ArgArray.Length; n++) ArgArrayPointer[n] = GetLiteralStringPointer(Args[n]);

							CInternals._argc = Args.Length;
							CInternals._argv = ArgArrayPointer;

							Result = MainMethod.Invoke(null, new object[] { Args.Length, (IntPtr)ArgArrayPointer });
						}
					}
					else
					{
						throw (new InvalidProgramException("Invalid 'main' signature : wrong parameters"));
					}
				}
				else
				{
					throw (new InvalidProgramException(String.Format("Invalid number of 'main' parameters expected [0 or 2] and have {0}", MainParameters.Length)));
				}

				if (Result == null)
				{
					return -1;
				}
				else if (MainMethod.ReturnType == typeof(void))
				{
					return 0;
				}
				else if (MainMethod.ReturnType == typeof(int))
				{
					return (int)Result;
				}
				else
				{
					throw (new InvalidProgramException("Function 'main' signature should return int or void"));
				}
			}
			catch (Exception Exception)
			{
				Console.Error.WriteLine(Exception);
				return -1;
			}
		}

		/*
		public static IntPtr MethodInfoToPointer(MethodInfo MethodInfo)
		{
			return GCHandle.ToIntPtr(GCHandle.Alloc(MethodInfo, GCHandleType.Normal));
		}

		public static MethodInfo PointerToMethodInfo(IntPtr Pointer)
		{
			return (MethodInfo)GCHandle.FromIntPtr(Pointer).Target;
		}

		public static MethodInfo PointerToMethodInfo(Type DelegateType, IntPtr Pointer)
		{
			
			//Delegate.CreateDelegate(DelegateType, 
			return (MethodInfo)GCHandle.FromIntPtr(Pointer).Target;
		}
		*/
	}
}
