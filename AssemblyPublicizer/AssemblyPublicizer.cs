using System;
using System.IO;
using System.Linq;
using CommandLine;
using dnlib.DotNet;

namespace CabbageCrow.AssemblyPublicizer
{
    public class Options
    {
        [Value(0, MetaName = "input", Required = true, HelpText = "Path (relative or absolute) to the input assembly")]
        public string Input { get; set; }

        [Option('o', "output", Required = false, HelpText = "Path/dir/filename for the output assembly")]
        public string Output { get; set; }
    }

    public class AssemblyPublicizer
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    const string suffix = "_publicized";
                    const string defaultOutputDir = "publicized_assemblies";

                    o.Output ??= defaultOutputDir + Path.DirectorySeparatorChar;

                    if (string.IsNullOrEmpty(Path.GetFileName(o.Output)))
                    {
                        o.Output = Path.Combine(o.Output!, Path.GetFileNameWithoutExtension(o.Input) + suffix + Path.GetExtension(o.Input));
                    }

                    if (!File.Exists(o.Input))
                    {
                        Console.WriteLine();
                        Console.WriteLine("ERROR! File doesn't exist or you don't have sufficient permissions.");
                        Environment.Exit(30);
                    }

                    ModuleDefMD module = null;

                    try
                    {
                        module = ModuleDefMD.Load(o.Input, ModuleDef.CreateModuleContext());
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
                        var directory = Path.GetDirectoryName(o.Output);
                        if (!string.IsNullOrEmpty(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        module.Write(o.Output);
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
        }
    }
}