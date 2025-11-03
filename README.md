# FuzzyMatchTool

**FuzzyMatchTool** is a lightweight .NET console utility that compares two strings using fuzzy logic (via the [FuzzySharp](https://www.nuget.org/packages/FuzzySharp) library).  
It determines whether two strings are a match based on a specified **confidence threshold** and **matching method**.

---

## Features

- Supports multiple fuzzy matching algorithms:

| Method             | Description                                                           |
| ------------------ | --------------------------------------------------------------------- |
| **ratio**          | Basic Levenshtein distance ratio (standard fuzzy score).              |
| **partialratio**   | Compares best matching substring. Useful for partial overlaps.        |
| **tokensortratio** | Ignores word order by sorting tokens before comparison.               |
| **tokensetratio**  | Ignores word order and duplicate tokens. Best for multi-word phrases. |

- Command-line driven — ideal for scripts, pipelines, and automation.
- Minimal output (true / false) for easy parsing.
- Robust error handling with exit codes.

## Usage

FuzzyMatchTool.exe -s <string1> -t <string2> -c <confidence> -m <method>

| Flag | Description                                                                  | Example      |
| ---- | ---------------------------------------------------------------------------- | ------------ |
| -s   | Source string                                                                | -s "apple"   |
| -t   | Target string                                                                | -t "appl"    |
| -c   | Confidence level (0–100)                                                     | -c 80        |
| -m   | Matching method (ratio, partialratio, tokensortratio, tokensetratio)         | -m ratio     |


e.g. C:\FuzzyMatch\FuzzyMatchTool.exe -s Test string -t test strong -c 90 -m ratio

Output: true or false

## Exit Codes

| Code | Meaning                              |
| ---- | ------------------------------------ |
| 0    | Strings matched (score ≥ confidence) |
| 1    | Strings did not match                |
| 2    | Input or argument error              |
| 3    | Unexpected runtime error             |

## Third-Party Licenses

This project uses the following open-source library:

- **FuzzySharp**  
  © Jake Bayer — [MIT License](https://github.com/JakeBayer/FuzzySharp/blob/master/LICENSE)