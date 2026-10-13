# Low-Level Design (LLD) — Standard Terminal Hello World

| Field | Value |
| :-- | :-- |
| Project | `00-hello-world` |
| Tools | dp-hello |
| Implementation language | C# 14 on .NET 10 (LTS) |
| Author | Brandon Perkins (with Claude Code) |
| Version / date | 1.0 — 2026-10-13 |
| Status | Approved |
| Inputs | `hld.md`, `specs/SRS.md`, `shared/CLI-CONVENTIONS.md` |

## 1. Source layout

```text
src/
├── DpHello.slnx                 solution (three projects)
├── global.json                  SDK pin (10.0.100, rollForward latestFeature); MTP test runner
├── Directory.Build.props        shared settings: net10.0, Nullable, warnings as errors, analyzers, lock files
├── .editorconfig                C# style read by the compiler analyzers and dotnet format
├── DpHello.Core/                class library (namespace DialecticPlayground.Hello)
│   ├── CommandLine.cs           OptionValue, ParseOutcome, CommandLine.Parse
│   ├── Validator.cs             Settings, ValidationOutcome, Validator.Validate / TryParseCount
│   ├── Greeting.cs              Greeting.Line (pure), DefaultName
│   ├── Output.cs                Output.Write / WriteRepeated (UTF-8 bytes to a Stream)
│   ├── Messages.cs              help, version, CONV-13 messages, CONV-14 hint
│   └── Application.cs           Application.Run: the program as a function
├── DpHello.Cli/                 console app; AssemblyName dp-hello
│   └── Program.cs               opens the raw standard streams, calls Application.Run
└── DpHello.Tests/               xUnit v3 tests (one file per Core unit)
    └── CommandLineTests.cs, ValidatorTests.cs, GreetingTests.cs, ApplicationTests.cs
```

Each project has a committed `packages.lock.json`. `make all` publishes `DpHello.Cli` to
`00-hello-world/bin/dp-hello` (`dp-hello.exe` on Windows).

## 2. Data types

### 2.1 `OptionValue(string Option, string Value)`
| Field | Type | Constraint / invariant | SRS |
| :-- | :-- | :-- | :-- |
| `Option` | `string` | The option as written: `-n`, `--name`, `-c`, or `--count` | FR-03, FR-05, CONV-13 |
| `Value` | `string` | Raw value, unvalidated; may be empty | FR-02, FR-04 |

### 2.2 `ParseOutcome` (closed: private constructor, sealed nested records)
| Variant | Fields | Meaning |
| :-- | :-- | :-- |
| `HelpRequested` | — | `--help` reached first (FR-07) |
| `VersionRequested` | — | `--version` reached first (FR-08) |
| `SyntaxError` | `Message` | Unknown option, missing argument, or `=value` on a flag (FR-10) |
| `Arguments` | `Name?`, `Count?` (`OptionValue`), `Operands` (`IReadOnlyList<string>`) | Scan completed; last occurrence of each option (FR-06) |

### 2.3 `Settings(string Name, int Count)`
Invariants: `Name` is non-empty; `1 <= Count <= 1000` (FR-03, FR-04).

### 2.4 Error type: `ValidationOutcome`
`Valid(Settings)` or `Invalid(Message)`. Every usage error, from either the scan or the
validation, is reported identically by `Application.Run`:

| Variant / source | Diagnostic (stderr) | Exit status |
| :-- | :-- | :-- |
| `SyntaxError` / unknown short option | `dp-hello: invalid option -- 'x'` + hint | 2 |
| `SyntaxError` / unknown long option | `dp-hello: unrecognized option '<token>'` + hint | 2 |
| `SyntaxError` / `-n`, `-c` without value | `dp-hello: option requires an argument -- 'n'` + hint | 2 |
| `SyntaxError` / `--name`, `--count` without value | `dp-hello: option '--name' requires an argument` + hint | 2 |
| `SyntaxError` / `--help=…`, `--version=…` | `dp-hello: option '--help' doesn't allow an argument` + hint | 2 |
| `Invalid` / operand | `dp-hello: extra operand '<first>'` + hint | 2 |
| `Invalid` / empty name | `dp-hello: invalid argument '' for '<-n or --name>'` + hint | 2 |
| `Invalid` / bad count | `dp-hello: invalid argument '<value>' for '<-c or --count>'` + hint | 2 |

The hint is `Try 'dp-hello --help' for more information.`; both lines end in LF.

## 3. Module and function specifications

### 3.1 `CommandLine.Parse`
- **Signature:** `public static ParseOutcome Parse(IReadOnlyList<string> args)`
- **Responsibility:** perform the CONV-12 scan.
- **Preconditions:** `args` is not null (guarded) and excludes the program name.
- **Postconditions:** returns the first of help, version, or syntax error met while scanning;
  otherwise `Arguments` with the last value of each option and all operands in order.
