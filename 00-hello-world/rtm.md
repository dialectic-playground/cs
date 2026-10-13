# Requirements Traceability Matrix (RTM) — Standard Terminal Hello World

| Field | Value |
| :-- | :-- |
| Project | `00-hello-world` |
| Tools | dp-hello |
| Author | Brandon Perkins (with Claude Code) |
| Version / date | 1.0 — 2026-10-13 |

Code paths are relative to `src/`. Core functions live in namespace
`DialecticPlayground.Hello` (`DpHello.Core/`); unit tests in `DpHello.Tests/`. Public
black-box tests are `P01` … `P10`. The hidden suite runs in CI and is not cited.

## 1. User requirements to system requirements

| User requirement (URD) | Derived system requirements (SRS) |
| :-- | :-- |
| UR-01 | FR-01 |
| UR-02 | FR-02 |
| UR-03 | FR-04 |
| UR-04 | FR-07 |
| UR-05 | FR-08 |
| UR-06 | FR-03, FR-05, FR-09, FR-10, FR-11 |
| UR-07 | FR-01, SRS §3.7 |
| UR-08 | NFR-01, CONV-03 |
| UR-09 | FR-02, CONV-05 |
| UR-10 | FR-06, FR-10, CONV-07 … CONV-15 |
| UR-11 | NFR-02 |
| UR-12 | NFR-06 |

## 2. Forward traceability

