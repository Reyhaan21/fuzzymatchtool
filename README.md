# FuzzyMatchTool

**FuzzyMatchTool** is a lightweight .NET console utility that compares two strings using fuzzy logic (via the [FuzzySharp](https://www.nuget.org/packages/FuzzySharp) library).  
It determines whether two strings are a match based on a specified **confidence threshold** and **matching method**.

---

## Features

- Supports multiple fuzzy matching algorithms:

| Method | Description |
| ------- | ----------- |
| **ratio** | Basic Levenshtein distance ratio (standard fuzzy score). |
| **partialratio** | Compares the best matching substring. Useful for partial overlaps. |
| **tokensortratio** | Ignores word order by sorting tokens before comparison. |
| **tokensetratio** | Ignores word order and duplicate tokens. Best for multi-word phrases. |
| **partialtokensortratio** | Compares strings after sorting their words alphabetically, then finds the best-matching substring — great for reordered phrases with extra text. |
| **partialtokensetratio** | Compares only the overlapping words between strings and finds the best substring match — best for messy names with extra or missing words. |

- Command-line driven — ideal for scripts, pipelines, and automation.
- Structured JSON output for easy parsing and integration.
- Robust error handling with exit codes.

## Usage

FuzzyMatchTool.exe -s <source> -l <list> -c <confidence> -m <method>

| Flag | Description | Example |
| ---- | ------------ | -------- |
| **-s** | Source string | `-s "apple"` |
| **-l** | List of candidate strings (pipe `\|` separated) | `-l "appl\|appel\|aaple"` |
| **-c** | Confidence level (0–100) | `-c 80` |
| **-m** | Matching method (`ratio`, `partialratio`, `tokensortratio`, `tokensetratio`, `partialtokensortratio`, `partialtokensetratio`) | `-m ratio` |


e.g. "C:\FuzzyMatch\FuzzyMatchTool.exe" -s "Test string" -l "test strong" -c 90 -m ratio

## Output Format

The tool outputs JSON to stdout:

**On successful match:**
```json
{ "match": true, "score": 92, "index": 0, "value": "test strong" }
```

**On no match or collision:**
```json
{ "match": false, "score": 75, "index": -1 }
```

- `match`: Boolean indicating whether a match was found above the confidence threshold
- `score`: The highest fuzzy match score (0-100)
- `index`: Zero-based index of the matched candidate in the list (-1 if no match)
- `value`: The matched value from the list (only present when match is true)

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