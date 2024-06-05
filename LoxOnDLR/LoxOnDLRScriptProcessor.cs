using LoxOnDLR.Runtime;

namespace LoxOnDLR
{
    public class LoxOnDLRScriptProcessor
    {
        public int Process(FileInfo inputValue)
        {
            var file = inputValue.FullName;
            var runtime = new LoxRuntime();
            dynamic moduleEO = runtime.ExecuteFile(file);

            int result = moduleEO.ExitCode;
            return result;
        }
    }
}