| Req. ID | Summary | Design (HLD/LLD) | Code (file::function) | Verification (unit / black-box) | Status |
| :-- | :-- | :-- | :-- | :-- | :-- |
| UR-01 | Classic greeting | HLD §3.3 | see FR-01 | P01 | Verified |
| UR-02 | Choose the name | HLD §3.2 | see FR-02 | P02, P03, P04 | Verified |
| UR-03 | Repeat the greeting | HLD §3.2 | see FR-04 | P05, P06 | Verified |
| UR-04 | Help screen | HLD §3.2 | see FR-07 | P07 | Verified |
| UR-05 | Version | HLD §3.2 | see FR-08 | P08 | Verified |
| UR-06 | Clear usage errors, distinct status | HLD §6 | see FR-03, FR-05, FR-09 … FR-11 | P09, P10 | Verified |
| UR-07 | Exit status 0 on success | HLD §3.3 | `Application.cs::Run` | P01–P08 | Verified |
| UR-08 | Identical bytes on all OSes | HLD §5 | see NFR-01 | P01–P10 on 3 OSes (CI) | Verified |
| UR-09 | Spaces and non-ASCII names | HLD §5 | see FR-02, CONV-05 | `Run_NonAsciiName_IsWrittenAsUtf8WithoutBom` | Verified |
| UR-10 | Common CLI conventions | HLD §2 | see CONV rows | `CommandLineTests.*` | Verified |
| UR-11 | Instant start | HLD §7 | see NFR-02 | Measurement (HLD §7) | Verified |
| UR-12 | Standard install/uninstall | HLD §3 | see NFR-06 | CI `make roundtrip` | Verified |
| FR-01 | Default greeting | HLD §3.3, LLD §3.4 | `Greeting.cs::Line`, `Greeting.DefaultName`, `Validator.cs::Validate` | `Line_DefaultName_IsTheClassicGreeting`, `Validate_Defaults`, `Run_Success`, P01 | Verified |
| FR-02 | Named greeting, verbatim | LLD §3.1, §3.4 | `CommandLine.cs::Parse`, `Greeting.cs::Line` | `Parse_Name_AllSpellings`, `Line_UsesTheNameVerbatim`, `Run_Success`, P02, P03, P04 | Verified |
| FR-03 | Empty name rejected | LLD §3.2 | `Validator.cs::Validate` | `Validate_Name`, `Run_UsageErrors_WriteTwoLinesToStderrOnly` | Verified |
| FR-04 | Repetition 1–1000, leading zeros | LLD §3.3, §3.5 | `Validator.cs::TryParseCount`, `Output.cs::WriteRepeated` | `Validate_CountWithinRange`, `Run_MaximumCount_WritesExactlyThatManyLfTerminatedLines`, P05, P06 | Verified |
| FR-05 | Invalid count rejected | LLD §3.2, §3.3 | `Validator.cs::Validate`, `Validator.cs::TryParseCount` | `Validate_InvalidCount`, `Run_UsageErrors_WriteTwoLinesToStderrOnly` | Verified |
| FR-06 | Last occurrence wins | LLD §4.1 | `CommandLine.cs::Parse` | `Parse_RepeatedOptions_LastWins`, `Run_Success` (`-n A -n B`) | Verified |
| FR-07 | Help, scan order | LLD §3.1, §3.7 | `CommandLine.cs::Parse`, `Messages.HelpText`, `Application.cs::Run` | `Parse_Help_WinsWhenReachedFirst`, `Run_Help_PrintsTheManualTextExactly`, P07 | Verified |
| FR-08 | Version line | LLD §3.7 | `Messages.VersionLine`, `Application.cs::Run` | `Parse_Version_WinsWhenReachedFirst`, `Run_Success`, P08 | Verified |
| FR-09 | Operands rejected after the scan | LLD §3.2 | `CommandLine.cs::Parse`, `Validator.cs::Validate` | `Parse_Terminator_MakesEverythingAfterItOperands`, `Validate_Operands_ReportTheFirstBeforeValueErrors`, P10 | Verified |
| FR-10 | Unknown/malformed options | LLD §4.1 | `CommandLine.cs::Parse`, `Messages.cs` | `Parse_SyntaxErrors_UseConv13Messages`, `Run_UsageErrors_WriteTwoLinesToStderrOnly`, P09 | Verified |
| FR-11 | Nothing on stdout on error | HLD §3.3, LLD §3.6 | `Application.cs::Run` | `Run_UsageErrors_WriteTwoLinesToStderrOnly`, P09, P10 | Verified |
| NFR-01 | Byte-identical, LF only | HLD §5 | `Output.cs::WriteRepeated`, `Program.cs` | `Run_MaximumCount_WritesExactlyThatManyLfTerminatedLines`, P01–P10 on Linux, macOS, Windows (CI) | Verified |
| NFR-02 | < 100/300 ms; < 32 MiB above baseline | HLD §7, D4 | `Output.cs::WriteRepeated` | Measurement: 36 ms, +4.6 MiB over the runtime baseline (HLD §7; MTP §4.4) | Verified |
| NFR-03 | No crash or stack trace | HLD §6 | `CommandLine.cs::Parse`, `Application.cs::Run` | `Parse_SyntaxErrors_UseConv13Messages` (incl. `-😀`), `Run_BrokenPipe_StopsSilently` | Verified |
| NFR-04 | Separate units | HLD §3.2 | `CommandLine.cs`, `Validator.cs`, `Greeting.cs`, `Output.cs` | Design review (LLD §8) | Verified |
| NFR-05 | Pure, tested greeting | LLD §3.4 | `Greeting.cs::Line` | `GreetingTests.*` | Verified |
| NFR-06 | Every Makefile target works | HLD §3 | `00-hello-world/Makefile` (`DP_*` hooks) | CI `make check roundtrip` (3 OSes) and container `make check` | Verified |
| NFR-07 | No files, env, subprocesses | HLD §7 | whole `DpHello.Core` | Inspection: no `File`, `Environment`, or `Process` APIs are referenced | Verified |
| CONV-01 | Canonical name in messages | LLD §3.7 | `Messages.ToolName`, `Messages.cs::UsageDiagnostic` | `Run_UsageErrors_WriteTwoLinesToStderrOnly`, P09 | Verified |
| CONV-02 | Version format | LLD §3.7 | `Messages.VersionLine` | P08 | Verified |
| CONV-03 | LF output, binary mode | HLD §5, D3 | `Output.cs::WriteRepeated`, `Program.cs` | P01–P10 (Windows CI), `Run_MaximumCount_WritesExactlyThatManyLfTerminatedLines` | Verified |
| CONV-05 | UTF-8 arguments | HLD §5 | `Output.cs::WriteRepeated` (UTF-8, no BOM) | `Run_NonAsciiName_IsWrittenAsUtf8WithoutBom`, `Run_UsageErrors_WriteTwoLinesToStderrOnly` (`Zoë`) | Verified |
| CONV-06 | Locale-independent | HLD §5 | `Directory.Build.props` (`InvariantGlobalization`), `Validator.cs::TryParseCount` (ASCII digits) | `Validate_InvalidCount` (non-ASCII digits) | Verified |
| CONV-07 | Short options, attached values | LLD §4.1 | `CommandLine.cs::Parse` | `Parse_BundledValues_ConsumeRestOfToken`, P06 | Verified |
| CONV-08 | Long options, no abbreviations | LLD §4.1 | `CommandLine.cs::Parse` | `Parse_Name_AllSpellings`, `Parse_SyntaxErrors_UseConv13Messages` (`--nam`), P04 | Verified |
| CONV-09 | `--` terminator | LLD §4.1 | `CommandLine.cs::Parse` | `Parse_Terminator_MakesEverythingAfterItOperands` | Verified |
| CONV-10 | Operands, `-` is an operand | LLD §4.1 | `CommandLine.cs::Parse` | `Parse_Operands_AreCollectedInterleaved`, P10 | Verified |
| CONV-11 | Repetition, last wins | LLD §4.1 | `CommandLine.cs::Parse` | `Parse_RepeatedOptions_LastWins` | Verified |
| CONV-12 | Scan order | HLD D1, LLD §4.1 | `CommandLine.cs::Parse`, `Validator.cs::Validate` | `Parse_Help_WinsWhenReachedFirst`, `Parse_Version_WinsWhenReachedFirst`, `Run_Help_PrintsTheManualTextExactly` | Verified |
| CONV-13 | Exact usage messages | LLD §2.4 | `Messages.cs` | `Parse_SyntaxErrors_UseConv13Messages`, `Validate_InvalidCount`, P09, P10 | Verified |
| CONV-14 | Hint line, exit 2 | LLD §3.6 | `Messages.cs::UsageDiagnostic`, `Application.cs::Run` | `Run_UsageErrors_WriteTwoLinesToStderrOnly`, P09, P10 | Verified |
| CONV-15 | Values verbatim, `''` | LLD §3.7 | `Messages.cs::InvalidArgument` | `Validate_Name`, `Validate_InvalidCount` | Verified |
| CONV-18 | Exit statuses (SRS §3.7 override) | HLD §6 | `Application.Success`, `Application.UsageError` | `Run_Success`, `Run_UsageErrors_WriteTwoLinesToStderrOnly`, P01–P10 | Verified |
| CONV-22 | `--help`, `--version`, no argument | LLD §4.1 | `CommandLine.cs::Parse` | `Parse_SyntaxErrors_UseConv13Messages` (`--help=1`) | Verified |
| CONV-23 | Help text verbatim | LLD §3.7 | `Messages.HelpText` | `Run_Help_PrintsTheManualTextExactly`, P07 | Verified |
| CONV-24 | No crash / stack trace | HLD §6 | `Application.cs::Run` | `Parse_SyntaxErrors_UseConv13Messages`, `Run_BrokenPipe_StopsSilently` | Verified |
| CONV-25 | EPIPE: stop silently | HLD D7 | `Application.cs::Run` | `Run_BrokenPipe_StopsSilently` | Verified |
| CONV-26 | Never read stdin | HLD §3.1 | whole `DpHello.Core` | Inspection: stdin is never opened | Verified |
| CONV-31 | Memory above runtime baseline | HLD §7 | `Output.cs::WriteRepeated` | Measurement (MTP §4.4) | Verified |

