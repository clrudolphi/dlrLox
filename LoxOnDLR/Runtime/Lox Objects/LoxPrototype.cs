using System.Collections;
using System.Dynamic;

namespace LoxOnDLR.Runtime
{
    internal struct MethodKey
    {
        public string MethodName;
        public int Depth;
    }

    internal class LoxPrototype
    {
        public readonly string ClassName;
        private readonly string SuperclassName;
        private readonly LoxPrototype? Superclass;
        internal readonly int Depth;
        internal Dictionary<string, int> ClassDepthMap = new();
        internal Dictionary<string, LoxProtoMethodGroup[]> ProtoMethodGroups = new();

        private static LoxRuntime _runtime;
        internal int DeclaredOnLineNumber;

        //This constructor is called when defining the class during the startup script
        private LoxPrototype(string className, string superclassName, int declarationLineNumber)
        {
            ClassName = className;
            SuperclassName = superclassName;
            Superclass = superclassName == String.Empty ? null : FetchProto(superclassName);
            Depth = Superclass == null ? 0 : (Superclass!.Depth) + 1;
            SetupDepthMap();
            CopyProtoDepthMap();

            DeclaredOnLineNumber = declarationLineNumber;

            void SetupDepthMap()
            {
                if (Superclass != null) ClassDepthMap = Superclass!.ClassDepthMap.ToDictionary(x => x.Key, x => x.Value);
                ClassDepthMap[ClassName] = Depth;
            }


            void CopyProtoDepthMap()
            {
                if (Depth > 0)
                {
                    foreach (var protoList in Superclass!.ProtoMethodGroups)
                    {
                        string methodName = protoList.Key;
                        var pmgArray = protoList.Value;
                        var newPmgArray = new LoxProtoMethodGroup[Depth + 1];
                        ProtoMethodGroups[methodName] = newPmgArray;
                        pmgArray.CopyTo(ProtoMethodGroups[methodName], 0);
                        newPmgArray[Depth] = newPmgArray[Depth - 1];
                    }
                }
            }
        }
        private void DefineMethod(string name, LoxFunction method)
        {

            method.ClassName = ClassName;

            if (!ProtoMethodGroups.TryGetValue(name, out LoxProtoMethodGroup[]? pmg))
            {
                pmg = new LoxProtoMethodGroup[Depth + 1];
                ProtoMethodGroups.Add(name, pmg);
                pmg[Depth] = new LoxProtoMethodGroup(name, method);
            }
            else
            {
                pmg[Depth] = new LoxProtoMethodGroup(name, method, pmg[Depth-1]);
            }
        }

        public LoxProtoMethodGroup? FindMethodGroup(string name, string searchRootClassName)
        {
            string searchRoot = searchRootClassName;
            string currentClassName = ClassName;

            int fetchDepth = ClassDepthMap[searchRootClassName];
            if (ProtoMethodGroups.TryGetValue(name, out var result))
            {
                return result[fetchDepth];
            }
            return null;
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
        public static LoxObject Create(string className)
        {
            return Create(className, Array.Empty<object>());
        }

        public static LoxObject Create(string className, params object[] initargs)
        {
            LoxPrototype loxPrototype = FetchProto(className);
            var obj = new LoxObject(className, loxPrototype);
            obj.InvokeInitFunction(initargs);
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