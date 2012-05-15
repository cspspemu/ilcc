ilcc
====

C compiler that generates .NET IL made in C#.

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

## License:

This project ir completely free and released under [GPLv2](http://www.gnu.org/licenses/gpl-2.0.html).
But you should consider donating some money if you are going to take profit of this project indirectly.
It takes lot of time to develop things like this. You can donate with the link below.

## Donations:

[Donate via PayPal](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=RB867JB5N5R9S)
