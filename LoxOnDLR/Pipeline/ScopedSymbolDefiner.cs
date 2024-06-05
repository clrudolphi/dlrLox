using Lox;
using LoxOnDLR.Infrastructure;
using LoxOnDLR.Runtime;
using System.Linq.Expressions;

namespace LoxOnDLR.Pipeline
{
    // Purpose of this class is to be called at the start of each scope (class, method, function, block, loop)
    // to identify the classes, methods and functions that will be defined in that scope
    // and to prepare ParameterExpression's for them that will be stored in the scope's SymbolTableScope.
    // This will allow for forward references to be resolved.
    internal class ScopedSymbolDefiner : SyntaxNodeVisitorBase
    {
        private SymbolTableScope Scope;

        public ScopedSymbolDefiner(SymbolTableScope scope)
        {
            Scope = scope;
        }

        public void DefineSymbolsInScope(List<SyntaxNode> nodes)
        {
            // HACK: this short circuits this function to limit its applicability to the Module level scope. We can leave the plumbing in place in case we want to
            //       extend forward visibility in other scopes in the future.
            if (Scope.IsModule)
                nodes.ForEach(x => ((IVisitable)x).Accept(this));
        }

        public override void Visit(FunctionStatement node)
        {
            var p = Expression.Variable(typeof(LoxFunction), node.Name.Lexeme);
            Scope.Names[node.Name.Lexeme] = p; // allow for redefinition
        }

        public override void Visit(ClassStatement node)
        {
            var p = Expression.Variable(typeof(LoxClassConstructorFunction), node.Name.Lexeme);
            Scope.Names[node.Name.Lexeme] = p;
        }

        // The purpose of these overrides to is to prevent the visitor from traversing the children of these nodes
        public override void Visit(WhileStatement node)
        {
        }

        public override void Visit(BlockStatement node)
        {
        }
    }
}