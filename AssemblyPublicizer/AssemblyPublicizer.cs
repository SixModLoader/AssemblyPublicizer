using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using dnlib.DotNet;

namespace CabbageCrow.AssemblyPublicizer
{
    public class AssemblyPublicizer
    {
        private const string Suffix = "_publicized";
        private const string DefaultOutputDir = "publicized_assemblies";

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
                    () => DefaultOutputDir + Path.DirectorySeparatorChar,
                    "Path/dir/filename for the output assembly"
                )
            };

            rootCommand.Handler = CommandHandler.Create<string, string>((input, output) =>
            {
                if (string.IsNullOrEmpty(Path.GetFileName(output)))
                {
                    output = Path.Combine(output!, Path.GetFileNameWithoutExtension(input) + Suffix + Path.GetExtension(input));
                }

                if (!File.Exists(input))
                {
                    Console.WriteLine();
                    Console.WriteLine("ERROR! File doesn't exist or you don't have sufficient permissions.");
                    Environment.Exit(30);
                }

                ModuleDefMD module = null;

                try
                {
                    module = ModuleDefMD.Load(input, ModuleDef.CreateModuleContext());
                }
                catch (Exception e)
                {
                    Console.WriteLine();
                    Console.WriteLine("ERROR! Cannot read the assembly. Please check your permissions.");
                    Console.WriteLine(e);
                    Environment.Exit(40);
                }

                var runtimeVisibilityAttribute = new TypeDefUser("RuntimeVisibilityAttribute", module.Import(typeof(Attribute)))
                {
                    Attributes = TypeAttributes.Class & TypeAttributes.Public
                };

                module.Types.Add(runtimeVisibilityAttribute);

                runtimeVisibilityAttribute.Methods.Add(new MethodDefUser(
                        ".ctor",
                        MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.String),
                        MethodImplAttributes.Managed & MethodImplAttributes.IL,
                        MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName)
                    {
                        ParamDefs = {new ParamDefUser("visibility")}
                    }
                );

                var allTypes = module.GetTypes().ToList();

                var types = 0;
                var methods = 0;
                var fields = 0;

                foreach (var type in allTypes)
                {
                    foreach (var method in type.Methods)
                    {
                        if (method.IsPublic) continue;

                        methods++;
                        method.CustomAttributes.Add(
                            new CustomAttribute(runtimeVisibilityAttribute.FindConstructors().Single(),
                                new[]
                                {
                                    new CAArgument(module.CorLibTypes.String, method.Access.ToString())
                                })
                        );
                        method.Access = MethodAttributes.Public;
                    }

                    foreach (var field in type.Fields)
                    {
                        if (field.IsPublic) continue;

                        fields++;
                        field.CustomAttributes.Add(
                            new CustomAttribute(runtimeVisibilityAttribute.FindConstructors().Single(),
                                new[]
                                {
                                    new CAArgument(module.CorLibTypes.String, field.Access.ToString())
                                })
                        );
                        field.Access = FieldAttributes.Public;
                    }

                    if (type.IsNested ? type.IsNestedPublic : type.IsPublic) continue;

                    types++;
                    type.CustomAttributes.Add(
                        new CustomAttribute(runtimeVisibilityAttribute.FindConstructors().Single(),
                            new[]
                            {
                                new CAArgument(module.CorLibTypes.String, type.Visibility.ToString())
                            })
                    );
                    type.Visibility = type.IsNested ? TypeAttributes.NestedPublic : TypeAttributes.Public;
                }

                const string reportString = "Changed {0} {1} to public.";
                Console.WriteLine(reportString, types, "types");
                Console.WriteLine(reportString, methods, "methods (including getters and setters)");
                Console.WriteLine(reportString, fields, "fields");

                Console.WriteLine();

                Console.WriteLine("Saving a copy of the modified assembly ...");

                try
                {
                    var directory = Path.GetDirectoryName(output);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    module.Write(output);
                }
                catch (Exception e)
                {
                    Console.WriteLine();
                    Console.WriteLine("ERROR! Cannot create/overwrite the new assembly. ");
                    Console.WriteLine("Please check the path and its permissions " +
                                      "and in case of overwriting an existing file ensure that it isn't currently used.");
                    Console.WriteLine(e);
                    Environment.Exit(50);
                }

                Console.WriteLine("Completed.");
            });

            return await rootCommand.InvokeAsync(args);
        }
    }
}