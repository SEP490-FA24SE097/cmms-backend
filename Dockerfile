# Create a stage for building the application.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-debian AS build

# Set working directory
COPY . /source
WORKDIR /source/CMMS/CMMS.API

# Build the application
ARG TARGETARCH
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish -a ${TARGETARCH/amd64/x64} --use-current-runtime --self-contained false -o /app

# Create a new stage for running the application
FROM debian:bullseye-slim AS final

# Install required dependencies for wkhtmltopdf
RUN apt-get update && apt-get install -y \
    libxrender1 \
    libxext6 \
    libfontconfig1 \
    libjpeg-turbo8 \
    libpng16-16 \
    ttf-freefont \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Download and install wkhtmltopdf
RUN curl -o /tmp/wkhtmltox.deb -L https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6.1-2/wkhtmltox_0.12.6.1-2.buster_ppc64el.deb \
    && dpkg -i /tmp/wkhtmltox.deb \
    && rm -f /tmp/wkhtmltox.deb

# Set working directory
WORKDIR /app

# Copy the built app from the build stage
COPY --from=build /app .

# Expose the port the app will run on
EXPOSE 80

# Switch to a non-privileged user (if necessary)
# USER $APP_UID

# Run the application
ENTRYPOINT ["dotnet", "CMMS.API.dll"]
