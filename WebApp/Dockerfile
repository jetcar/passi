#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM common_image:1.0.5 AS build

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /myapp
COPY --from=build /publish/WebApp .
RUN useradd -m -u 1001 appuser
USER appuser
CMD ["chattr", "+i", "/"]
EXPOSE 5002

ENTRYPOINT ["dotnet", "WebApp.dll"]