<h1 >
   DlrLox
  <br>
  
#  An implementation of [Lox](https://github.com/munificent/craftinginterpreters) on the .NET Dynamic Runtime
</h1>

## The parser for this implementation is borrowed from [Lox.NET](https://github.com/FaberSanZ/Lox.NET)
<br>
Some of the code for the Lox dynamic binders was borrowed from/inspired by the [Sympl](https://github.com/IronLanguages/dlr/tree/master/Samples/sympl) language sample provided with the DLR.
For this implementation of Lox, interoperability with .NET types and other DLR languages was a non-goal. As a result, the binding code is simpler and less fully featured than found in other DLR-based languages.

No time has been spent on performance tuning. This implementation runs about an order of magnitude slower than clox.

This implementation was built as a learning exercise.

Future Goals:
* Performance improvements
* Code Editor support (syntax highlighting, LSP integration to VS Code)
* Debugger
* Experiment with replacing the RD parser with a Pratt parser
* Experiment with alternative backend implementations

[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)

<hr>
<br>
