using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using FuzzySharp;

namespace FuzzyMatchTool
{
    class Program
    {
        // --------------------------
        // Exit codes
        // --------------------------
        private const int EXIT_SUCCESS = 0;
        private const int EXIT_NO_MATCH = 1;
        private const int EXIT_INVALID_ARGS = 2;
        private const int EXIT_RUNTIME_ERROR = 3;

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
            try
            {
                var argDict = ParseArgs(args);
                if (!argDict.ContainsKey("-s") || !argDict.ContainsKey("-l") ||
                    !argDict.ContainsKey("-c") || !argDict.ContainsKey("-m"))
                {
                    Console.Error.WriteLine("Usage: FuzzyMatchTool.exe -s <source> -l <list> -c <confidence> -m <method>");
                    return EXIT_INVALID_ARGS;
                }

                // Validate and parse confidence parameter
                if (!int.TryParse(argDict["-c"], out int confidence))
                {
                    Console.Error.WriteLine($"Error: Confidence must be a valid integer (0-100). Got: '{argDict["-c"]}'");
                    return EXIT_INVALID_ARGS;
                }
                confidence = Math.Clamp(confidence, 0, 100);

                string name = Normalize(argDict["-s"]);
                var list = argDict["-l"].Split('|').Select(Normalize).ToList();
                string method = argDict["-m"].ToLowerInvariant();

                // Validate list is not empty
                if (list.Count == 0 || list.All(string.IsNullOrWhiteSpace))
                {
                    Console.Error.WriteLine("Error: List of candidates cannot be empty.");
                    return EXIT_INVALID_ARGS;
                }

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
                    var noMatchResult = new { match = false, score = maxScore, index = -1 };
                    Console.WriteLine(JsonSerializer.Serialize(noMatchResult));
                    return EXIT_NO_MATCH;
                }

                var best = results.First(r => r.Score == maxScore);
                var matchResult = new { match = true, score = best.Score, index = best.Index, value = best.Value };
                Console.WriteLine(JsonSerializer.Serialize(matchResult));
                return EXIT_SUCCESS;
            }
            catch (ArgumentException ex)
            {
                // Handle known validation errors (e.g., unknown method)
                Console.Error.WriteLine($"Error: {ex.Message}");
                return EXIT_INVALID_ARGS;
            }
            catch (Exception ex)
            {
                // Handle unexpected runtime errors
                Console.Error.WriteLine($"Unexpected error: {ex.Message}");
                return EXIT_RUNTIME_ERROR;
            }
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
            // Loop stops at args.Length - 1 to prevent index out of bounds when checking args[i + 1]
            // This intentionally skips the last argument if it's a flag without a value
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].StartsWith("-") && !args[i + 1].StartsWith("-"))
                    dict[args[i]] = args[i + 1];
            }
            return dict;
        }
    }
}
