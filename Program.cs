using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using Newtonsoft.Json;

class Program
{
    static void Main(string[] args)
    {
        var outputOption = new Option<FileInfo>("--output", "File path and name");
        var languageOption = new Option<string[]>("--language", "Language of the code files");
        languageOption.IsRequired = true;
        languageOption.AllowMultipleArgumentsPerToken = true;
        var noteOption = new Option<bool>("--source", "Include code source path");
        var sortOption = new Option<string>("--sort", "Sort by file name or code language");
        var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "Remove empty lines from code files");
        var authorOption = new Option<string>("--author", "Author of the code files");

        outputOption.AddAlias("-o");
        languageOption.AddAlias("-l");
        noteOption.AddAlias("-n");
        sortOption.AddAlias("-s");
        removeEmptyLinesOption.AddAlias("-r");
        authorOption.AddAlias("-a");

        var bundleCommand = new Command("bundle", "Bundle code files into a single file.");

        bundleCommand.AddOption(outputOption);
        bundleCommand.AddOption(languageOption);
        bundleCommand.AddOption(noteOption);
        bundleCommand.AddOption(sortOption);
        bundleCommand.AddOption(removeEmptyLinesOption);
        bundleCommand.AddOption(authorOption);

        bundleCommand.SetHandler(BundleFiles, outputOption, languageOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);

        var createRspCommand = new Command("create-rsp", "Create a response file for the bundle command.");

        createRspCommand.SetHandler(createRsp);

        var rootCommand = new RootCommand("Root Command for file bundler CLI");

        rootCommand.AddCommand(bundleCommand);
        rootCommand.AddCommand(createRspCommand);
        rootCommand.InvokeAsync(args);
    }

    static void BundleFiles(FileInfo output, string[] languages, bool note, string sort, bool removeEmptyLines, string author)
    {
        try
        {
            using (var outputFile = output.CreateText())
            {
                var languageMap = LoadLanguageMapFromFile("C:\\Users\\tzipp\\Documents\\fib\\languages.json");

                foreach (var language in languages)
                {
                    if (!languageMap.ContainsKey(language))
                    {
                        Console.WriteLine($"Invalid language: {language}. Please use valid languages.");
                        return;
                    }
                }

                if (sort != null && sort != "a-z" && sort != "languages")
                {
                    Console.WriteLine($"Invalid sort term: {sort}. Please use 'a-z' or 'languages'.");
                    return;
                }

                var fileExtensions = languages.Select(lang => languageMap.ContainsKey(lang) ? languageMap[lang] : null).Where(ext => ext != null).ToArray();

                var ignoredDirectories = new List<string>
                {
                    "bin",
                    "debug",
                    "node_modules",
                    "obj",
                    ".git",
                };

                var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.*", SearchOption.AllDirectories)
                    .Where(file =>
                    {
                        var directoryPath = Path.GetDirectoryName(file);
                        return !ignoredDirectories.Any(ignoredDir => directoryPath.Contains(ignoredDir));
                    })
                    .Where(file => fileExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                    .ToList();


                if (sort == "languages")
                {
                    files.Sort((file1, file2) =>
                    {
                        string lang1 = languages.FirstOrDefault(lang => file1.EndsWith(languageMap[lang], StringComparison.OrdinalIgnoreCase));
                        string lang2 = languages.FirstOrDefault(lang => file2.EndsWith(languageMap[lang], StringComparison.OrdinalIgnoreCase));
                        if (lang1 != null && lang2 != null)
                        {
                            return lang1.CompareTo(lang2);
                        }
                        return string.Compare(file1, file2);
                    });
                }

                else
                {
                    files.Sort();
                }


                foreach (var file in files)
                {
                    if (note)
                    {
                        outputFile.WriteLine($"Source Path: {file}");
                    }

                    var lines = File.ReadLines(file);

                    if (removeEmptyLines)
                    {
                        lines = lines.Where(line => !string.IsNullOrWhiteSpace(line));
                    }

                    outputFile.WriteLine(string.Join(Environment.NewLine, lines));
                    outputFile.WriteLine();

                }
                if (author != null)
                {
                    outputFile.WriteLine($"Author: {author}");
                    outputFile.WriteLine();
                }

                Console.WriteLine($"Bundling completed. Output written to {output.FullName}");
            }
        }
        catch (DirectoryNotFoundException e)
        {
            Console.WriteLine("Error: Invalid Path!");
        }
    }

    static Dictionary<string, string> LoadLanguageMapFromFile(string fileName)
    {
        if (File.Exists(fileName))
        {
            string json = File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }
        else
        {
            Console.WriteLine("Language map file not found.");
            return new Dictionary<string, string>();
        }
    }

    static void createRsp()
    {
        Console.WriteLine("Bundle Configuration:");

        // Prompt the user for the output file path
        Console.Write("Enter the output file path and name: ");
        string outputFilePath = Console.ReadLine();

        Console.Write("Enter the language(s) of the code files: ");
        string languages = Console.ReadLine();

        Console.Write("Include code source path? (y/n): ");
        string note = Console.ReadLine();

        Console.Write("Sort by file name or code language? (a-z/languages): ");
        string sort = Console.ReadLine();

        Console.Write("Remove empty lines from code files? (y/n): "); 
        string removeEmptyLines = Console.ReadLine();

        Console.Write("Author of the code files: ");
        string author = Console.ReadLine();

        string responseContent = $"bundle\n";
        responseContent += $"--output \"{outputFilePath}\"\n";
        responseContent += $"--language {languages}\n";
        if (note == "y")
        {
            responseContent += $"--source\n";
        }
        responseContent += $"--sort {sort}\n";
        if (removeEmptyLines == "y")
        {
            responseContent += $"--remove-empty-lines\n";
        }
        responseContent += $"--author \"{author}\"";

        File.WriteAllText("bundle.rsp", responseContent);

        Console.WriteLine("Bundle.rsp file created with the configuration.");
    }

}