using Lox;

namespace LoxOnDLR.Infrastructure
{
    internal abstract class SyntaxNodeVisitorBase : ISyntaxNodeVisitor
    {
        public virtual void Visit(BlockStatement node)
        {
            foreach (var statement in node.Statements) { ((IVisitable)statement).Accept(this); }
        }

        public virtual void Visit(ExpressionStatement node)
        {
            ((IVisitable)node.Expression).Accept(this);
        }

        public virtual void Visit(PrintStatement node)
        {
            ((IVisitable)node.Expression).Accept(this);
        }

        public virtual void Visit(ReturnStatement node)
        {
            if (node.Value != null) ((IVisitable)node.Value).Accept(this);
        }

        public virtual void Visit(VariableDeclarationStatement node)
        {
            if (node.Initializer != null) ((IVisitable)node.Initializer).Accept(this);
        }

        public virtual void Visit(IfStatement node)
        {
            ((IVisitable)node.Condition).Accept(this);
            ((IVisitable)node.ThenBranch).Accept(this);
            if (node.ElseBranch != null) ((IVisitable)node.ElseBranch).Accept(this);
        }

        public virtual void Visit(WhileStatement node)
        {
            ((IVisitable)node.Condition).Accept(this);
            ((IVisitable)node.Body).Accept(this);
        }

        public virtual void Visit(FunctionStatement node)
        {
            foreach (var statement in node.Body) { ((IVisitable)statement).Accept(this); }
        }

        public virtual void Visit(ClassStatement node)
        {
            foreach (var method in node.Methods) { ((IVisitable)method).Accept(this); }
        }

        public virtual void Visit(BinaryExpression node)
        {
            ((IVisitable)node.Left).Accept(this);
            ((IVisitable)node.Right).Accept(this);
        }

        public virtual void Visit(UnaryExpression node)
        {
            ((IVisitable)node.Right).Accept(this);
        }

        public virtual void Visit(VariableExpression node)
        {
        }

        public virtual void Visit(GroupingExpression node)
        {
            ((IVisitable)node.Expression).Accept(this);
        }

        public virtual void Visit(AssignmentExpression node)
        {
            ((IVisitable)node.Value).Accept(this);
        }

        public virtual void Visit(LiteralExpression node)
        {
        }

        public virtual void Visit(CallExpression node)
        {
            ((IVisitable)node.Callee).Accept(this);
            foreach (var arg in node.Arguments) { ((IVisitable)arg).Accept(this); }
        }

        public virtual void Visit(SetExpression node)
        {
            ((IVisitable)node.Object).Accept(this);
            ((IVisitable)node.Value).Accept(this);
        }

        public virtual void Visit(GetExpression node)
        {
            ((IVisitable)node.Object).Accept(this);
        }

        public virtual void Visit(ThisExpression node)
        {
        }

        public virtual void Visit(SuperExpression node)
        {
        }
    }
}