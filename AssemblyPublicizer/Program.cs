using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace AssemblyPublicizer
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Argument<string>(
                    "input",
                    "Path (relative or absolute) to the input assembly"
                ),

                new Option<string>(
                    "--output",
                    () => Library.Publicizer.DefaultOutputDir + Path.DirectorySeparatorChar,
                    "Path/dir/filename for the output assembly"
                )
            };

            rootCommand.Handler = CommandHandler.Create<string, string>(Library.Publicizer.Publicize);

            return await rootCommand.InvokeAsync(args);
        }
    }
}