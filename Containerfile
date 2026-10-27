# Toolchain image for dialectic-playground/cs: C# (container-first; see tools/dp-run).
#
# .NET 10 (LTS); build and package tool: dotnet CLI (NuGet).
# Official base image pinned by multi-architecture index digest, plus make and python3 (for the
# curriculum's black-box runner). Dependabot (docker) refreshes the digest; moving to a new
# release line is a deliberate change (keep `make toolchain` and the native CI in step).
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:de4fe90a98845d75910b9769662465c84de1a934665c431279d1382b2bddc02b

# Debian keeps only the current version of each package, so apt versions cannot be pinned
# reproducibly; the base image is pinned by digest instead.
# hadolint ignore=DL3008
RUN apt-get update \
    && apt-get install -y --no-install-recommends make python3 \
    && rm -rf /var/lib/apt/lists/*

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DP_IN_CONTAINER=1
