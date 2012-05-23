ilcc
====

C compiler that generates .NET IL made in C#.

## Mirrors:

https://ilcc.codeplex.com/
https://github.com/soywiz/ilcc

## Current Status:

This project has several parts:

* C Preprocessor
* C Parser
* C Codegen
* Command Line Tool

At this point the C Preprocessor is almost complete.
It supports:  #include<>, #include"", #if, #ifdef, #ifndef, #elif, #else, #endif, #error, #define, #undef, macro constants, macro expressions, stringify, concatenation, variadic macros, macro expansion... 
And still misses some common #pragma

C Parser has lot of work done. Still misses some stuff and requires some refactoring work.

IL Codegen on this branch is not implemented. I implemented some of it at the Irony branch.
Even when you cannot generate code yet, you will be able to convert parser nodes into YAML or XML.

The command line tool allows you to preprocess files and to transform the parser trees into YAML oR XML at this point.

## Command Line Help

	ilcc - 0.2 - Carlos Ballesteros Velasco - soywiz (C) 2012
	A C compiler that generates .NET CIL code, XML, YAML and .NET PInvoke

	Switches:
	 -c                     (compile only and generates an intermediate object file)
	 --preprocess, -E       (just preprocesses)
	 --show_macros          (show defined macros after the preprocessing)
	 --target=XXX, -t       (output target) (default target is 'cil')
	 --include_path=XXX, -I (include path/zip file for preprocessor)
	 --define=D=V, -D       (define a constant for the preprocessor)
	 --output=XXX, -o       (file that will hold the output)
	 -run                   (allows to run the program directly, this switch should be the last one and all parameters after it will be passed to the program)

	Available Targets:
	  yaml - Outputs YAML markup
	  pinvoke - Outputs .NET pinvoke source with function declarations and structures (not fully implemented yet)
	  cil - Outputs .NET IL code (not fully implemented yet)
	  xml - Outputs YAML XML

	Help:
	  --show_targets
	  --help -h -?
	  --version -v

## How to report bugs

This project is developed using TDD. So in order to fix stuff you should provide small C snipets that
reproduce the problem. If you have a large C file, try to identify the main problem first and then try
to reduce the C file to avoid including files if required (if it is not a preprocessor bug).

In order to identify preprocessor bugs, you should call the compiler with the -E switch to generate the
preprocessor output.

For parser bugs you can check the internal AST representation using the -t=yaml and see if the representation
is correct.

For runtime bugs you can comment parts of your code and display variable values. Also now it generates .PDB files
with debug information so if you run the C program into visual studio you will be able to see where it is failing.

When you know how to reproduce the problem in a small snipet of C code or for preprocessor with a small amount of
small included files, you can create an issue on github and put in there your snippet and commenting the problem:

https://github.com/soywiz/ilcc/issues
	
## License:

This project ir completely free and released under [GPLv2](http://www.gnu.org/licenses/gpl-2.0.html).
But you should consider donating some money if you are going to take profit of this project indirectly.
It takes lot of time to develop things like this. You can donate with the link below.

## Donations:

[Donate via PayPal](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=RB867JB5N5R9S)
