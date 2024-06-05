using Lox;
using LoxOnDLR.Infrastructure;
using LoxOnDLR.Runtime;
using LoxOnDLR.Runtime.DLR_Binders;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace LoxOnDLR.Pipeline
{
    internal class ExpressionGenerator : ISyntaxNodeVisitor<Expression>
    {
        private const string THIS = "_this";
        private string currentFunction;
        private string currentClass;
        private string currentSuperClass;

        private SymbolTableScope symbolTable;

        public ExpressionGenerator(SymbolTableScope scope)
        {
            symbolTable = scope;
        }

        public Expression Generate(Lox.SyntaxNode node)
        {
            return ((IVisitable)node).Accept(this);
        }

        public IEnumerable<Expression> Generate(IEnumerable<SyntaxNode> nodes)
        {
            var expressions = new List<Expression>();
            foreach (var node in nodes)
            {
                expressions.Add(((IVisitable)node).Accept(this));
            }
            return expressions;
        }

        public Expression Visit(BlockStatement node)
        {
            List<Expression> bodyOfBlock = new List<Expression>();
            var oldScope = symbolTable;
            symbolTable = symbolTable.CreateBlockScope("<b>");

            foreach (var statement in node.Statements)
            {
                bodyOfBlock.Add(((IVisitable)statement).Accept(this));
            }
            var block = Expression.Block(symbolTable.Names.Values, bodyOfBlock);
            symbolTable = oldScope;
            return block;
        }

        public Expression Visit(ExpressionStatement node)
        {
            return ((IVisitable)node.Expression).Accept(this);
        }

        public Expression Visit(PrintStatement node)
        {
            var valNode = node.Expression;
            ThrowIfMethodNameUsedAsVariableForOtherThanCallORReturn(valNode);

            var val = ((IVisitable)node.Expression).Accept(this);
            val = RuntimeHelpers.EnsureObjectResult(val);

            return Expression.Call(null, typeof(LoxRuntimeHelpers).GetMethod("Print", new Type[] { typeof(object) }), val);
        }
       
        private void ThrowIfMethodNameUsedAsVariableForOtherThanCallORReturn(SyntaxNode valNode)
        {
            if (valNode != null && valNode is VariableExpression)
            {
                var variableName = ((VariableExpression)valNode).Name.Lexeme;
                //Guard
                if (symbolTable.IsSymbolAFunctionName(variableName))
                    throw new LoxRuntimeException($"Undefined variable '{variableName}'.", ((VariableExpression)valNode).Name.Line);
            }
        }

        public Expression Visit(ReturnStatement node)
        {
            ThrowIfMethodNameUsedAsVariableForOtherThanCallORReturn(node.Value);
            if (node.Value != null)
            {
                return Expression.Return(symbolTable.FindReturnTarget(), RuntimeHelpers.EnsureObjectResult(((IVisitable)node.Value).Accept(this)));
            }
            Expression defaultValueToReturnFromFunc = Expression.Constant(null, typeof(object)); // return a null object by default
            // unless this method is a class init method, in which case we return the value of the "_this" variable
            if (IsReturnFromAClassInitMethod(node))
                defaultValueToReturnFromFunc = symbolTable.FindIdentifierInSymbolTable(THIS);

            return Expression.Return(symbolTable.FindReturnTarget(), defaultValueToReturnFromFunc);
        }

        private bool IsReturnFromAClassInitMethod(SyntaxNode node)
        {
            var currentNode = node;
            bool foundClass = false;
            bool methodIsInit = false;
            while (currentNode != null)
            {
                foundClass = currentNode is ClassStatement | foundClass;
                if (currentNode is FunctionStatement)
                {
                    methodIsInit = ((FunctionStatement)currentNode).Name.Lexeme == "init" | methodIsInit;
                }
                currentNode = currentNode.Parent;
            }

            return foundClass && methodIsInit;
        }

        public Expression Visit(VariableDeclarationStatement node)
        {
            var variableName = node.Name.Lexeme;
            //Guard
            ThrowIfMethodNameUsedAsVariableForOtherThanCallORReturn(node);

            ParameterExpression v;
            // allow for variables to be redeclared and re-initialized at the module level
            if (symbolTable.IsModule && symbolTable.Names.Keys.Contains(node.Name.Lexeme))
            {
                v = symbolTable.Names[variableName];
            }
            else
            {
                v = Expression.Parameter(typeof(object), variableName);
                symbolTable.Names.Add(variableName, v);
            }
            Expression init = null;
            if (node.Initializer != null)
            {
                init = RuntimeHelpers.EnsureObjectResult(((IVisitable)node.Initializer).Accept(this));
            }
            else
                init = Expression.Constant(null, typeof(object));
            return Expression.Assign(v, init);
        }

        public Expression Visit(IfStatement node)
        {
            var conditionTemp = Expression.Parameter(typeof(object), "IfConditionTmp");
            var ifStatementBody = new List<Expression>() {
                Expression.Assign(conditionTemp, RuntimeHelpers.EnsureObjectResult(( (IVisitable)node.Condition).Accept(this))) };

            if (node.ElseBranch == null)
            {
                ifStatementBody.Add(Expression.IfThen(GeneratorHelpers.TruthyTest(conditionTemp), ((IVisitable)node.ThenBranch).Accept(this)));
            }
            else ifStatementBody.Add(Expression.IfThenElse(GeneratorHelpers.TruthyTest(conditionTemp), ((IVisitable)node.ThenBranch).Accept(this), ((IVisitable)node.ElseBranch).Accept(this)));
            return Expression.Block(new[] { conditionTemp }, ifStatementBody);
        }

        public Expression Visit(WhileStatement node)
        {
            var oldScope = symbolTable;
            var exitLabel = Expression.Label(typeof(void), "LoopBreak");
            var continueLabel = Expression.Label(typeof(void), "LoopContinue");
            symbolTable = symbolTable.CreateLoopScope("<whileLoop>", exitLabel, continueLabel);
            var loopSymbolTable = symbolTable;
            var condition = ((IVisitable)node.Condition).Accept(this);
            var notCondition = Expression.Convert(Expression.Not(Expression.Convert(condition, typeof(bool))), typeof(bool));
            var shouldContinue = Expression.IfThen(notCondition, Expression.Break(symbolTable.LoopBreak));
            var body = new Expression[2];
            body[0] = shouldContinue;
            body[1] = ((IVisitable)node.Body).Accept(this);
            this.symbolTable = oldScope;
            return Expression.Loop(Expression.Block(typeof(void), body), exitLabel, continueLabel);
        }

        public Expression Visit(FunctionStatement node)
        {
            var functionName = node.Name.Lexeme;
            var oldScope = symbolTable;

            // Define the function as an entry in the symbol table to support recursion and for later as a storage location for where the function is assigned
            // At the module level, the function is ALREADY defined in the symbol table by the LoxRuntime before Generation begins
            if (!symbolTable.IsModule)
                symbolTable.Names.Add(functionName, Expression.Parameter(typeof(LoxFunction), functionName));
            var symbolTableFuncReturn = Expression.Label(typeof(object), functionName + ".Return");

            symbolTable = symbolTable.CreateFunctionScope($":{functionName}", symbolTableFuncReturn);

            var signature = new List<ParameterExpression>();
            foreach (var param in node.Parameters)
            {
                var p = Expression.Parameter(typeof(object), param.Lexeme);
                symbolTable.Names.Add(param.Lexeme, p);
                signature.Add(p);
            }

            var body = new List<Expression>();
            foreach (var statement in node.Body)
            {
                body.Add(((IVisitable)statement).Accept(this));
            }
            // return a null object by default
            // unless this method is a class init method, in which case we return the value of the "_this" variable
            Expression defaultValueToReturnFromFunc = IsReturnFromAClassInitMethod(node) ?
                symbolTable.FindIdentifierInSymbolTable(THIS) :
                Expression.Constant(null, typeof(object));

            body.Add(Expression.Label(symbolTable.FuncReturn, defaultValueToReturnFromFunc));

            //find all the names in the function's scope that are not in the signature. These must have been added as variable declarations or nested function declarations.
            //Add them as params/variables of the block

            var funcVariables = symbolTable.Names.Values.ToHashSet().Except(signature).ToList();

            Expression funcLambda;
            var bodyBlock = Expression.Block(funcVariables, body);

            Expression returnExpression;

            symbolTable = oldScope;

            if (!symbolTable.IsCurrentScopeAClass())
            {
                funcLambda = Expression.Lambda(
                                bodyBlock,
                                functionName,
                                signature);

                // the containing scope for this function is a function. Store the Lambda for this function in a variable attached to the outer function

                var variable = symbolTable.Names[functionName];
                var funcCallable = Expression.New(typeof(LoxFunction).GetConstructor(new Type[] { typeof(string), typeof(int), typeof(LambdaExpression) }),
                                    new Expression[] { Expression.Constant(functionName), Expression.Constant(node.Parameters.Count), funcLambda });

                returnExpression = Expression.Assign(variable, funcCallable);
            }
            else
            {
                // is a Method on a Class
                var self = symbolTable.FindIdentifierInSymbolTable(THIS) as ParameterExpression;
                signature.Insert(0, self);

                funcLambda = Expression.Lambda(
                                bodyBlock,
                                currentClass + "." + functionName,
                                signature);
                returnExpression = funcLambda;
            }

            return returnExpression;
        }

        public Expression Visit(ClassStatement node)
        {
            var className = node.Name.Lexeme;
            if (!symbolTable.IsModule && symbolTable.FindIdentifierInSymbolTable(className) != null)
                throw new InvalidOperationException($"Class {className} already defined in this scope");

            string superClassName = string.Empty;

            if (node.SuperClass != null)
            {
                superClassName = node.SuperClass.Name.Lexeme;
            }

            var oldClassName = currentClass;
            var oldSuperClassName = currentSuperClass;

            currentClass = className;
            currentSuperClass = superClassName;

            List<Expression> definitionScript = new List<Expression>();
            //  implement defining a class and return expression sequence that:
            //    calls LoxObject.Define to add the class to the module table
            //    adds a constructor function in the scope that calls LoxObject.Create
            var classDefinition = Expression.Call(
                typeof(LoxPrototype),
                "DefineClass",
                null,
                new Expression[] { Expression.Constant(className), Expression.Constant(superClassName), Expression.Constant(node.Name.Line), symbolTable.GetRuntimeExpr() });
            definitionScript.Add(classDefinition);

            // For classes defined at the module level, the class is ALREADY defined in the symbol table by the LoxRuntime before Generation begins
            if (!symbolTable.IsModule)
                symbolTable.Names[className] = Expression.Parameter(typeof(LoxClassConstructorFunction), className); // define the name of the class in the symbol table to support recursion?

            // this is used to create a variable in the scope/block that has this class definition.
            var classConstructorVariable = symbolTable.Names[className];

            // process the methods of the class within a nested scope
            var oldScope = symbolTable;

            symbolTable = symbolTable.CreateClassScope($":{className}");

            symbolTable.Names[THIS] = Expression.Parameter(typeof(LoxObject), THIS);

            foreach (var method in node.Methods)
            {
                var methodDefinition = (((IVisitable)method).Accept(this));
                var instanceMethod = Expression.New(typeof(LoxFunction).GetConstructor(new Type[] { typeof(string), typeof(int), typeof(LambdaExpression) }),
                    new Expression[] { Expression.Constant(method.Name.Lexeme), Expression.Constant(method.Parameters.Count + 1), methodDefinition });

                var defineMethodExpr = Expression.Call(
                   typeof(LoxPrototype).GetMethod("DefineMethod"),
                   Expression.Constant(className), Expression.Constant(method.Name.Lexeme), symbolTable.GetRuntimeExpr(), instanceMethod);
                definitionScript.Add(defineMethodExpr);
            }

            symbolTable = oldScope;
            currentClass = oldClassName;
            currentSuperClass = oldSuperClassName;

            var initMethodArity = node.Tags.OfType<ClassConstructorArityTag>().FirstOrDefault()?.Arity ?? 0;
            // define a function that will construct instance of the class by calling upon the LoxObject.Create function
            // the signature of this function must match the signature of the init method (if one exists)

            var constructorBody = new List<Expression>();
            var newObject = Expression.Parameter(typeof(LoxObject), "newObject");
            var constructorReturnLabel = Expression.Label(typeof(LoxObject), "constructorReturn");
            var createCallArguments = new List<Expression>() { Expression.Constant(className) };
            var initArgParams = new List<ParameterExpression>();
            for (int i = 0; i < initMethodArity; i++) { initArgParams.Add(Expression.Parameter(typeof(object))); }
            var initArgsArrayParam = Expression.NewArrayInit(typeof(object), initArgParams.ToArray());
            if (initMethodArity > 0) { createCallArguments.Add(initArgsArrayParam); }
            Expression classConstructorDefinition = Expression.Assign(newObject,
                   Expression.Call(
                       typeof(LoxPrototype),
                       "Create",
                       null,
                       createCallArguments.ToArray()
                       ));

            constructorBody.Add(classConstructorDefinition);
            constructorBody.Add(Expression.Return(constructorReturnLabel, newObject));
            constructorBody.Add(Expression.Label(constructorReturnLabel, Expression.Convert(Expression.Constant(null), typeof(LoxObject))));
            var constructorBodyBlock = Expression.Block(new ParameterExpression[] { newObject }, constructorBody);
            var constructorLambda = Expression.Lambda(
                constructorBodyBlock,
                className + ".ctor",
                 initArgParams.ToArray()
                );

            var constructorCall = Expression.New(
                typeof(LoxClassConstructorFunction).GetConstructor(new Type[] { typeof(string), typeof(int), typeof(LambdaExpression) }),
                new Expression[] { Expression.Constant(className), Expression.Constant(initMethodArity), constructorLambda });

            var assignSymbol = Expression.Assign(classConstructorVariable, constructorCall);

            definitionScript.Add(assignSymbol);
            return Expression.Block(definitionScript);
        }

        public Expression Visit(Lox.BinaryExpression node)
        {
            ThrowIfMethodNameUsedAsVariableForOtherThanCallORReturn(node.Left);
            ThrowIfMethodNameUsedAsVariableForOtherThanCallORReturn(node.Right);

            var left = ((IVisitable)node.Left).Accept(this);
            var right = ((IVisitable)node.Right).Accept(this);

            if (node.Operator.Kind == SyntaxKind.EqualEqual || node.Operator.Kind == SyntaxKind.BangEqual)
            {
                var equalityTest = Expression.Call(typeof(LoxRuntimeHelpers).GetMethod("LoxEq"), RuntimeHelpers.EnsureObjectResult(left), RuntimeHelpers.EnsureObjectResult(right));
                if (node.Operator.Kind == SyntaxKind.BangEqual)
                    return Expression.Not(equalityTest);

                return equalityTest;
            }
            if (node.Operator.Kind == SyntaxKind.AndAnd)
            {
                var andAndTempLeftValue = Expression.Parameter(typeof(object), "andAndTempLeft");
                var andAndTempRightValue = Expression.Parameter(typeof(object), "andAndTempRight");
                var andAndAssignLeft = Expression.Assign(andAndTempLeftValue, RuntimeHelpers.EnsureObjectResult(left));
                var andAndAssignRight = Expression.Assign(andAndTempRightValue, RuntimeHelpers.EnsureObjectResult(right));
                return Expression.Block(new ParameterExpression[] { andAndTempLeftValue, andAndTempRightValue },
                    andAndAssignLeft,
                    Expression.Condition(
                        Expression.AndAlso(GeneratorHelpers.TruthyTest(andAndTempLeftValue), Expression.Block(andAndAssignRight, GeneratorHelpers.TruthyTest(andAndTempRightValue))),
                        RuntimeHelpers.EnsureObjectResult(andAndTempRightValue),
                        Expression.Condition(GeneratorHelpers.TruthyTest(andAndTempLeftValue),
                            RuntimeHelpers.EnsureObjectResult(andAndTempRightValue),
                            RuntimeHelpers.EnsureObjectResult(andAndTempLeftValue),
                            typeof(object)),
                        typeof(object)));
            }
            if (node.Operator.Kind == SyntaxKind.OrOr)
            {
                var OrOrTempLeftValue = Expression.Parameter(typeof(object), "OrOrTempLeft");
                var OrOrTempRightValue = Expression.Parameter(typeof(object), "OrOrTempRight");
                var OrOrAssignLeft = Expression.Assign(OrOrTempLeftValue, RuntimeHelpers.EnsureObjectResult(left));
                var OrOrAssignRight = Expression.Assign(OrOrTempRightValue, RuntimeHelpers.EnsureObjectResult(right));

                return Expression.Block(new ParameterExpression[] { OrOrTempLeftValue, OrOrTempRightValue },
                    OrOrAssignLeft,
                    Expression.Condition(
                        Expression.OrElse(GeneratorHelpers.TruthyTest(OrOrTempLeftValue), Expression.Block(OrOrAssignRight, GeneratorHelpers.TruthyTest(OrOrTempRightValue))),
                        Expression.Condition(GeneratorHelpers.TruthyTest(OrOrTempLeftValue),
                            RuntimeHelpers.EnsureObjectResult(OrOrTempLeftValue),
                            RuntimeHelpers.EnsureObjectResult(OrOrTempRightValue),
                            typeof(object)),
                        RuntimeHelpers.EnsureObjectResult(OrOrTempRightValue),
                        typeof(object)));
            }
            var op = node.Operator.Kind switch
            {
                SyntaxKind.Minus => ExpressionType.Subtract,
                SyntaxKind.Slash => ExpressionType.Divide,
                SyntaxKind.Star => ExpressionType.Multiply,
                SyntaxKind.Plus => ExpressionType.Add,
                SyntaxKind.Greater => ExpressionType.GreaterThan,
                SyntaxKind.GreaterEqual => ExpressionType.GreaterThanOrEqual,
                SyntaxKind.Less => ExpressionType.LessThan,
                SyntaxKind.LessEqual => ExpressionType.LessThanOrEqual,
            };
            return GeneratorHelpers.Checked(
                Expression.Dynamic(LoxBinders.GetBinaryOperationBinder(op),
                    typeof(object),
                    left,
                    right),
                node.Operator.Line);
        }

        public Expression Visit(Lox.UnaryExpression node)
        {
            ThrowIfMethodNameUsedAsVariableForOtherThanCallORReturn(node.Right);

            switch (node.Operator.Kind)
            {
                case SyntaxKind.Minus:
                    var temp = Expression.Parameter(typeof(object), "temp");
                    var assignTemp = Expression.Assign(temp, RuntimeHelpers.EnsureObjectResult(((IVisitable)node.Right).Accept(this)));
                    var check = Expression.Condition(
                        Expression.TypeIs(temp, typeof(double)),
                        Expression.Negate(Expression.Convert(temp, typeof(double))),
                        Expression.Block(
                            Expression.Throw(
                                Expression.New(typeof(LoxRuntimeException).GetConstructor(new Type[] { typeof(string), typeof(int) }),
                                    Expression.Constant($"Operand must be a number."),
                                    Expression.Constant(node.Operator.Line))),
                                Expression.Constant(0.0D)));
                    return Expression.Block(new ParameterExpression[] { temp }, assignTemp, check);
                    break;

                case SyntaxKind.Bang:
                    var bangTemp = Expression.Parameter(typeof(object), "temp");
                    var assignBangTemp = Expression.Assign(bangTemp, RuntimeHelpers.EnsureObjectResult(((IVisitable)node.Right).Accept(this)));
                    return Expression.Block(new[] { bangTemp }, assignBangTemp, Expression.Not(GeneratorHelpers.TruthyTest(bangTemp)));
                    break;

                default:
                    throw new NotImplementedException();
                    break;
            }
        }

        public Expression Visit(VariableExpression node)
        {
            var param = symbolTable.FindIdentifierInSymbolTable(node.Name.Lexeme);
            if (param != null) return param;

            // if we can't find a param/variable in the scope chain, then it must be a Class Prototype stored in the runtime

            // TODO: build class constructor search in to the runtime, returning LRE and then use the Checked function wrapper
            return Expression.TryCatch(
                Expression.Property(
                    Expression.Field(
                        symbolTable.GetRuntimeExpr(),
                        "LoxPrototypes"),
                    "Item",
                    Expression.Constant(node.Name.Lexeme)),
                Expression.Catch(
                    typeof(KeyNotFoundException),
                    Expression.Block(
                        Expression.Throw(
                            Expression.New(typeof(LoxRuntimeException).GetConstructor(new Type[] { typeof(string), typeof(int) }),
                                                new Expression[] { Expression.Constant($"Undefined variable '{node.Name.Lexeme}'."), Expression.Constant(node.Name.Line) })
                            ),
                        Expression.Constant(null))));
        }

        public Expression Visit(GroupingExpression node)
        {
            return ((IVisitable)node.Expression).Accept(this);
        }

        public Expression Visit(AssignmentExpression node)
        {
            var v = symbolTable.FindIdentifierInSymbolTable(node.Name.Lexeme);

            if (v == null) throw new LoxRuntimeException("Undefined variable '" + node.Name.Lexeme + "'.", node.Name.Line);
            if (symbolTable.IsSymbolAFunctionName(node.Name.Lexeme)) throw new LoxRuntimeException("Undefined variable '" + node.Name.Lexeme + "'.", node.Name.Line);
            ThrowIfMethodNameUsedAsVariableForOtherThanCallORReturn(node.Value); 

            var val = RuntimeHelpers.EnsureObjectResult(((IVisitable)node.Value).Accept(this));
            return Expression.Assign(v, val);
        }

        public Expression Visit(LiteralExpression node)
        {
            var val = node.Value as Some<object>;
            return Expression.Constant(val.Value);
        }

        public Expression Visit(CallExpression node)
        {
            var func = ((IVisitable)node.Callee).Accept(this);
            List<Expression> args = new List<Expression>();
            args.Add(func);
            args.AddRange(node.Arguments.Select(x => RuntimeHelpers.EnsureObjectResult(((IVisitable)x).Accept(this))));
            Expression[] argsArray = args.ToArray();

            var targetInvocationExceptionToBeCaught = Expression.Parameter(typeof(TargetInvocationException), "e");
            var loxRuntimeExceptionToBeCaught = Expression.Parameter(typeof(LoxRuntimeException), "caughtlre");
            var loxRuntimeException = Expression.Parameter(typeof(LoxRuntimeException), "lre");

            // switch on the result of the call to ConfirmCallableType
            var call = Expression.Block(
                            Expression.Call(typeof(LoxRuntimeHelpers).GetMethod("ThrowIfNotCallable"), RuntimeHelpers.EnsureObjectResult(func), Expression.Constant(node.Paren.Line)),
                            Expression.TryCatch(
                                Expression.Dynamic(
                                    LoxBinders.GetInvokeBinder(new CallInfo(args.Count - 1)),
                                    typeof(object),
                                    argsArray),
                                Expression.Catch(
                                    targetInvocationExceptionToBeCaught,
                                    Expression.Block(new List<ParameterExpression> { loxRuntimeException },
                                                    Expression.Assign(loxRuntimeException, Expression.Convert(Expression.Property(targetInvocationExceptionToBeCaught, "InnerException"), typeof(LoxRuntimeException))),
                                                    Expression.Condition(
                                                            Expression.Call(loxRuntimeException, typeof(LoxRuntimeException).GetMethod("HasLine")),
                                                        //Then
                                                        Expression.Block(
                                                            Expression.Throw(loxRuntimeException),
                                                            Expression.Constant(null)),
                                                        //Else
                                                        Expression.Block(
                                                            Expression.Throw(Expression.Call(loxRuntimeException, typeof(LoxRuntimeException).GetMethod("AtLine"), Expression.Constant(node.Paren.Line))),
                                                            Expression.Constant(null))
                                                        )
                                                    )
                                    ),
                                Expression.Catch(
                                    loxRuntimeExceptionToBeCaught,
                                    Expression.Condition(
                                            Expression.Call(loxRuntimeExceptionToBeCaught, typeof(LoxRuntimeException).GetMethod("HasLine")),
                                            //Then
                                            Expression.Block(
                                                Expression.Throw(loxRuntimeExceptionToBeCaught),
                                                Expression.Constant(null)),
                                            //Else
                                            Expression.Block(
                                                Expression.Throw(Expression.Call(loxRuntimeExceptionToBeCaught, typeof(LoxRuntimeException).GetMethod("AtLine"), Expression.Constant(node.Paren.Line))),
                                                Expression.Constant(null))
                                            )
                                        )

                                )
                       );
            return call;
        }

        public Expression Visit(SetExpression node)
        {
            ThrowIfMethodNameUsedAsVariableForOtherThanCallORReturn(node.Object);
            ThrowIfMethodNameUsedAsVariableForOtherThanCallORReturn(node.Value);

            var obj = ((IVisitable)node.Object).Accept(this);
            var val = ((IVisitable)node.Value).Accept(this);
            var exp = Expression.Block(
                            Expression.Call(typeof(LoxRuntimeHelpers).GetMethod("ThrowIfNotSupportingFields"), RuntimeHelpers.EnsureObjectResult(obj), Expression.Constant(node.Name.Line), Expression.Constant("fields")),
                            GeneratorHelpers.Checked(
                                Expression.Dynamic(
                                    LoxBinders.GetSetMemberBinder(node.Name.Lexeme),
                                    typeof(object),
                                    obj,
                                    RuntimeHelpers.EnsureObjectResult(val)),
                                node.Name.Line));
            return exp;
        }

        public Expression Visit(GetExpression node)
        {
            ThrowIfMethodNameUsedAsVariableForOtherThanCallORReturn(node.Object);

            var lre = Expression.Variable(typeof(LoxRuntimeException), "lre");
            var obj = ((IVisitable)node.Object).Accept(this);
            var exp = Expression.Block(
                            Expression.Call(typeof(LoxRuntimeHelpers).GetMethod("ThrowIfNotSupportingFields"), RuntimeHelpers.EnsureObjectResult(obj), Expression.Constant(node.Name.Line), Expression.Constant("properties")),
                            Expression.TryCatch(
                                Expression.Dynamic(
                                    LoxBinders.GetGetMemberBinder(node.Name.Lexeme, null, false),
                                    typeof(object),
                                    obj),
                                Expression.Catch(
                                    lre,
                                    Expression.Block(
                                        Expression.Throw(Expression.Call(lre, typeof(LoxRuntimeException).GetMethod("AtLine"), Expression.Constant(node.Name.Line))),
                                        Expression.Constant(null))
                                    )
                                )
                            );
            return exp;
        }

        public Expression Visit(ThisExpression node)
        {
            //validate that this is in a method scope
            var thisExpr = RuntimeHelpers.EnsureObjectResult(symbolTable.FindIdentifierInSymbolTable(THIS));
            if (thisExpr == null)
            {
                throw new InvalidOperationException("this must be in a method scope");
            }
            return thisExpr;
        }

        public Expression Visit(SuperExpression node)
        {
            // in Lox, super is a pseudo "GetMember" in that it returns a field or callable from the first superclass of the object to have the named member.

            var _this = RuntimeHelpers.EnsureObjectResult(symbolTable.FindIdentifierInSymbolTable(THIS));

            if (_this == null)
            {
                throw new InvalidOperationException("super must be used in a method scope");
            }
            // this binder sets the start of the search chain to be the superclass of the class in current lexical scope
            var superGetMemberBinder = LoxBinders.GetGetMemberBinder(node.Method.Lexeme, currentSuperClass, false);
            return GeneratorHelpers.Checked(
                        Expression.Dynamic(
                            superGetMemberBinder,
                            typeof(object),
                            _this),
                        node.Method.Line);
        }

        // can super be used in any method or only in the init method - any method
        // can super be used to reference a field? (or only methods)? - both
        // can super be used as the target of a Call expression? (such as super() to create an instance of the superclass) - no
        // can super be referenced as a variable? (as in var x = super;) - no
    }
}