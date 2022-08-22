#Obter SDK Image paracompilação
FROM mcr.microsoft.com/dotnet/sdk:6.0 as build-env
WORKDIR /app

#Copiar o projeto e restaurar via nuget as dependancias
COPY *.csproj ./
RUN dotnet restore

# Copiar os arquivos de projeto e compilar o release
COPY . ./
RUN dotnet publish -c Release -o out

#Gerar a imagem de runtime
FROM  mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
EXPOSE  80
COPY  --from=build-env /app/out .
ENTRYPOINT [ "dotnet","WebApi.dll" ]