- **Errors:** `SyntaxError` as in §2.4; never throws for any argument content.
- **Algorithm:** see §4.1.
- **Complexity:** O(total argument length) time; O(number of operands) memory.
- **Satisfies:** FR-02, FR-06, FR-07, FR-08, FR-10, CONV-07 … CONV-13, CONV-22.
- **Unit tests:** `CommandLineTests.Parse_NoArguments_RecordsNothing`,
  `Parse_Name_AllSpellings`, `Parse_Count_RecordsRawValue`,
  `Parse_BundledValues_ConsumeRestOfToken`, `Parse_RepeatedOptions_LastWins`,
  `Parse_Operands_AreCollectedInterleaved`, `Parse_Terminator_MakesEverythingAfterItOperands`,
  `Parse_Help_WinsWhenReachedFirst`, `Parse_Version_WinsWhenReachedFirst`,
  `Parse_SyntaxErrors_UseConv13Messages`.

### 3.2 `Validator.Validate`
- **Signature:** `public static ValidationOutcome Validate(ParseOutcome.Arguments arguments)`
- **Responsibility:** check operands and the winning option values after the scan.
- **Postconditions:** `Valid(Settings)` satisfying §2.3, or `Invalid` with the first error in
  the order operands → name → count (the reference implementation's order).
- **Algorithm:** (1) any operand → `extra operand` for the first; (2) name = value or `World`;
  empty → `invalid argument '' for '<option>'`; (3) count = 1, or `TryParseCount(value)`;
  failure → `invalid argument '<value>' for '<option>'`.
- **Complexity:** O(length of COUNT) time, O(1) memory.
- **Satisfies:** FR-03, FR-04, FR-05, FR-06, FR-09, CONV-12, CONV-13, CONV-15.
- **Unit tests:** `ValidatorTests.Validate_Defaults`, `Validate_CountWithinRange`,
  `Validate_InvalidCount`, `Validate_Name`, `Validate_Operands_ReportTheFirstBeforeValueErrors`,
  `Validate_EmptyName_ReportedBeforeInvalidCount`.

### 3.3 `Validator.TryParseCount`
- **Signature:** `public static bool TryParseCount(string text, out int count)`
- **Algorithm:** false if empty; for each UTF-16 unit, false unless `char.IsAsciiDigit`;
  accumulate `min(count * 10 + digit, 1001)` (saturating, so 20-digit values cannot
  overflow); true iff `1 <= count <= 1000`. Leading zeros are accepted (`007` = 7).
- **Satisfies:** FR-04, FR-05. **Unit tests:** `Validate_CountWithinRange`,
  `Validate_InvalidCount`, `TryParseCount_RejectsNull`.

### 3.4 `Greeting.Line`
- **Signature:** `public static string Line(string name)`
- **Postconditions:** returns `"Hello, " + name + "!\n"`; pure and total.
- **Satisfies:** FR-01, FR-02, NFR-05. **Unit tests:** `GreetingTests.*`.

### 3.5 `Output.WriteRepeated` / `Output.Write`
- **Signature:** `public static void WriteRepeated(Stream stream, string text, int times)`;
  `Write(stream, text)` calls it with `times = 1`.
- **Algorithm:** encode `text` once with `UTF8Encoding(false)`; wrap `stream` in a 64 KiB
  `BufferedStream`; write the bytes `times` times; flush.
- **Errors:** propagates `IOException` from the stream (handled by `Application.Run`).
- **Complexity:** O(times × bytes) time; O(bytes + 64 KiB) memory.
- **Satisfies:** CONV-03, CONV-05, NFR-01, NFR-02. **Unit tests:**
  `ApplicationTests.Run_NonAsciiName_IsWrittenAsUtf8WithoutBom`,
  `Run_MaximumCount_WritesExactlyThatManyLfTerminatedLines`.

### 3.6 `Application.Run`
- **Signature:** `public static int Run(IReadOnlyList<string> args, Stream stdout, Stream stderr)`
- **Responsibility:** the whole program, minus opening the real streams.
- **Algorithm:** map `CommandLine.Parse(args)` (and, for `Arguments`, `Validator.Validate`)
  to `(text, times, error)`: help → (help, 1), version → (version, 1), valid →
  (`Greeting.Line(name)`, count), syntax/invalid → error. If error: write
  `Messages.UsageDiagnostic(error)` to stderr, return 2. Else `Output.WriteRepeated`; on
  `IOException` return 141 without a message; else return 0.
- **Satisfies:** FR-01 … FR-11, §3.7, CONV-14, CONV-24, CONV-25.
- **Unit tests:** `ApplicationTests.Run_Success`, `Run_Help_PrintsTheManualTextExactly`,
  `Run_UsageErrors_WriteTwoLinesToStderrOnly`, `Run_BrokenPipe_StopsSilently`, and the two
  output tests in §3.5.

### 3.7 `Messages`
Constants `ToolName`, `HelpText`, `VersionLine` and one formatting method per CONV-13 row
(`InvalidOption`, `UnrecognizedOption`, `ShortOptionRequiresArgument`,
`LongOptionRequiresArgument`, `OptionDoesNotAllowArgument`, `InvalidArgument`, `ExtraOperand`)
plus `UsageDiagnostic(message)`. Values are inserted verbatim (CONV-15). Verified through the
parser, validator, and application tests above.

### 3.8 `Program` (top-level statements, `DpHello.Cli`)
Opens `Console.OpenStandardOutput()` and `Console.OpenStandardError()` and returns
`Application.Run(args, stdout, stderr)` as the exit status. Verified by P01–P10.

## 4. Algorithms

### 4.1 The scan (CONV-07 … CONV-12)
```text
i = 0
while i < len(args):
    t = args[i++]
    if t == "--":            operands += args[i..]; break
    if t starts with "--":   name, inline = split t[2..] at the first "="
        help|version with inline   -> SyntaxError(doesn't allow an argument)
        help / version             -> HelpRequested / VersionRequested   (return immediately)
        name|count: value = inline ?? args[i++] ?? SyntaxError(requires an argument)
                    record (option "--" + name, value)                    (last wins)
        otherwise                  -> SyntaxError(unrecognized option 't')
    elif len(t) > 1 and t[0] == '-':
        letter = t[1] (a whole surrogate pair if it is one)
        not n/c -> SyntaxError(invalid option -- 'letter')
        value = t[2..] if non-empty else args[i++] ?? SyntaxError(requires an argument -- 'letter')
        record (option "-" + letter, value)
    else: operands += t        (includes "-")
return Arguments(name, count, operands)
```
Both short options take a value, so a bundle always ends at its first letter (`-c2` → value
`2`; `-cn5` → count `n5`, rejected later). Values may start with `-` when attached or given
as the next token. Long names must match exactly (no abbreviations, CONV-08).

## 5. Platform-specific details

| Concern | Linux | macOS | Windows | Abstraction |
| :-- | :-- | :-- | :-- | :-- |
| Arguments → `string[]` | runtime decodes `argv` as UTF-8 | same | runtime takes the UTF-16 command line | .NET runtime |
| Binary stdout/stderr | raw fd streams | raw fd streams | raw handle streams (`WriteFile`), no CRLF translation | `Console.OpenStandard*` |
| Executable | `bin/dp-hello` (apphost, linux-x64/arm64) | `bin/dp-hello` (osx-x64/arm64) | `bin/dp-hello.exe` (win-x64/arm64) | `UseCurrentRuntimeIdentifier`, `$(EXE)` |
| Runtime lookup | default `/usr/share/dotnet` | registered `/etc/dotnet/install_location` (CI) or default | default `C:\Program Files\dotnet` | apphost |
| Broken pipe | `IOException` (EPIPE; .NET ignores SIGPIPE) | same | `IOException` (`ERROR_NO_DATA`) | `Application.Run` |

## 6. Resource management
The two standard streams are opened once in `Program` with `using` and disposed on return.
`Output` holds one encoded line plus a 64 KiB buffer and flushes it on every successful path.
Peak managed allocation is O(length of NAME), independent of COUNT. No files, threads, or
child processes are created.

## 7. Sequence of one invocation
Public test P06, `dp-hello -c2 -n Ken`:
1. The apphost starts the runtime; `Program` opens stdout/stderr and calls `Application.Run`.
2. `CommandLine.Parse`: `-c2` → short `c`, rest `2` → record `(-c, "2")`; `-n` → short `n`,
   rest empty → value `Ken` from the next token → record `(-n, "Ken")`. End:
   `Arguments((-n, Ken), (-c, 2), [])`.
3. `Validator.Validate`: no operands; name `Ken` non-empty; `TryParseCount("2")` → 2 →
   `Valid(Settings("Ken", 2))`.
4. `Greeting.Line("Ken")` → `"Hello, Ken!\n"`; `Output.WriteRepeated(stdout, …, 2)` writes
   24 bytes; `Run` returns 0, which becomes the exit status.

## 8. Design-to-code checklist

| HLD component | LLD section | Source file(s) | Unit-test file(s) |
| :-- | :-- | :-- | :-- |
| `CommandLine` | §3.1, §4.1 | `DpHello.Core/CommandLine.cs` | `DpHello.Tests/CommandLineTests.cs` |
| `Validator` | §3.2, §3.3 | `DpHello.Core/Validator.cs` | `DpHello.Tests/ValidatorTests.cs` |
| `Greeting` | §3.4 | `DpHello.Core/Greeting.cs` | `DpHello.Tests/GreetingTests.cs` |
| `Output` | §3.5 | `DpHello.Core/Output.cs` | `DpHello.Tests/ApplicationTests.cs` |
| `Messages` | §3.7 | `DpHello.Core/Messages.cs` | `CommandLineTests.cs`, `ValidatorTests.cs`, `ApplicationTests.cs` |
| `Application` | §3.6 | `DpHello.Core/Application.cs` | `DpHello.Tests/ApplicationTests.cs` |
| `Program` | §3.8 | `DpHello.Cli/Program.cs` | black-box P01–P10 |
