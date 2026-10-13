# Toolchain image for dialectic-playground/cs: C# (container-first; see tools/dp-run).
#
# .NET 10 (LTS); build and package tool: dotnet CLI (NuGet).
# Official base image pinned by multi-architecture index digest, plus make and python3 (for the
# curriculum's black-box runner). Dependabot (docker) refreshes the digest; moving to a new
# release line is a deliberate change (keep `make toolchain` and the native CI in step).
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317

RUN apt-get update \
    && apt-get install -y --no-install-recommends make python3 \
    && rm -rf /var/lib/apt/lists/*

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DP_IN_CONTAINER=1
