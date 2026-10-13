# 00-hello-world: Standard Terminal Hello World
#
# Project Makefile template. It extends the universal build contract in
# shared/templates/BASE-MAKEFILE, which defines the standard targets:
#
#   make all | install | uninstall | test | check | lint | format | clean | distclean
#   make test-hidden | grade | help
#
# A language implementation repository copies this file next to its sources, sets BIN_DIR,
# fills in each DP_* hook with its toolchain's commands, and points CURRICULUM_ROOT at its
# checkout of sdlc-curriculum (for example a git submodule). Hooks that are left empty make
# their target fail with an explanation. `make help` shows which hooks are defined.
#
# Container-first: goals run in the toolchain container built from the repository's
# Containerfile (podman, then docker, then local; override with DP_RUNTIME). The image needs
# the language toolchain, make, and python3 >= 3.11. Use DP_RUNTIME=local for native builds.

PROJECT := 00-hello-world
TOOLS   := dp-hello

# Path to the sdlc-curriculum checkout (the specs, test suites, and runner live there).
CURRICULUM_ROOT ?= ../sdlc-curriculum

# C# (.NET 10): the solution, global.json (SDK pin), and NuGet lock files live in src/.
# Every dotnet command runs from src/ so that global.json selects the SDK.
DOTNET ?= dotnet
SOLUTION := DpHello.slnx
RESTORE := $(DOTNET) restore $(SOLUTION) --locked-mode

# make all publishes a framework-dependent, single-file native executable (the .NET apphost,
# dp-hello or dp-hello.exe) for the build machine into bin/.
BIN_DIR := bin

DP_BUILD = cd src && $(RESTORE) && $(DOTNET) publish DpHello.Cli/DpHello.Cli.csproj \
	--configuration Release --no-restore --output ../$(BIN_DIR)

define DP_INSTALL
mkdir -p "$(BINDIR)"
cp "$(BIN_DIR)/dp-hello$(EXE)" "$(BINDIR)/dp-hello$(EXE)"
endef

DP_UNINSTALL = rm -f "$(BINDIR)/dp-hello$(EXE)"

DP_UNIT_TEST = cd src && $(RESTORE) && $(DOTNET) test --solution $(SOLUTION) \
	--configuration Release --no-restore

# TreatWarningsAsErrors + .NET analyzers + code style (EnforceCodeStyleInBuild), then the
# formatter in verify mode.
define DP_LINT
cd src && $(RESTORE) && $(DOTNET) build $(SOLUTION) --configuration Release --no-restore
cd src && $(DOTNET) format $(SOLUTION) --verify-no-changes --no-restore
endef

DP_FORMAT = cd src && $(RESTORE) && $(DOTNET) format $(SOLUTION) --no-restore

DP_CLEAN = rm -rf $(BIN_DIR) src/*/bin src/*/obj src/*/TestResults

# NuGet's package cache lives in the user's home directory, outside the project; remove
# local editor and IDE state.
DP_DISTCLEAN = rm -rf src/.vs src/*/*.csproj.user

include $(CURRICULUM_ROOT)/shared/templates/BASE-MAKEFILE
