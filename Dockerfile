# Build stage
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

COPY . /source

WORKDIR /source/CMMS/CMMS.API

ARG TARGETARCH

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish -a ${TARGETARCH/amd64/x64} --use-current-runtime --self-contained false -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final

# Install dependencies for wkhtmltopdf
RUN apk add --no-cache \
    libxrender \
    libxext \
    libfontconfig \
    libjpeg-turbo \
    libpng \
    ttf-freefont \
    curl

# Download and install wkhtmltopdf
RUN curl -o /tmp/wkhtmltox.tar.xz -L https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6-1/wkhtmltox-0.12.6-1.alpine_linux-amd64.tar.xz \
    && tar -xvf /tmp/wkhtmltox.tar.xz -C /tmp \
    && mv /tmp/wkhtmltox/bin/wkhtmltopdf /usr/local/bin/ \
    && chmod +x /usr/local/bin/wkhtmltopdf \
    && rm -rf /tmp/wkhtmltox*

# Enable globalization support
RUN apk add icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /app

# Copy build output from the build stage
COPY --from=build /app .

USER $APP_UID

ENTRYPOINT ["dotnet", "CMMS.API.dll"]
