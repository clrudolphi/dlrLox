using Lox;
using LoxOnDLR.Infrastructure;

namespace LoxOnDLR.Pipeline
{
    internal class ClassConstructorArityTag : IMetaTag
    {
        public int Arity { get; set; }
    }

    internal class ClassInitializerArityFinder : SyntaxNodeVisitorBase
    {
        private record ClassSymbolTableEntry(string Name, string SuperClass, int ConstructorArity);
        private Dictionary<string, ClassSymbolTableEntry> _symbolTable = new Dictionary<string, ClassSymbolTableEntry>();

        public void BuildSymbolTable(List<SyntaxNode> nodes)
        {
            nodes.ForEach(x => ((IVisitable)x).Accept(this));
        }

        public override void Visit(ClassStatement node)
        {
            var cname = node.Name.Lexeme;
            var super = node.SuperClass?.Name.Lexeme;

            // If a superclass is specified, we look it up in the symbol table. If it exists there, start with that Arity. If no superclass is specified, start with 0.
            // If the superclass is specified, but not found in the symbol table, start with 0. (The runtime error will be caught later.)
            int arity = string.IsNullOrEmpty(super) ? 0 : _symbolTable.Keys.Contains(super) ? _symbolTable[super].ConstructorArity : 0;

            foreach (var method in node.Methods)
            {
                if (method.Name.Lexeme == "init")
                {
                    arity = method.Parameters.Count;
                    break;
                }
            }

            _symbolTable.Add(cname, new ClassSymbolTableEntry(cname, super, arity));

            node.Tags.Add(new ClassConstructorArityTag() { Arity = arity });
        }
    }
}