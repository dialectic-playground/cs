# Master Test Plan (MTP) — Standard Terminal Hello World

| Field | Value |
| :-- | :-- |
| Project | `00-hello-world` |
| Tools | dp-hello |
| Implementation language | C# 14 on .NET 10 (LTS) |
| Author | Brandon Perkins (with Claude Code) |
| Version / date | 1.0 — 2026-10-13 |
| Standard | Structure adapted from IEEE 829 / ISO/IEC/IEEE 29119-3 |

## 1. Introduction and scope

### 1.1 Objectives
`dp-hello` is tested enough when every SRS acceptance criterion (FR-01 … FR-11) holds
byte-for-byte on Linux, macOS, and Windows, every parsing rule and boundary in CONV-07 …
CONV-15 has a unit test, and the NFRs are measured or inspected as SRS §7 prescribes.

### 1.2 In scope / out of scope
| In scope | Out of scope |
| :-- | :-- |
| Every FR/NFR in the SRS and the applicable CONV rules, on Linux, macOS, and Windows | Localization, terminal rendering of non-ASCII text, file and stdin behavior (none exists) |
| Unit tests of each Core unit and of the whole program with in-memory streams | Testing the .NET runtime's own argument decoding beyond the `Zoë` cases |

## 2. Test items

| Item | Version / location |
| :-- | :-- |
| dp-hello | `00-hello-world/src/` on branch `feat/00-hello-world` (PR "feat(00-hello-world): implement dp-hello in C#") |
| Public black-box suite | `tests/public/` (10 tests) |
| Hidden black-box suite | `tests/private/` (10 tests, run by CI) |

## 3. Test levels and approach

| Level | Purpose | Technique(s) | Tooling | Owner |
| :-- | :-- | :-- | :-- | :-- |
| Unit | Verify parser, validator, greeting in isolation | Equivalence partitioning, boundary-value analysis | xUnit v3 (`make test-unit` → `dotnet test`) | Implementer |
| Integration | Parser → validator → greeting → output | Whole-program tests of `Application.Run` with `MemoryStream`s, byte comparisons | xUnit v3 (`ApplicationTests`) | Implementer |
| System (black-box) | The specified CLI behavior, byte-exact, on the built apphost | Specification-based | `make test` → `run-tests.py` | Implementer |
| Acceptance | Hidden suite passes on all OSes | — | CI `make test-hidden` (container, Linux, macOS, Windows) | CI |
| Static analysis | Warnings, analyzers, code style, formatting | — | `make lint` (`dotnet build` with warnings as errors, `dotnet format --verify-no-changes`) | Implementer / CI |

## 4. Test design

### 4.1 Equivalence partitions and boundaries

| Input | Valid partitions | Invalid partitions | Boundaries |
| :-- | :-- | :-- | :-- |
| NAME | ASCII, spaces, non-ASCII (`Zoë`, emoji), leading `-`, contains `=` | empty (`-n ''`, `--name=`) | length 0 / 1 |
| COUNT | ASCII digits with value 1–1000, leading zeros | empty, signs, spaces, `.`, exponent, non-ASCII digits, 0, > 1000, 20-digit values | 0, 1, 1000, 1001; `000`, `0001000` |
| Option form | `-nX`, `-n X`, `--name=X`, `--name X`, bundles (`-c2`) | unknown short/long, abbreviated long, missing value, `=value` on flags | value as next token at end of list |
| Operands | none | any word, `-`, anything after `--` | first of several |
| Scan order | help/version first | syntax error first | `-x --help` vs `extra --help` vs `--count 0 --help` |

### 4.2 Additional test cases (beyond the supplied suite)

| ID | Requirement(s) | Preconditions | Steps / command | Expected result | Level |
| :-- | :-- | :-- | :-- | :-- | :-- |
| T-01 | FR-02, CONV-05 | — | `Run("--name", "Zoë")` | stdout bytes `Hello, Zo` C3 AB `!\n` (`Run_NonAsciiName_IsWrittenAsUtf8WithoutBom`) | integration |
| T-02 | FR-04, CONV-03 | — | `Run("-c", "1000")` | 14 000 bytes, no CR (`Run_MaximumCount_…`) | integration |
| T-03 | FR-05 | — | `Validate` with `0`, `1001`, `٣`, `３`, `+1`, `1e3` | `invalid argument '<v>' for '-c'` / `'--count'` (`Validate_InvalidCount`) | unit |
| T-04 | FR-04 | — | `Validate` with `007`, `0001000`, 23-digit `…01` | 7, 1000, 1 (`Validate_CountWithinRange`) | unit |
| T-05 | FR-07, CONV-12 | — | `Run("extra", "--help")`, `Run("--count", "0", "--help")` | help text, exit 0 (`Run_Help_PrintsTheManualTextExactly`) | integration |
| T-06 | FR-10, CONV-12 | — | `Run("-x", "--help")` | `invalid option -- 'x'`, exit 2 (`Run_UsageErrors_…`) | integration |
| T-07 | FR-06 | — | `Run("-n", "", "-n", "B")` | `Hello, B!` (only the winning value is validated) (`Run_Success`) | integration |
| T-08 | FR-09, CONV-09 | — | `Parse("-n", "A", "--", "-n", "--help", "--")` | operands `-n`, `--help`, `--` (`Parse_Terminator_…`) | unit |
| T-09 | FR-10, CONV-13 | — | `Parse("--x=1")`, `Parse("--=1")`, `Parse("--nam")` | `unrecognized option '<token>'` with the whole token (`Parse_SyntaxErrors_…`) | unit |
| T-10 | NFR-03, CONV-05 | — | `Parse("-😀")` | `invalid option -- '😀'` (whole surrogate pair) | unit |
| T-11 | CONV-25 | stdout stream throws `IOException` | `Run(["-c", "1000"], BrokenPipeStream, stderr)` | status 141, stderr empty (`Run_BrokenPipe_StopsSilently`) | integration |
| T-12 | FR-09, CONV-12 | — | `Validate` with operands, empty name, count 0 | `extra operand 'a'` first (`Validate_Operands_…`) | unit |

