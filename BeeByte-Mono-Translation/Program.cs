using Mono.Cecil;
using System;

namespace MonoNameTranslation
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Args checks
            if (args.Length != 2 && args.Length != 3)
            {
                Console.WriteLine("BB-Mono-Translation usage: BB-Mono-Translation.exe <path-to-mono-dlls> <path-to-nameTranslation> <OPTIONAL-dll-name>");
                Console.WriteLine("If DLL name is not supplied, Assembly-CSharp.dll will be used as default.");
                return;
            }
            if (!Directory.Exists(args[0]))
            {
                Console.WriteLine("Incorrect <path-to-mono-dlls>");
                return;
            }
            if (!File.Exists(args[1]))
            {
                Console.WriteLine("Incorrect <path-to-nameTranslation>");
                return;
            }

            Console.WriteLine("Starting Deobfuscation...");
            Deobfuscator deobfscator = new Deobfuscator(args[0], args[1]);
            string dllName;
            if (args.Length == 2)
            {
                dllName = "Assembly-CSharp.dll";
            }
            else
            {
                dllName = args[2];
            }
            deobfscator.Deobfuscate(args[0] + dllName);
            Console.WriteLine("Done! " + Path.GetFileNameWithoutExtension(dllName) + "_cleaned.dll has been made.");
        }
    }
    
    public class Translations
    {
        public Dictionary<string, string> _translations;

        public bool ReverseOrder { get; set; }

        public bool ClassesObfuscated { get; set; }

        public bool MethodsObfuscated { get; set; }

        public bool FieldsObfuscated { get; set; }

        public bool PropertiesObfuscated { get; set; }

        public bool EventsObfuscated { get; set; }

        public bool ParametersObfuscated { get; set; }

        //public string[] Hashes = new string[1];
        public Translations(string path)
        {
            _translations = new Dictionary<string, string>();

            string[] translationText = File.ReadAllLines(path);
            foreach (string t in translationText)
            {   
                // To-Do: Support ReverseOrder = False
                if (t.Contains("#ReverseOrder"))
                {
                    ReverseOrder = true;
                    continue;
                }

                if (t.Contains("#Classes"))
                {
                    ClassesObfuscated = true;
                    continue;
                }

                if (t.Contains("#Methods"))
                {
                    MethodsObfuscated = true;
                    continue;
                }

                if (t.Contains("#Fields"))
                {
                    FieldsObfuscated = true;
                    continue;
                }

                if (t.Contains("#Properties"))
                {
                    PropertiesObfuscated = true;
                    continue;
                }

                if (t.Contains("#Events"))
                {
                    EventsObfuscated = true;
                    continue;
                }

                if (t.Contains("#Parameters"))
                {
                    ParametersObfuscated = true;
                    continue;
                }

                /*if (t.Contains("#Hashes"))
                {   
                    // To-Do: Make multiple hashes work
                    string[] HashesLine = t.Split(" ");
                    foreach (string hash in HashesLine)
                    {
                        Hashes.Append(hash);
                    }
                    continue;
                }*/

                if (t.Contains('⇨'))
                {
                    if (ReverseOrder)
                    {
                        _translations.Add(t.Split("⇨")[0], t.Split("⇨")[1]);
                    }
                    else
                    {
                        _translations.Add(t.Split("⇨")[1], t.Split("⇨")[0]);
                    }
                    continue;
                }
            }
        }
    }

    class Deobfuscator
    {
        private List<MethodDefinition> fakeMethods = new List<MethodDefinition>();

        private Translations translations;

        private string ddlsPath;

        public Deobfuscator(string DLLsPath, string nameTranslationPath)
        {
            ddlsPath = DLLsPath;
            translations = new Translations(nameTranslationPath);
        }
        
        public void Deobfuscate(string fileName)
        {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(ddlsPath);

            ModuleDefinition module = ModuleDefinition.ReadModule(fileName, new ReaderParameters { AssemblyResolver = resolver });
            foreach (TypeDefinition type in module.Types)
            {
                DeobfuscateType(type);
                //Console.WriteLine(type.FullName);
            }
            RemoveFakeMethods();
            module.Write("./"+Path.GetFileNameWithoutExtension(fileName)+"_cleaned.dll");
        }

        private void DeobfuscateType(TypeDefinition type)
        {
            if (translations.ClassesObfuscated)
            {
                if (translations._translations.TryGetValue(type.Name, out string typeTranslation))
                {   
                    // BB uses "/" as a seperator for nested obfuscation. We can simply use the last element for real name.
                    if (typeTranslation.Contains("/"))
                    {
                        typeTranslation = typeTranslation.Split("/").Last();
                    }
                    type.Name = typeTranslation;
                }
            }

            if (translations.FieldsObfuscated)
            {
                foreach (FieldDefinition field in type.Fields)
                {
                    if (translations._translations.TryGetValue(field.Name, out string fieldTranslation))
                    {
                        field.Name = fieldTranslation;
                    }
                }
            }

            if (translations.PropertiesObfuscated)
            {
                foreach (PropertyDefinition property in type.Properties)
                {
                    if (translations._translations.TryGetValue(property.Name, out string propertyTranslation))
                    {
                        property.Name = propertyTranslation;
                    }
                }
            }

            if (translations.MethodsObfuscated)
            {
                foreach (MethodDefinition method in type.Methods)
                {
                    if (translations._translations.TryGetValue(method.Name, out string methodTranslation))
                    {
                        // BB only really makes fake code that call other fake code.
                        // None of the fake code is actually called by real code, so we can safely remove them.
                        if (methodTranslation.Contains("BB_OBFUSCATOR"))
                        {
                            fakeMethods.Add(method);
                        }
                        method.Name = methodTranslation;
                    }
                    if (translations.ParametersObfuscated)
                    {
                        foreach (ParameterDefinition parameter in method.Parameters)
                        {
                            if (translations._translations.TryGetValue(parameter.Name, out string varTranslation))
                            {
                                parameter.Name = varTranslation;
                            }
                        }
                    }
                }
            }

            if (translations.EventsObfuscated)
            {
                foreach (EventDefinition _event in type.Events)
                {
                    if (translations._translations.TryGetValue(_event.Name, out string methodTranslation))
                    {
                        _event.Name = methodTranslation;
                    }
                }
            }
            
            foreach (TypeDefinition nestedType in type.NestedTypes)
            {
                DeobfuscateType(nestedType);
            }
        }

        private void RemoveFakeMethods()
        {
            foreach (MethodDefinition fake in fakeMethods)
            {
                fake.DeclaringType.Methods.Remove(fake);
            }
        }
    }
}