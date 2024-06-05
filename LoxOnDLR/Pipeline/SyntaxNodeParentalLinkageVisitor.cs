using Lox;

namespace DlrLox.Pipeline
{
    internal class SyntaxNodeParentalLinkageVisitor : ISyntaxNodeVisitor
    {
        public void LinkUpParents(List<SyntaxNode> nodes)
        {
            foreach (var node in nodes)
            {
                ((IVisitable)node).Accept(this);
            }
        }

        public void Visit(BlockStatement node)
        {
            foreach (var statement in node.Statements)
            {
                statement.Parent = node;
                ((IVisitable)statement).Accept(this);
            }
        }

        public void Visit(ExpressionStatement node)
        {
            node.Expression.Parent = node;
            ((IVisitable)node.Expression).Accept(this);
        }

        public void Visit(PrintStatement node)
        {
            node.Expression.Parent = node;
            ((IVisitable)node.Expression).Accept(this);
        }

        public void Visit(ReturnStatement node)
        {
            if (node.Value != null)
            {
                node.Value.Parent = node;
                ((IVisitable)node.Value).Accept(this);
            }
        }

        public void Visit(VariableDeclarationStatement node)
        {
            if (node.Initializer != null)
            {
                node.Initializer.Parent = node;
                ((IVisitable)node.Initializer).Accept(this);
            }
        }

        public void Visit(IfStatement node)
        {
            node.Condition.Parent = node;
            node.ThenBranch.Parent = node;
            ((IVisitable)node.Condition).Accept(this);
            ((IVisitable)node.ThenBranch).Accept(this);

            if (node.ElseBranch != null)
            {
                node.ElseBranch.Parent = node;
                ((IVisitable)node.ElseBranch).Accept(this);
            }
        }

        public void Visit(WhileStatement node)
        {
            node.Condition.Parent = node;
            node.Body.Parent = node;
            ((IVisitable)node.Condition).Accept(this);
            ((IVisitable)node.Body).Accept(this);
        }

        public void Visit(FunctionStatement node)
        {
            foreach (var parameter in node.Parameters)
            {
                parameter.Parent = node;
            }
            foreach (var statement in node.Body)
            {
                statement.Parent = node;
                ((IVisitable)statement).Accept(this);
            }
        }

        public void Visit(ClassStatement node)
        {
            if (node.SuperClass != null) node.SuperClass.Parent = node;
            foreach (var method in node.Methods)
            {
                method.Parent = node;
                ((IVisitable)method).Accept(this);
            }
        }

        public void Visit(BinaryExpression node)
        {
            node.Left.Parent = node;
            node.Right.Parent = node;
            ((IVisitable)node.Left).Accept(this);
            ((IVisitable)node.Right).Accept(this);
        }

        public void Visit(UnaryExpression node)
        {
            node.Right.Parent = node;
            ((IVisitable)node.Right).Accept(this);
        }

        public void Visit(VariableExpression node)
        { }

        public void Visit(GroupingExpression node)
        {
            node.Expression.Parent = node;
            ((IVisitable)node.Expression).Accept(this);
        }

        public void Visit(AssignmentExpression node)
        {
            node.Value.Parent = node;
            ((IVisitable)node.Value).Accept(this);
        }

        public void Visit(LiteralExpression node)
        { }

        public void Visit(CallExpression node)
        {
            node.Callee.Parent = node;
            ((IVisitable)node.Callee).Accept(this);
            foreach (var argument in node.Arguments)
            {
                argument.Parent = node;
                ((IVisitable)argument).Accept(this);
            }
        }

        public void Visit(SetExpression node)
        {
            node.Object.Parent = node;
            node.Value.Parent = node;
            ((IVisitable)node.Object).Accept(this);
            ((IVisitable)node.Value).Accept(this);
        }

        public void Visit(GetExpression node)
        {
            node.Object.Parent = node;
            ((IVisitable)node.Object).Accept(this);
        }

        public void Visit(ThisExpression node)
        { }

        public void Visit(SuperExpression node)
        { }
    }
}