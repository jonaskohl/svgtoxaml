using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Xsl;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("SvgToXaml")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("SvgToXaml")]
[assembly: AssemblyCopyright("Copyright © 2022 Jonas Kohl")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("9023ce33-3ce4-4c0b-9277-7ad2d5681358")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace SvgToXaml
{
    class Program
    {
        static readonly string[] commandSwitches = new string[] { "F", "M", "?", "H", "HELP" };
        static readonly string[] flagSwitches = new string[] { "SILVERLIGHT" };
        static readonly string[] knownSwitches = commandSwitches.Concat(flagSwitches).ToArray();
        
        static int Main(string[] args)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var copyright = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).LegalCopyright.Replace("\u00A9", "(C)");
            
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var xslPath = Path.Combine(baseDir, "svgtoxaml.xsl");

            if (!File.Exists(xslPath))
            {
                Console.Error.WriteLine("Error: Missing XSL at " + xslPath);
                return 1;
            }

            if (args.Length < 1)
            {
                Console.Error.WriteLine("Error: Too few arguments");
                return 2;
            }
            
            var switches = new List<string>();
            
            var paramIndex = 0;
            for (var i = 0; i < args.Length; ++i)
            {
                var arg = args[i];
                
                if (arg.StartsWith("/") || arg.StartsWith("-"))
                    switches.Add(arg.ToUpper().Substring(1));
                else
                {
                    paramIndex = i;
                    break;
                }
            }

            if (switches.Count < 1)
            {
                Console.Error.WriteLine("Error: Too few arguments");
                return 2;
            }
            
            if (switches.Where(i => commandSwitches.Contains(i)).Count() > 1)
            {
                Console.Error.WriteLine("Error: Specified multiple commands");
                return 7;
            }
            
            if (switches.Any(i => !knownSwitches.Contains(i))) {
                foreach (var sw in switches.Where(i => !knownSwitches.Contains(i)))
                    Console.Error.WriteLine("Warning: Unknown command line switch: " + sw);
            }

            var command = switches.First(i => commandSwitches.Contains(i));

            var xargs = new XsltArgumentList();
            xargs.AddParam("silverlight", "", switches.Contains("SILVERLIGHT"));

            var xslt = new XslCompiledTransform();
            xslt.Load(xslPath);
            
            var pargs = args.Skip(paramIndex).ToArray();

            if (command == "F")
            {
                if (pargs.Length < 1)
                {
                    Console.Error.WriteLine("Error: Too few arguments");
                    return 2;
                }
                if (pargs.Length > 2)
                {
                    Console.Error.WriteLine("Error: Too many arguments");
                    return 3;
                }

                var inputFile = pargs[0];
                var outputFile = pargs[1];

                if (!File.Exists(inputFile) || !FileIsReadable(inputFile))
                {
                    Console.Error.WriteLine("Error: Cannot open input file");
                    return 4;
                }

                Transform(xslt, xargs, inputFile, outputFile);
            }
            else if (command == "M")
            {
                if (pargs.Length < 2)
                {
                    Console.Error.WriteLine("Error: Too few arguments");
                    return 2;
                }
                if (pargs.Length > 3)
                {
                    Console.Error.WriteLine("Error: Too many arguments");
                    return 3;
                }

                var inputFilePath = pargs[0];
                var inputFilePattern = pargs[1];
                var outputDir = pargs[2];

                if (!Directory.Exists(inputFilePath))
                {
                    Console.Error.WriteLine("Error: Input directory does not exist");
                    return 5;
                }

                if (File.Exists(outputDir))
                {
                    Console.Error.WriteLine("Error: Output directory is a file");
                    return 6;
                }

                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                var files = Directory.GetFiles(inputFilePath, inputFilePattern);
                
                if (files.Length < 1)
                    Console.Error.WriteLine("Warning: No input files found");
                else
                    foreach (var file in files)
                    {
                        Console.WriteLine(file);
                        Transform(xslt, xargs, file, Path.Combine(outputDir, Path.GetFileNameWithoutExtension(file) + ".xaml"));
                    }
            }
            else if (command == "?" || command == "H" || command == "HELP")
            {
                Console.WriteLine("SvgToXaml " + version);
                Console.WriteLine(copyright);
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("svgtoxaml /F [[/SILVERLIGHT]] [Input file] [Output file]");
                Console.WriteLine("svgtoxaml /M [[/SILVERLIGHT]] [Input folder] [Input pattern] [Output folder]");
                Console.WriteLine("svgtoxaml [/?|/H|/HELP]");
            }
            else
            {
                Console.WriteLine("Invalid option: " + command);
                Console.WriteLine("Enter svgtoxaml /? to view the available options");
            }

            return 0;
        }

        private static void Transform(XslCompiledTransform xslt, XsltArgumentList args, string inputFile, string outputFile)
        {
            using (var sw = new StreamWriter(outputFile))
                xslt.Transform(inputFile, args, sw);
        }

        private static bool FileIsReadable(string filename)
        {
            try
            {
                File.Open(filename, FileMode.Open, FileAccess.Read).Dispose();
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }
    }
}
