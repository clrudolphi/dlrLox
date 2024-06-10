using System.Text;

namespace LoxOnDLR.Runtime
{
    internal static class LoxRuntimeHelpers
    {
        // Time keeping functions to support the built-in function Clock()
        private static readonly DateTime _startTime = DateTime.Now;

        public static object BooleansToLowerStrings(object obj)
        {
            if (obj is bool)
                return (bool)obj ? "true" : "false";
            else
                return obj;
        }

        public static double Clock()
        {
            var ts = (DateTime.Now - _startTime);
            var t = ts.TotalMilliseconds / 1000.0;
            return t;
        }

        public static object EnsurePrintableNil(object val)
        {
            if (val == null)
                return "nil";
            return val;
        }

        public static bool IsNotAscii(string input)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var test = bytes.Any(b => b > 127);
                return test;
            }
            catch (ArgumentException)
            {
                return true;
            }
        }

        // Uses of the 'eq' keyword form in Sympl compile to a call to this
        // helper function.
        //
        public static bool LoxEq(object x, object y)
        {
            if (x == null)
                return y == null;
            else if (y == null)
                return x == null;
            else
            {
                var xtype = x.GetType();
                var ytype = y.GetType();
                if (xtype.IsPrimitive && xtype != typeof(string) &&
                    ytype.IsPrimitive && ytype != typeof(string))
                    return x.Equals(y);
                else
                    return object.ReferenceEquals(x, y);
            }
        }

        public static void Print(object o)
        {
            o = LoxRuntimeHelpers.BooleansToLowerStrings(o);
            o = LoxRuntimeHelpers.EnsurePrintableNil(o);

            var current = Console.OutputEncoding;
            if (IsNotAscii(o.ToString()))
            {
                Console.OutputEncoding = Encoding.UTF8;
                Console.WriteLine(o);
                Console.OutputEncoding = current;
            }
            else
            {
                Console.WriteLine(o);
            }
        }

        public static void ThrowIfNotCallable(object thing, int line)
        {
            if (thing == null) throw new LoxRuntimeException("Can only call functions and classes.", line);
            var t = thing.GetType();
            if (t.IsPrimitive
                || t == typeof(string)
                || t == typeof(Lox.Nothing)
                || t == typeof(LoxObject))
                throw new LoxRuntimeException("Can only call functions and classes.", line);
        }

        public static void ThrowIfNotSupportingFields(object thing, int line, string fieldPropLabel)
        {
            if (thing == null) throw new LoxRuntimeException($"Only instances have {fieldPropLabel}.", line); 
            var t = thing.GetType();
            if (t.IsPrimitive
                || t == typeof(string)
                || t == typeof(Lox.Nothing)
                || t == typeof(LoxFunction)
                || t == typeof(LoxClassConstructorFunction))
                throw new LoxRuntimeException($"Only instances have {fieldPropLabel}.", line);
        }
    }
}