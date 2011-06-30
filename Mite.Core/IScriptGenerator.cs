using System.IO;

namespace Mite.Core
{
    public interface IScriptGenerator
    {
        void GenerateScript(TextWriter writer, bool includeData);
        void GenerateScript(TextWriter writer);
    }
}