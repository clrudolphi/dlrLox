namespace LoxOnDLR.Runtime
{
    internal class LoxRuntimeException : ApplicationException
    {
        private int _line = -1;

        public LoxRuntimeException(string message) : base(message)
        {
        }

        public LoxRuntimeException(string message, int line) : base(message)
        {
            _line = line;
        }

        public LoxRuntimeException AtLine(int line)
        {
            return new LoxRuntimeException(Message, line);
        }

        public int Line
        {
            get
            {
                return _line;
            }
        }

        public bool HasLine()
        {
            return _line >= 0;
        }

        public override string ToString()
        {
            var l = $"[line {_line}] ";
            return l + $"Error: {Message}";
        }
    }
}