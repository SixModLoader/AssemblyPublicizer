# Assembly Publicizer

## What is it for?
  
A tool to create a copy of an assembly in which **all members are public** (types, methods, fields, getters and setters of properties).  
  
The intended usage is for modding in Unity(*), because this way you can **access everything normally without the use of reflection** or some helper classes.  
If you use the modified publicized libary in your references and **compile your dll with "Allow unsafe code" enabled**, 
the access even works with the original assembly fine where the member still are private.  
Without "Allow unsafe code" you get (sometimes?) an access violation exception during runtime when accessing private members except for types.  
This way you get the full features of your IDE, like **auto completion** and you don't have to worry about cumbersome stuff like 
creating an instance of an private nested class to use as an parameter for a private method.  
  
(*) It probably works for other instance than Unity too, maybe it's dependent if the software/game uses Mono? If you know more about it I would be happy to hear about it. :-)
  
## Usage
You can drop your target dll onto the .exe (on Windows) or use the command line.  
The **first argument** is the path to the **target assembly** (absolute or relative).  
  
### How to "Allow unsafe code" in Visual Studio
See the following link:  
https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/unsafe-compiler-option  
  
## Command line options
Usage: AssemblyPublicizer.exe [Options]+  
An input path must be provided, the other options are optional.  
You can use it without the option identifiers; If so, the first argument is for input and the optional second one for output.  

Options:

|  -short, --long            | Description                                       |
| -------------------------- | ------------------------------------------------- |
|  input (pos. 0)            | Path (relative or absolute) to the input assembly |
|  -o, --output              | Path/dir/filename for the output assembly         |
|  --help                    | Display help screen.                              |
