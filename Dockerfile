
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

COPY . /source

WORKDIR /source/CMMS/CMMS.API


ARG TARGETARCH


RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish -a ${TARGETARCH/amd64/x64} --use-current-runtime --self-contained false -o /app


FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final

RUN apk add icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /app

# Copy everything needed to run the app from the "build" stage.
COPY --from=build /app .

# Switch to a non-privileged user (defined in the base image) that the app will run under.
# See https://docs.docker.com/go/dockerfile-user-best-practices/
# and https://github.com/dotnet/dotnet-docker/discussions/4764
USER $APP_UID

ENTRYPOINT ["dotnet", "CMMS.API.dll"]
