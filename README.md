<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->

**Table of Contents** _generated with [DocToc](https://github.com/thlorenz/doctoc)_

- [cs](#cs)
  - [C# at a glance](#c-at-a-glance)
  - [Toolchain](#toolchain)
    - [Per-project tooling](#per-project-tooling)
  - [Layout](#layout)
  - [Development](#development)
  - [Implementing a project](#implementing-a-project)
  - [Contributing](#contributing)
  - [License](#license)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="https://raw.githubusercontent.com/dialectic-playground/.github/main/assets/cs/dist/logo-dark.svg">
    <img alt="dialectic playground / cs" src="https://raw.githubusercontent.com/dialectic-playground/.github/main/assets/cs/dist/logo.svg" height="96">
  </picture>
</p>

# cs

C# implementation of the Dialectic Playground SDLC curriculum.

📖 **Documentation:** <https://dialectic-playground.github.io/docs/reference/languages/cs/>

This repository implements the fifty-three projects of the
[SDLC curriculum](https://github.com/dialectic-playground/sdlc-curriculum) in C#. Each project is
specified to the byte by the curriculum and verified by its black-box tests on Linux, macOS,
and Windows.

**Status:** Implemented projects: 00-hello-world.

## C# at a glance

| Aspect                  | C#                                                         |
| :---------------------- | :--------------------------------------------------------- |
| File extension          | `.cs`                                                      |
| CLI classification      | Included                                                   |
| Image category          | First-Class                                                |
| Access to OS primitives | Runtime wraps system arguments into top-level Main() array |
| Entry-point mechanism   | Application Level (deep native reflection tools)           |

## Toolchain

|                      |                                                                                          |
| :------------------- | :--------------------------------------------------------------------------------------- |
| Release line         | .NET 10 (LTS)                                                                            |
| Build / package tool | dotnet CLI (NuGet)                                                                       |
| Container image      | `mcr.microsoft.com/dotnet/sdk:10.0`, pinned by digest ([`Containerfile`](Containerfile)) |
| Native CI            | actions/setup-dotnet (10.0.x)                                                            |

`make toolchain` verifies that the container and native toolchains are on this release line.

### Per-project tooling

Everything language-specific lives in the project's `src/`, and every `dotnet` command runs
from there so that `global.json` selects the SDK. All tools ship with the .NET SDK; the
container needs nothing beyond the base image, `make`, and `python3`.

| Concern               | Tool / setting                                                                                                                                                 |
| :-------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Layout                | `src/<Name>.slnx` with an app project (assembly `dp-…`), a class library for the logic, and an xUnit test project                                              |
| SDK pin               | `src/global.json`: `10.0.100` with `rollForward: latestFeature` (any 10.0 SDK)                                                                                 |
| Shared build settings | `src/Directory.Build.props`: `net10.0`, `Nullable`, `TreatWarningsAsErrors`, .NET analyzers (`AnalysisLevel latest`, `Recommended`), `EnforceCodeStyleInBuild` |
| Dependencies          | NuGet with committed `packages.lock.json` files, restored with `dotnet restore --locked-mode`                                                                  |
| Unit tests            | xUnit v3 on Microsoft.Testing.Platform (`dotnet test --solution`; opted in through `global.json`)                                                              |
| Lint                  | `dotnet build` (warnings are errors, analyzers and code style included) + `dotnet format --verify-no-changes`                                                  |
| Format                | `dotnet format`; C# style in `src/.editorconfig` (4-space indentation, file-scoped namespaces)                                                                 |
| `BIN_DIR`             | `bin/`: `dotnet publish` of a framework-dependent, single-file apphost (`dp-hello`, `dp-hello.exe` on Windows) for the build machine's runtime identifier      |
| Install               | copies that single executable to `$(BINDIR)`; it needs the .NET 10 runtime, found at the standard install location or through `DOTNET_ROOT`                    |

## Layout

| Path                                 | Contents                                                                                                          |
| :----------------------------------- | :---------------------------------------------------------------------------------------------------------------- |
| `00-hello-world/` … `52-spacecraft/` | One workspace per curriculum project: `Makefile` (with `DP_*` hooks), HLD, LLD, RTM, master test plan, and `src/` |
| `sdlc-curriculum/`                   | The curriculum, as a git submodule pinned to a release tag                                                        |
| `Makefile`                           | `make check` (repository, toolchain, implemented projects), `toolchain`, `projects`, `roundtrip`                  |
| `Containerfile`, `.devcontainer/`    | The toolchain image and the matching dev container                                                                |
| `tools/dp-run`, `tools/container.mk` | Shared container tooling (copies of the canonical files in `dialectic-playground/.github`)                        |
| `.github/workflows/ci.yml`           | CI: container and native (Linux, macOS, Windows) `make` targets, shared-tooling check, DCO                        |

## Development

Container-first: `make` targets and `tools/dp-run <command>` run in the toolchain container
via Podman, then Docker, then the local system. `DP_RUNTIME=podman|docker|local` overrides
the choice.

```sh
git clone --recurse-submodules https://github.com/dialectic-playground/cs.git
cd cs
make check                    # repository, toolchain, and every implemented project
DP_RUNTIME=local make check   # the same with your native toolchain
make -C 00-hello-world help   # a project's configuration and hooks
```

## Implementing a project

1. Complete the project's `hld.md` and `lld.md` before writing code.
2. Write the implementation and its unit tests under the project's `src/`, add the
   repository's dotnet CLI (NuGet) configuration, and fill in `BIN_DIR` and the `DP_*` hooks in
   the project's `Makefile`.
3. Pass `make -C <project> test` (container) and `DP_RUNTIME=local make -C <project> test`.
4. Add the project directory to `IMPLEMENTED` in the root `Makefile`. CI then builds, lints,
   and tests it in the container and natively on all three operating systems.
5. Keep `rtm.md` and `master-test-plan.md` current.

Specifications: [curriculum reference](https://dialectic-playground.github.io/docs/reference/curriculum/). Language setup:
[Set up a language repository](https://dialectic-playground.github.io/docs/how-to/set-up-a-language-repository/).

## Contributing

See the [organization contributing guide](https://github.com/dialectic-playground/.github/blob/main/CONTRIBUTING.md).
Pull requests are squash-merged, titles follow Conventional Commits, and every commit needs a
DCO sign-off.

## License

[MIT](LICENSE)
