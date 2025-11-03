using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FuzzySharp;

namespace FuzzyMatchTool
{
    class Program
    {
        // --------------------------
        // Configurable normalization helpers
        // --------------------------

        private static readonly string[] _exclusions = {
            "mr", "mrs", "ms", "dr", "prof", "sir", "madam",
            "pty", "ltd", "inc", "co", "company", "cc", "the"
        };

        private static readonly Dictionary<string, string> _fuzzyReplacementDictionary = new()
        {
            { @"\b&\b", "and" },
            { @"\bco\b", "company" },
            { @"\bcorp\b", "corporation" },
            { @"\bintl\b", "international" },
            { @"\bmfg\b", "manufacturing" },
            { @"\bplc\b", "public limited company" },
            { @"\blimited\b", "ltd" }
        };

        static int Main(string[] args)
        {
            var argDict = ParseArgs(args);
            if (!argDict.ContainsKey("-s") || !argDict.ContainsKey("-l") ||
                !argDict.ContainsKey("-c") || !argDict.ContainsKey("-m"))
            {
                Console.Error.WriteLine("Usage: FuzzyMatchTool.exe -s <source> -l <list> -c <confidence> -m <method>");
                return 2;
            }

            string name = Normalize(argDict["-s"]);
            var list = argDict["-l"].Split('|').Select(Normalize).ToList();
            string method = argDict["-m"].ToLowerInvariant();
            int confidence = Math.Clamp(int.Parse(argDict["-c"]), 0, 100);

            // helper to compute score based on method name
            int GetScoreByName(string methodName, string a, string b)
            {
                return methodName switch
                {
                    "ratio" => Fuzz.Ratio(a, b),
                    "partialratio" => Fuzz.PartialRatio(a, b),
                    "tokensortratio" => Fuzz.TokenSortRatio(a, b),
                    "tokensetratio" => Fuzz.TokenSetRatio(a, b),
                    "partialtokensortratio" => Fuzz.PartialTokenSortRatio(a, b),
                    "partialtokensetratio" => Fuzz.PartialTokenSetRatio(a, b),
                    _ => throw new ArgumentException($"Unknown method '{methodName}'.")
                };
            }

            // Step 1: Run fuzzy comparison manually
            var results = list
                .Select((candidate, index) => new
                {
                    Value = candidate,
                    Score = GetScoreByName(method, name, candidate),
                    Index = index
                })
                .ToList();

            // Step 2: Find best score and check for collisions
            int maxScore = results.Select(r => r.Score).DefaultIfEmpty(0).Max();
            bool hasCollision = results.Count(r => r.Score == maxScore) > 1;

            // Step 3: Return JSON result
            if (hasCollision || maxScore < confidence)
            {
                Console.WriteLine($"{{ \"match\": false, \"score\": {maxScore}, \"index\": -1 }}");
                return 1;
            }

            var best = results.First(r => r.Score == maxScore);
            Console.WriteLine($"{{ \"match\": true, \"score\": {best.Score}, \"index\": {best.Index}, \"value\": \"{best.Value}\" }}");
            return 0;
        }

        // --------------------------
        // Matching helpers
        // --------------------------

        static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            input = input.ToLowerInvariant();

            // Replace special characters and punctuation
            input = Regex.Replace(input, @"[^a-z0-9\s&]", " ");
            input = Regex.Replace(input, @"\s+", " ").Trim();

            // Apply known fuzzy replacements
            foreach (var kv in _fuzzyReplacementDictionary)
                input = Regex.Replace(input, kv.Key, kv.Value, RegexOptions.IgnoreCase);

            // Remove exclusions
            foreach (var word in _exclusions)
                input = Regex.Replace(input, $@"\b{word}\b", "", RegexOptions.IgnoreCase);

            // Final cleanup
            input = Regex.Replace(input, @"\s+", " ").Trim();
            return input;
        }

        static Dictionary<string, string> ParseArgs(string[] args)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].StartsWith("-") && !args[i + 1].StartsWith("-"))
                    dict[args[i]] = args[i + 1];
            }
            return dict;
        }
    }
}
