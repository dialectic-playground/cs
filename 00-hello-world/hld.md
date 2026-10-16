<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->

**Table of Contents** _generated with [DocToc](https://github.com/thlorenz/doctoc)_

- [High-Level Design (HLD) — Standard Terminal Hello World](#high-level-design-hld--standard-terminal-hello-world)
  - [1. Introduction](#1-introduction)
    - [1.1 Scope](#11-scope)
    - [1.2 Definitions and acronyms](#12-definitions-and-acronyms)
    - [1.3 References](#13-references)
  - [2. Requirements summary](#2-requirements-summary)
  - [3. Architectural overview](#3-architectural-overview)
    - [3.1 Context diagram](#31-context-diagram)
    - [3.2 Component (module) decomposition](#32-component-module-decomposition)
    - [3.3 Data flow](#33-data-flow)
  - [4. Key design decisions](#4-key-design-decisions)
  - [5. Cross-platform strategy](#5-cross-platform-strategy)
  - [6. Error handling and diagnostics](#6-error-handling-and-diagnostics)
  - [7. Non-functional design](#7-non-functional-design)
  - [8. Risks and open issues](#8-risks-and-open-issues)
  - [9. Approval](#9-approval)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

# High-Level Design (HLD) — Standard Terminal Hello World

| Field                   | Value                                                                               |
| :---------------------- | :---------------------------------------------------------------------------------- |
| Project                 | `00-hello-world`                                                                    |
| Tools                   | dp-hello                                                                            |
| Implementation language | C# 14 on .NET 10 (LTS)                                                              |
| Author                  | Brandon Perkins (with Claude Code)                                                  |
| Version / date          | 1.0 — 2026-10-13                                                                    |
| Status                  | Approved                                                                            |
| Inputs                  | `specs/URD.md`, `specs/SRS.md`, `specs/user-manual.md`, `shared/CLI-CONVENTIONS.md` |

## 1. Introduction

### 1.1 Scope

`dp-hello` writes `Hello, NAME!` plus LF to standard output COUNT times (NAME defaults to
`World`, COUNT to 1, range 1–1000), prints help and version text, and reports usage errors
with the CONV-13/14 diagnostics and exit status 2. As SRS §1.2 states, it reads no input,
takes no operands, and touches no files; it also reads no environment variables and starts no
processes (NFR-07).

### 1.2 Definitions and acronyms

| Term                | Meaning                                                                       |
| :------------------ | :---------------------------------------------------------------------------- |
| apphost             | The small native executable the .NET SDK generates to start a managed program |
| Single-file         | A publish mode that bundles the managed assemblies into the apphost           |
| Framework-dependent | Uses the shared .NET runtime installed on the machine instead of carrying one |
| Scan                | The left-to-right pass over the arguments described in CONV-12                |

### 1.3 References

- SRS-00 §3 (interfaces), §4 (FR-01 … FR-11), §5 (NFR-01 … NFR-07), §6 (diagnostics).
- CLI-CONV §2 (CONV-03, CONV-05), §3 (CONV-07 … CONV-12), §4 (CONV-13 … CONV-15), §8.
- User manual §"Help output".

## 2. Requirements summary

| Area                | SRS requirements                                     | Architectural impact                                                                                          |
| :------------------ | :--------------------------------------------------- | :------------------------------------------------------------------------------------------------------------ |
| Option scanning     | FR-02, FR-06, FR-07, FR-08, FR-10, CONV-07 … CONV-12 | A parser that only records values and collects operands, returning early for help, version, and syntax errors |
| Validation          | FR-03, FR-05, FR-09, FR-11                           | A separate validator run after the scan, so value and operand errors never beat `--help`                      |
| Greeting            | FR-01, FR-02, FR-04, NFR-05                          | A pure function `Greeting.Line(name)`                                                                         |
| Output              | NFR-01, CONV-03, CONV-05, CONV-25                    | UTF-8 bytes written to raw binary streams; broken pipes end the run silently                                  |
| Diagnostics         | §6, CONV-13 … CONV-15                                | All message texts in one class; one reporting point                                                           |
| Build / portability | NFR-06, UC-1                                         | One codebase; the apphost is built per OS by `make all`                                                       |

## 3. Architectural overview

### 3.1 Context diagram

```text
                    ┌──────────────────────────────┐
  argv (strings ──▶ │ dp-hello (apphost)           │ ──▶ stdout  (UTF-8 bytes, LF)
  decoded by the    │   └─ .NET 10 shared runtime  │ ──▶ stderr  (UTF-8 bytes, LF)
  runtime)          └──────────────────────────────┘ ──▶ exit status 0 / 2
  no stdin, files, environment variables, network, or child processes
```

### 3.2 Component (module) decomposition

| Component            | Responsibility                                                                                                                   | Depends on                            |
| :------------------- | :------------------------------------------------------------------------------------------------------------------------------- | :------------------------------------ |
| `CommandLine` (Core) | Scan arguments: record the last `--name`/`--count` value with its spelling, collect operands, stop at help/version/syntax errors | `Messages`                            |
| `Validator` (Core)   | Turn recorded arguments into `Settings` or a usage error (operands, then name, then count)                                       | `Messages`, `Greeting` (default name) |
| `Greeting` (Core)    | Build one greeting line; pure                                                                                                    | —                                     |
| `Output` (Core)      | Encode text once as UTF-8 and write it N times through a buffer to a binary stream                                               | —                                     |
| `Messages` (Core)    | Canonical name, help text, version line, every CONV-13 message, the CONV-14 hint                                                 | —                                     |
| `Application` (Core) | Orchestrate scan → validate → build → write; map outcomes to exit statuses; the only place that reports errors                   | all of the above                      |
| `Program` (Cli)      | Thin shell: open the raw standard streams and call `Application.Run`                                                             | `Application`                         |

### 3.3 Data flow

`Program` passes `args` and the raw stdout/stderr streams to `Application.Run`.
`CommandLine.Parse` returns one of `HelpRequested`, `VersionRequested`, `SyntaxError(message)`,
or `Arguments(name?, count?, operands)`. For `Arguments`, `Validator.Validate` returns
`Valid(Settings)` or `Invalid(message)`. `Application` then either writes the help text, the
version line, or `Greeting.Line(name)` COUNT times to stdout and returns 0, or writes the
two-line diagnostic to stderr and returns 2. Nothing reaches stdout before every check has
passed (FR-11).

## 4. Key design decisions

| #   | Decision                                                                          | Alternatives considered                                                                                                               | Rationale                                                                                                                 |
| :-- | :-------------------------------------------------------------------------------- | :------------------------------------------------------------------------------------------------------------------------------------ | :------------------------------------------------------------------------------------------------------------------------ |
| D1  | Scan and validate in two units; outcomes are closed record hierarchies            | Validate while parsing; exceptions for usage errors                                                                                   | CONV-12 requires value/operand errors after the scan; records make every outcome explicit and testable without exceptions |
| D2  | `Application.Run(args, stdout, stderr) -> int` in the library, a 3-line `Program` | Logic in `Main`                                                                                                                       | Whole-program unit tests with `MemoryStream`, no process spawning                                                         |
| D3  | Write bytes to `Console.OpenStandardOutput()`/`OpenStandardError()`               | `Console.Out` / `Console.Error`                                                                                                       | The text writers encode with the console code page and may translate; raw streams give exact UTF-8 and LF on every OS     |
| D4  | Encode the greeting once, write it COUNT times through a 64 KiB `BufferedStream`  | Build the whole output string                                                                                                         | Memory bounded by one line (a 128 KiB name × 1000 would otherwise be 128 MB)                                              |
| D5  | Framework-dependent single-file publish with `UseCurrentRuntimeIdentifier`        | Self-contained (≈70 MB), Native AOT (needs a C toolchain in every environment), multi-file publish (install would need several files) | One small executable to copy on install; relies on the .NET 10 runtime that every environment already has                 |
| D6  | `RuntimeIdentifiers` lists all six desktop RIDs                                   | RID-free lock file                                                                                                                    | Restoring for the current RID would otherwise make `packages.lock.json` OS-specific and break `--locked-mode`             |
| D7  | EPIPE (`IOException` while writing) ends the run with status 141, silently        | Crash; status 1                                                                                                                       | CONV-25 leaves the status unspecified; 128 + SIGPIPE is the conventional value and avoids the SRS's "never 1"             |

## 5. Cross-platform strategy

- **Line endings (CONV-03):** every generated line ends in `\n` inside C# string constants;
  output goes to raw handle streams, which never translate newlines on any OS.
- **Encoding (CONV-05):** the .NET runtime builds `args` from UTF-8 `argv` bytes on POSIX and
  from the UTF-16 command line on Windows; `Output` encodes with `UTF8Encoding` without a
  BOM. `Zoë` therefore round-trips on all three OSes. `InvariantGlobalization` removes any
  locale dependence (CONV-06).
- **Paths:** not applicable (no file access).
- **Platform isolation:** the runtime and `Program` are the only OS-facing code; the library
  sees only strings and `Stream`s. The OS-specific part of the build is the apphost, produced
  for the machine's RID by `make all` (`$(EXE)` adds `.exe` on Windows).

## 6. Error handling and diagnostics

Usage errors are values, not exceptions: `ParseOutcome.SyntaxError` and
`ValidationOutcome.Invalid` carry the CONV-13 message text built by `Messages`.
`Application.Run` is the single place that turns them into output: it writes
`Messages.UsageDiagnostic(message)` (the `dp-hello:` prefix and the CONV-14 hint) to stderr
and returns 2. The only runtime failure, a closed stdout, is caught there as `IOException`
(D7). No other exceptions can occur for any argument list (CONV-24); argument-null guards
protect the public API only.

## 7. Non-functional design

| Quality attribute           | SRS reference   | How the design achieves it                                                                                                                                                                   |
| :-------------------------- | :-------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Performance / memory        | NFR-02, CONV-31 | Measured in the container: 36 ms wall for `-c 1000` (empty .NET program: 28 ms); peak RSS 26.3 MiB for `-c 1000` with a 100 000-character name versus a 21.2 MiB runtime baseline (+4.6 MiB) |
| Portability                 | NFR-01          | D3, §5; CI runs every test natively on Linux, macOS, and Windows                                                                                                                             |
| Robustness                  | NFR-03          | Total functions over the argument list; no exceptions except the handled EPIPE                                                                                                               |
| Security / input validation | NFR-07          | No file, environment, network, or process APIs are referenced; COUNT is parsed digit by digit with saturation                                                                                |
| Maintainability             | NFR-04          | One responsibility per class (§3.2); analyzers and code style enforced as errors                                                                                                             |
| Testability                 | NFR-05          | `Greeting.Line` is pure; `Application.Run` is driven with in-memory streams                                                                                                                  |

## 8. Risks and open issues

| Risk / issue                                                                       | Likelihood | Impact | Mitigation                                                                                                 |
| :--------------------------------------------------------------------------------- | :--------- | :----- | :--------------------------------------------------------------------------------------------------------- |
| The apphost cannot find the runtime under a minimal environment (no `DOTNET_ROOT`) | M          | H      | Runtime installed at the default or registered location; macOS CI registers `/etc/dotnet/install_location` |
| On POSIX, `argv` bytes that are not valid UTF-8 become U+FFFD in `args`            | L          | L      | Outside the spec's UTF-8 assumption (SRS §2.4); documented                                                 |
| A new SDK patch changes implicit package versions and breaks locked restores       | L          | M      | The single-file analyzer (which adds `Microsoft.NET.ILLink.Tasks`) is disabled                             |

## 9. Approval

| Role     | Name                               | Date       | Decision       |
| :------- | :--------------------------------- | :--------- | :------------- |
| Author   | Brandon Perkins (with Claude Code) | 2026-10-13 | Submitted      |
| Reviewer | Pull-request reviewer              | 2026-10-13 | Pending review |
