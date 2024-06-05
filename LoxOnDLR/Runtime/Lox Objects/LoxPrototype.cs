using System.Collections;
using System.Dynamic;

namespace LoxOnDLR.Runtime
{
    internal class LoxPrototype : DynamicObject
    {
        public readonly string ClassName;
        private readonly string SuperclassName;
        private readonly LoxPrototype Superclass;
        internal readonly Dictionary<string, LoxFunction> ProtoMethods = new();
        private static LoxRuntime _runtime;
        internal int DeclaredOnLineNumber;

        //This constructor is called when defining the class during the startup script
        private LoxPrototype(string className, string superclassName, int declarationLineNumber)
        {
            ClassName = className;
            SuperclassName = superclassName;
            Superclass = superclassName == String.Empty ? null : FetchProto(superclassName);
            DeclaredOnLineNumber = declarationLineNumber;
        }

        public static void DefineClass(string className, string superclassName, int declarationLineNumber, LoxRuntime runtime)
        {
            _runtime = runtime;
            LoxPrototype proto;
            try
            {
                proto = new LoxPrototype(className, superclassName, declarationLineNumber);
            }
            catch
            {
                throw new LoxRuntimeException("Superclass must be a class.", declarationLineNumber);
            }

            runtime.LoxPrototypes.Add("_" + className, proto);
        }

        public static void DefineMethod(string className, string methodName, LoxRuntime runtime, LoxFunction method)
        {
            var proto = FetchProto(className);
            proto.DefineMethod(methodName, method);
        }

        private void DefineMethod(string name, LoxFunction method)
        {
            ProtoMethods.Add(name, method);
        }

        public static LoxObject Create(string className)
        {
            return Create(className, Array.Empty<object>());
        }

        public static LoxObject Create(string className, params object[] initargs)
        {
            LoxPrototype loxPrototype = FetchProto(className);
            var obj = new LoxObject(className, loxPrototype);

            object[] argInputs;
            if (initargs != null)
            {
                if (initargs is IEnumerable)
                {
                    argInputs = ((IEnumerable)initargs).Cast<object>().ToArray();
                }
                else
                {
                    argInputs = new object[] { initargs };
                }
            }
            else
            {
                argInputs = Array.Empty<object>();
            }
            int argCount = argInputs.Length;

            var methodKey = "init";
            if (obj.TryGetMethodGroup(methodKey, className, out var result))
            {
                var initMethodGroup = result as LoxMethodGroup;
                if (initMethodGroup != null)
                {
                    initMethodGroup.TryInvoke(argInputs, out var initResult);
                }
            }
            return obj;
        }

        private static LoxPrototype FetchProto(string className)
        {
            if (_runtime.LoxPrototypes.TryGetValue("_" + className, out var obj))
                return obj as LoxPrototype;
            throw new ApplicationException("Class not found: " + className);
        }

        internal IEnumerable<LoxPrototype> ProtoClassHeirarchy()
        {
            yield return this;
            LoxPrototype klass = this;
            while (klass.Superclass != null)
            {
                yield return klass = klass.Superclass;
            };
        }
    }
}