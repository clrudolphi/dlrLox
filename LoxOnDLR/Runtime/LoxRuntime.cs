using Lox;
using LoxOnDLR.Infrastructure;
using LoxOnDLR.Pipeline;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using Scope = Microsoft.Scripting.Runtime.Scope;

namespace LoxOnDLR.Runtime
{
    internal class LoxRuntime
    {
        private IList<Assembly> _assemblies;
        private ExpandoObject _globals = new ExpandoObject();
        private Scope _dlrGlobals;

        public IDynamicMetaObjectProvider Globals
        { get { return _globals; } }

        public IDynamicMetaObjectProvider DlrGlobals
        { get { return _dlrGlobals; } }

        public static ExpandoObject CreateScope()
        {
            return new ExpandoObject();
        }

        public Dictionary<string, object> LoxPrototypes = new();

        // ExecuteFile executes the file in a new module scope and stores the
        // scope on Globals, using either the provided name, globalVar, or the
        // file's base name.  This function returns the module scope.
        //
        public IDynamicMetaObjectProvider ExecuteFile(string filename)
        {
            return ExecuteFile(filename, null);
        }

        public IDynamicMetaObjectProvider ExecuteFile(string filename,
                                                      string globalVar)
        {
            var moduleEO = CreateScope();
            ExecuteFileInScope(filename, moduleEO);

            globalVar = globalVar ?? Path.GetFileNameWithoutExtension(filename);
            //            DynamicObjectHelpers.SetMember(this._globals, globalVar, moduleEO);
            ((IDictionary<string, object>)_globals)[globalVar] = moduleEO;

            return moduleEO;
        }

        // ExecuteFileInScope executes the file in the given module scope.  This
        // does NOT store the module scope on Globals.  This function returns
        // nothing.
        //
        public void ExecuteFileInScope(string filename,
                                       IDynamicMetaObjectProvider moduleEO)
        {
            // Simple way to convey script rundir for RuntimeHelpes.SymplImport
            // to load .sympl files.
            //DynamicObjectHelpers.SetMember(moduleEO, "__file__",
            //                               Path.GetFullPath(filename));
            ((IDictionary<string, object>)moduleEO)["__file__"] = Path.GetFullPath(filename);
            ((dynamic)moduleEO).ExitCode = 0;

            if (LoxSourceFileParser.TryParseFile(filename, out var asts))
            {
                try
                {
                    var moduleFun = GenerateModule(asts, filename);
                    var d = moduleFun.Compile();
                    LoxRuntimeHelpers.Clock();
                    d(this, moduleEO);
                }
                catch (LoxRuntimeException e)
                {
                    Console.Error.WriteLine(e.Message);
                    Console.Error.WriteLine(e.ToString());
                    ((dynamic)moduleEO).ExitCode = 70;
                }
                finally
                {
                }
            }
            else
            {
                ((dynamic)moduleEO).ExitCode = 65;
            }
        }

        internal Expression<Func<LoxRuntime, IDynamicMetaObjectProvider, object>>
                 GenerateModule(IEnumerable<SyntaxNode> asts, string filename)
        {
            var scope = new SymbolTableScope(
                                            null,
                                            filename,
                                            this,
                                            Expression.Parameter(typeof(LoxRuntime), "LoxRuntime"),
                                            Expression.Parameter(typeof(IDynamicMetaObjectProvider), "fileModule"));

            var topLevelSymbolDefiner = new ScopedSymbolDefiner(scope);
            topLevelSymbolDefiner.DefineSymbolsInScope(asts.ToList());

            List<Expression> body = new List<Expression>();
            ExpressionGenerator ETGen = new ExpressionGenerator(scope);
            body.AddRange(GeneratorHelpers.GenerateBuiltInFunctions(scope));

            body.AddRange(ETGen.Generate(asts));
            body.Add(Expression.Constant(null));
            var block = scope.Names.Count == 0 ? Expression.Block(body) : Expression.Block(scope.Names.Values, body);
            var moduleFun = Expression.Lambda<Func<LoxRuntime, IDynamicMetaObjectProvider, object>>(
                block,
                scope.RuntimeExpr,
                scope.ModuleExpr);
            return moduleFun;
        }
    }
}