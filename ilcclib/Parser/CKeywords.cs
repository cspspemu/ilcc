﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ilcclib.Parser
{
	public static class CKeywords
	{
		static public readonly HashSet<string> BultinTypeKeywords = new HashSet<string>(new string[]
		{
			"int",
			"void",
			"char",
			"extern",
			"static",
			"unsigned",
			"const",
			"__const",
			"__const__",
			"volatile",
			"__volatile",
			"__volatile__",
			"long",
			"register",
			"signed",
			"__signed",
			"__signed__",
			"auto",
			"inline",
			"__inline",
			"__inline__",
			"restrict",
			"__restrict",
			"__restrict__",
			"__extension__",
			"float",
			"double",
			"_Bool",
			"short",
			"struct",
			"union",
			"typedef",
			"enum",
			"__attribute",
			"__attribute__",
		});

		static public readonly HashSet<string> Keywords = new HashSet<string>(BultinTypeKeywords.Concat(new string[]
		{
			"if",
			"else",
			"while",
			"break",
			"return",
			"for",
			"goto",
			"do",
			"continue",
			"switch",
			"case",
			"default",
			"sizeof",
			"__alignof",
			"__alignof__",
			"typeof",
			"__typeof",
			"__typeof__",
			"__label__",
			"asm",
			"__asm",
			"__asm__",
		}));
	}
}
