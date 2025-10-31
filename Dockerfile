# syntax=docker/dockerfile:1

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /src

COPY src/k6-tester/k6-tester.csproj ./src/k6-tester/
RUN dotnet restore ./src/k6-tester/k6-tester.csproj -a $TARGETARCH

COPY ./src/k6-tester/ ./src/k6-tester/
RUN dotnet publish ./src/k6-tester/k6-tester.csproj -a $TARGETARCH -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ARG TARGETARCH
ARG K6_VERSION=1.3.0

RUN set -eux; \
    apt-get update; \
    apt-get install -y --no-install-recommends ca-certificates curl; \
    rm -rf /var/lib/apt/lists/*; \
    case "$TARGETARCH" in \
        amd64) k6_arch=amd64 ;; \
        arm64) k6_arch=arm64 ;; \
        arm*) k6_arch=arm ;; \
        *) echo "Unsupported TARGETARCH: $TARGETARCH" >&2; exit 1 ;; \
    esac; \
    curl -fsSL "https://github.com/grafana/k6/releases/download/v${K6_VERSION}/k6-v${K6_VERSION}-linux-${k6_arch}.tar.gz" -o /tmp/k6.tar.gz; \
    tar -C /usr/local/bin -xzf /tmp/k6.tar.gz --strip-components=1 "k6-v${K6_VERSION}-linux-${k6_arch}/k6"; \
    rm /tmp/k6.tar.gz

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "k6-tester.dll"]
