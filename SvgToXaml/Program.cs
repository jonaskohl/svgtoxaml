using System;
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
[assembly: AssemblyCopyright("Copyright © Jonas Kohl 2022")]
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
        static int Main(string[] args)
        {
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var xslPath = Path.Combine(baseDir, "svg2xaml.xsl");

            if (!File.Exists(xslPath))
            {
                Console.Error.WriteLine("Error: XSLT missing at " + xslPath);
                return 1;
            }

            if (args.Length < 1)
            {
                Console.Error.WriteLine("Error: Too few arguments");
                return 2;
            }

            var xargs = new XsltArgumentList();
            xargs.AddParam("silverlight", "", true);

            var xslt = new XslCompiledTransform();
            xslt.Load(xslPath);

            var command = args[0];

            command = Regex.Replace(command.ToUpper(), @"^[\/\-]", "/");

            if (command == "/F")
            {
                if (args.Length < 2)
                {
                    Console.Error.WriteLine("Error: Too few arguments");
                    return 2;
                }
                if (args.Length > 3)
                {
                    Console.Error.WriteLine("Error: Too many arguments");
                    return 3;
                }

                var inputFile = args[1];
                var outputFile = args[2];

                if (!File.Exists(inputFile) || !FileIsReadable(inputFile))
                {
                    Console.Error.WriteLine("Error: Cannot open input file");
                    return 4;
                }

                Transform(xslt, xargs, inputFile, outputFile);
            }
            else if (command == "/M")
            {
                if (args.Length < 3)
                {
                    Console.Error.WriteLine("Error: Too few arguments");
                    return 2;
                }
                if (args.Length > 4)
                {
                    Console.Error.WriteLine("Error: Too many arguments");
                    return 3;
                }

                var inputFilePath= args[1];
                var inputFilePattern = args[2];
                var outputDir = args[3];

                if (!Directory.Exists(outputDir))
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

                foreach (var file in files)
                {
                    Console.WriteLine(file);
                    Transform(xslt, xargs, file, Path.Combine(outputDir, Path.GetFileNameWithoutExtension(file) + ".xaml"));
                }
            }
            else if (command == "/?" || command == "/H" || command == "/HELP")
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("svgtoxaml /F [Input file] [Output file]");
                Console.WriteLine("svgtoxaml /M [Input folder] [Input pattern] [Output folder]");
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