## 3. Backward traceability (test → requirement)

| Test | Type | Requirement(s) verified |
| :-- | :-- | :-- |
| P01 | black-box, public | FR-01, UR-01, UR-07 |
| P02, P03 | black-box, public | FR-02 |
| P04 | black-box, public | FR-02, CONV-08 |
| P05 | black-box, public | FR-04 |
| P06 | black-box, public | FR-04, CONV-07 |
| P07 | black-box, public | FR-07, CONV-23 |
| P08 | black-box, public | FR-08, CONV-02 |
| P09 | black-box, public | FR-10, FR-11, CONV-13, CONV-14 |
| P10 | black-box, public | FR-09, FR-11, CONV-10, CONV-13 |
| `GreetingTests.Line_DefaultName_IsTheClassicGreeting` | unit | FR-01 |
| `GreetingTests.Line_UsesTheNameVerbatim`, `Line_IsPure` | unit | FR-02, NFR-05 |
| `CommandLineTests.Parse_NoArguments_RecordsNothing` | unit | FR-01 |
| `CommandLineTests.Parse_Name_AllSpellings` | unit | FR-02, CONV-07, CONV-08 |
| `CommandLineTests.Parse_Count_RecordsRawValue` | unit | FR-04, CONV-07, CONV-08 |
| `CommandLineTests.Parse_BundledValues_ConsumeRestOfToken` | unit | CONV-07 |
| `CommandLineTests.Parse_RepeatedOptions_LastWins` | unit | FR-06, CONV-11 |
| `CommandLineTests.Parse_Operands_AreCollectedInterleaved` | unit | FR-09, CONV-10 |
| `CommandLineTests.Parse_Terminator_MakesEverythingAfterItOperands` | unit | FR-09, CONV-09 |
| `CommandLineTests.Parse_Help_WinsWhenReachedFirst`, `Parse_Version_WinsWhenReachedFirst` | unit | FR-07, FR-08, CONV-12 |
| `CommandLineTests.Parse_SyntaxErrors_UseConv13Messages` | unit | FR-10, CONV-08, CONV-13, CONV-22, NFR-03 |
| `ValidatorTests.Validate_Defaults` | unit | FR-01 |
| `ValidatorTests.Validate_CountWithinRange`, `Validate_InvalidCount` | unit | FR-04, FR-05, CONV-06, CONV-15 |
| `ValidatorTests.Validate_Name` | unit | FR-03, CONV-15 |
| `ValidatorTests.Validate_Operands_ReportTheFirstBeforeValueErrors`, `Validate_EmptyName_ReportedBeforeInvalidCount` | unit | FR-09, CONV-12 |
| `ValidatorTests.TryParseCount_RejectsNull` | unit | NFR-03 |
| `ApplicationTests.Run_Success` | unit (whole program) | FR-01, FR-02, FR-04, FR-06, FR-08 |
| `ApplicationTests.Run_NonAsciiName_IsWrittenAsUtf8WithoutBom` | unit (whole program) | UR-09, CONV-05 |
| `ApplicationTests.Run_MaximumCount_WritesExactlyThatManyLfTerminatedLines` | unit (whole program) | FR-04, NFR-01, CONV-03 |
| `ApplicationTests.Run_Help_PrintsTheManualTextExactly` | unit (whole program) | FR-07, CONV-12, CONV-23 |
| `ApplicationTests.Run_UsageErrors_WriteTwoLinesToStderrOnly` | unit (whole program) | FR-03, FR-05, FR-09, FR-10, FR-11, CONV-13, CONV-14 |
| `ApplicationTests.Run_BrokenPipe_StopsSilently` | unit (whole program) | CONV-24, CONV-25 |

## 4. Coverage summary

| Category | Total | Designed | Implemented | Verified |
| :-- | :-- | :-- | :-- | :-- |
| User requirements (UR) | 12 | 12 | 12 | 12 |
| Functional (FR) | 11 | 11 | 11 | 11 |
| Non-functional (NFR) | 7 | 7 | 7 | 7 |
| Conventions (CONV) | 21 | 21 | 21 | 21 |

## 5. Gaps and justifications

No applicable requirement is unverified. Not applicable to `dp-hello`: CONV-04 (it copies no
input), CONV-16 … CONV-21 (no files, paths, or directory traversal), and CONV-27 … CONV-30
(conditions guaranteed by the black-box runner, relied on rather than implemented).
NFR-04 and NFR-07 are verified by design review and inspection, as SRS §7 prescribes.
