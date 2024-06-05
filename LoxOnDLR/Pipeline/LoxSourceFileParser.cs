using Lox;

namespace LoxOnDLR.Pipeline
{
    internal static class LoxSourceFileParser
    {
        public static bool TryParseFile(string inputValue, out List<SyntaxNode> parse)
        {
            // Read the input file into a string
            string scriptContent = File.ReadAllText(inputValue);

            bool hadError = false;
            // using the Lox Lexer and Parser
            Lexer lexer = new Lexer(scriptContent);
            lexer.ScanTokens();
            var errors = lexer.GetErrors().ToList();
            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    Console.Error.WriteLine($"[{error.Line}] Error{error.Where}: {error.Message}");
                }
                hadError = true;
            }

            Parser parser = new Parser(lexer.GetTokens().ToList());
            parse = parser.Parse();

            //check for parse/lex errors
            errors = parser.GetErrors().ToList();
            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    Console.Error.WriteLine($"[{error.Line}] Error{error.Where}: {error.Message}");
                }
                return false;
            }

            Lox.Resolver resolver = new Lox.Resolver(new Evaluator());
            resolver.Resolve(parse);
            var resolveErrors = resolver.GetErrors().ToList();
            if (resolveErrors.Count > 0)
            {
                foreach (var error in resolveErrors)
                {
                    Console.Error.WriteLine($"[{error.Line}] Error{error.Where}: {error.Message}");
                }
                hadError = true;
            }

            if (!hadError)
            {
                var syntaxNodeParentalLinkageVisitor = new DlrLox.Pipeline.SyntaxNodeParentalLinkageVisitor();
                syntaxNodeParentalLinkageVisitor.LinkUpParents(parse);
                var constructorVisitor = new ClassInitializerArityFinder();
                constructorVisitor.BuildSymbolTable(parse);
            }

            return !hadError;
        }
    }
}