# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Consist.Doxi.MCPServer/Consist.Doxi.MCPServer.csproj", "Consist.Doxi.MCPServer/"]
COPY ["Consist.Doxi.MCPServer.Domain/Consist.Doxi.MCPServer.Domain.csproj", "Consist.Doxi.MCPServer.Domain/"]
COPY ["Services/ApryseDataExtractor/ApryseDataExtractor.csproj", "Services/ApryseDataExtractor/"]
COPY ["Services/Consist.GPTDataExtruction/Consist.GPTDataExtruction.csproj", "Services/Consist.GPTDataExtruction/"]
COPY ["Services/Consist.PDFConverter/Consist.PDFConverter.csproj", "Services/Consist.PDFConverter/"]
COPY ["Services/Consist.MCPServer.DoxiClient/Consist.MCPServer.DoxiAPIClient.csproj", "Services/Consist.MCPServer.DoxiClient/"]
RUN dotnet restore "./Consist.Doxi.MCPServer/Consist.Doxi.MCPServer.csproj"
COPY . .
WORKDIR "/src/Consist.Doxi.MCPServer"
RUN dotnet build "./Consist.Doxi.MCPServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Consist.Doxi.MCPServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

###########################################################
#        INSTALL APRYSE DATA EXTRACTION MODULE            #
###########################################################

# Switch to root so we can install packages
USER root

# Install Maven + unzip (required for Apryse module)
RUN apt-get update && apt-get install -y maven unzip && rm -rf /var/lib/apt/lists/*

# Create folder for Apryse native libs
RUN mkdir -p /usr/local/lib/apryse

# Download Apryse Data Extraction module from Apryse Maven repository
RUN mvn dependency:get \
    -DrepoUrl=https://maven.apryse.com \
    -DgroupId=com.pdftron \
    -DartifactId=DataExtractionModuleLinux \
    -Dversion=10.12.0 \
    -Ddest=/tmp/dataextraction.jar

# Extract .so native libraries into the system lib folder
RUN unzip /tmp/dataextraction.jar -d /tmp/apryse && \
    cp /tmp/apryse/Lib/Linux/x64/*.so /usr/local/lib/apryse && \
    rm -rf /tmp/apryse /tmp/dataextraction.jar

# Set library path
ENV LD_LIBRARY_PATH="/usr/local/lib/apryse:${LD_LIBRARY_PATH}"

# Return to non-root user (as original Dockerfile)
USER $APP_UID

###########################################################

ENTRYPOINT ["dotnet", "Consist.Doxi.MCPServer.dll"]