### 4.3 Negative and robustness testing
Every CONV-13 message is exercised by `Parse_SyntaxErrors_UseConv13Messages` and
`Run_UsageErrors_WriteTwoLinesToStderrOnly`, which also assert an empty stdout and status 2.
Very large input is covered by T-02 and the memory measurement below (a 100 000-character
name × 1000). Missing files, permissions, stdin, and binary input do not apply: the tool
reads none. A closed stdout is covered by T-11.

### 4.4 Performance and resource testing
Measured in the toolchain container (`mcr.microsoft.com/dotnet/sdk:10.0`, x64) with a Python
harness using `os.wait4` (peak RSS) and the best of 20 runs (wall time), under a minimal
environment. Baseline (CONV-31): a framework-dependent single-file app whose `Main` returns 0.

| Check | Limit | Result |
| :-- | :-- | :-- |
| Wall time, `dp-hello -c 1000` | < 300 ms (managed runtime); < 100 ms native | 36 ms (baseline 28 ms) |
| Peak RSS above baseline, `-c 1000 -n <100 000 chars>` | < 32 MiB | 26.3 − 21.2 = 4.6 MiB |

## 5. Test environments

| Environment | OS / version | Toolchain | How provisioned |
| :-- | :-- | :-- | :-- |
| Local / container | Fedora host, Debian-based SDK image | .NET SDK 10.0.4xx, Python 3, GNU Make | `make check` (Podman, repository `Containerfile`) |
| CI | ubuntu-latest, macos-latest, windows-latest | `actions/setup-dotnet` 10.0.x, Python 3.12, GNU Make | `.github/workflows/ci.yml` (`make check roundtrip`) |

## 6. Entry and exit criteria

- **Entry:** the HLD/LLD are written, `make all` succeeds, and `make lint` is clean.
- **Exit:** all unit tests pass; all public and hidden black-box tests pass in the container
  and natively on all three OSes; `make lint` and `make roundtrip` pass; the RTM shows every
  applicable requirement as Verified; no open severity-1/2 defects.
- **Suspension / resumption:** suspend when the build or lock-file restore fails in any
  environment; resume once the toolchain issue is fixed and `make check` passes again.

## 7. Defect management
Defects are GitHub issues in `dialectic-playground/cs` with the failing command, expected and
actual bytes, OS, and severity. Every fix adds a unit test that fails before the fix, and the
fix is accepted only when CI passes on every platform.

| Severity | Definition |
| :-- | :-- |
| 1 — Critical | Crash, stack trace, or wrong stdout on a mainline path |
| 2 — Major | Wrong diagnostic text or exit status; failure on one OS only |
| 3 — Minor | Documentation, tooling, or performance issue with no behavioral effect |

## 8. Schedule and deliverables

| Milestone | Deliverable | Target date |
| :-- | :-- | :-- |
| Design complete | `hld.md`, `lld.md` | 2026-10-13 |
| Implementation complete | `src/`, unit tests, Makefile hooks | 2026-10-13 |
| Verification complete | RTM all Verified, CI green | 2026-10-13 |

## 9. Risks and contingencies

| Risk | Mitigation |
| :-- | :-- |
| Windows-only failure discovered late | CI runs on all OSes from the first commit |
| The apphost cannot find the .NET runtime under the runner's minimal environment | Default install locations on Linux/Windows; macOS CI registers `/etc/dotnet/install_location` |
| Formatter or analyzer behavior differs between SDK patch releases | One release line (10.0) everywhere; `make toolchain` checks it |

## 10. Test summary (complete at the end)

| Metric | Value |
| :-- | :-- |
| Unit tests (pass / total) | 111 / 111 |
| Coverage (line / branch) | Not instrumented; every function and every diagnostic has a unit test (RTM §3) |
| Public black-box (pass / 10) | 10 / 10 (container, Linux, macOS, Windows) |
| Hidden black-box (pass / 10, from CI) | 10 / 10 |
| Open defects by severity | none |
