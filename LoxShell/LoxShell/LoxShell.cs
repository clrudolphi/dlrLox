using System.CommandLine;
using System.Runtime.CompilerServices;
using System.Text;
using LoxOnDLR;

namespace DlrLox
{
    internal class LoxShell
    {
        static int Main(string[] args)
        {
            int exitCode = 0;
            var rootCommand = new RootCommand();

            var input = new Argument<FileInfo>(
                "input",
                description: "Lox script file path and name"
                );
            rootCommand.AddArgument(input);

            rootCommand.SetHandler((inputValue) =>
            {                
                exitCode = CompileLox(inputValue!);
            },
            input);
            rootCommand.Invoke(args);
            return exitCode;
        }

        private static int CompileLox(FileInfo inputValue)
        {
            Console.OutputEncoding = Encoding.ASCII;
            var sp = new LoxOnDLRScriptProcessor();
            return sp.Process(inputValue);
        }
    }
}