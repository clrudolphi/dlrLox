using LoxOnDLR.Runtime;
using System.Linq.Expressions;

namespace LoxOnDLR.Infrastructure
{
    // SymbolTableScope holds identifier information so that we can do name binding
    // during analysis.  It manages a map from names to ParameterExprs so ET
    // definition locations and reference locations can alias the same variable.
    //
    // These chain from inner most BlockExprs, through LambdaExprs, to the root
    // which models a file or top-level expression.  The root has non-None
    // ModuleExpr and RuntimeExpr, which are ParameterExprs.
    //
    internal class SymbolTableScope
    {
        private enum ScopeType
        {
            Module,
            Class,
            Function,
            Loop,
            Block
        }

        private ScopeType _scopeType;
        private SymbolTableScope _parent;
        private string _name;

        // Need runtime for interning Symbol constants at code gen time.
        private LoxRuntime _runtime;

        private ParameterExpression _runtimeParam;
        private ParameterExpression _moduleParam;

        // Need IsLambda when support return to find tightest closing fun.
        private bool _isLambda = false;

        private bool _isLoop = false;
        private LabelTarget _loopBreak = null;
        private LabelTarget _loopContinue = null;
        private LabelTarget _funcReturn = null;

        //private LabelTarget _continueBreak = null;
        private Dictionary<string, ParameterExpression> _names;

        private SymbolTableScope(SymbolTableScope parent, string name)
            : this(parent, name, null, null, null) { }

        private SymbolTableScope(SymbolTableScope parent, string name, ScopeType scopeType)
            : this(parent, name, null, null, null)
        {
            _scopeType = scopeType;
        }
        public SymbolTableScope(SymbolTableScope parent,
                              string name,
                              LoxRuntime runtime,
                              ParameterExpression runtimeParam,
                              ParameterExpression moduleParam)
        {
            _parent = parent;
            _name = (parent == null) ? name : parent.Name + "-" + name;
            _runtime = runtime;
            _runtimeParam = runtimeParam;
            _moduleParam = moduleParam;

            _names = new Dictionary<string, ParameterExpression>();
            _isLambda = false;
            _scopeType = ScopeType.Module;
        }

        public SymbolTableScope CreateClassScope(string name)
        {
            return new SymbolTableScope(this, name, ScopeType.Class);
        }

        public SymbolTableScope CreateFunctionScope(string name, LabelTarget funcReturn)
        {
            var scope = new SymbolTableScope(this, name, ScopeType.Function);
            scope.IsLambda = true;
            scope.FuncReturn = funcReturn;
            return scope;
        }

        public SymbolTableScope CreateLoopScope(string name, LabelTarget loopBreak, LabelTarget loopContinue)
        {
            var scope = new SymbolTableScope(this, name, ScopeType.Loop);
            scope.IsLoop = true;
            scope.LoopBreak = loopBreak;
            scope.LoopContinue = loopContinue;
            return scope;
        }

        public SymbolTableScope CreateBlockScope(string name)
        {
            return new SymbolTableScope(this, name, ScopeType.Block);
        }

        public bool IsCurrentScopeAClass() => this._scopeType == ScopeType.Class;

        public bool IsSymbolAFunctionName(string funcName)
        {
            var curScope = this;
            while (curScope != null)
            {
                if (curScope._scopeType == ScopeType.Function && curScope.Name.EndsWith($":{funcName}"))
                {
                    return true;
                }
                curScope = curScope.Parent;
            }
            return false;
        }

        public SymbolTableScope Parent
        { get { return _parent; } }

        public string Name
        { get { return _name; } }

        public ParameterExpression ModuleExpr
        { get { return _moduleParam; } }

        public ParameterExpression RuntimeExpr
        { get { return _runtimeParam; } }

        public LoxRuntime Runtime
        { get { return _runtime; } }

        public bool IsModule
        { get { return _moduleParam != null; } }

        public bool IsLambda
        {
            get { return _isLambda; }
            set { _isLambda = value; }
        }

        public LabelTarget FuncReturn
        {
            get { return _funcReturn; }
            set { _funcReturn = value; }
        }

        public bool IsLoop
        {
            get { return _isLoop; }
            set { _isLoop = value; }
        }

        public LabelTarget LoopBreak
        {
            get { return _loopBreak; }
            set { _loopBreak = value; }
        }

        public LabelTarget LoopContinue
        {
            get { return _loopContinue; }
            set { _loopContinue = value; }
        }

        public Dictionary<string, ParameterExpression> Names
        {
            get { return _names; }
            set { _names = value; }
        }

        public Expression FindIdentifierInSymbolTable(string name)
        {
            var curscope = this;
            ParameterExpression res;
            while (curscope != null)
            {
                if (curscope.Names.TryGetValue(name, out res))
                {
                    return res;
                }
                else
                {
                    curscope = curscope.Parent;
                }
            }

            return null;
        }

        public LabelTarget FindReturnTarget()
        {
            var curscope = this;
            while (curscope != null)
            {
                if (curscope.FuncReturn != null) return curscope.FuncReturn;
                curscope = curscope.Parent;
            }

            if (curscope == null)
                throw new InvalidOperationException("Return statement outside of a function.");
            return null;
        }

        public ParameterExpression GetModuleExpr()
        {
            var curScope = this;
            while (!curScope.IsModule)
            {
                curScope = curScope.Parent;
            }
            return curScope.ModuleExpr;
        }

        public ParameterExpression GetRuntimeExpr()
        {
            var curScope = this;
            while (curScope.RuntimeExpr == null)
            {
                curScope = curScope.Parent;
            }
            return curScope.RuntimeExpr;
        }

        public LoxRuntime GetRuntime()
        {
            var curScope = this;
            while (curScope.Runtime == null)
            {
                curScope = curScope.Parent;
            }
            return curScope.Runtime;
        }
    } //SymbolTableScope
}