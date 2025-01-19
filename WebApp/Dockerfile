
FROM common_image:latest AS build

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /myapp
COPY --from=build /src/WebApp/publish/publish .
RUN useradd -m -u 1001 appuser
USER appuser
CMD ["chattr", "+i", "/"]
EXPOSE 5002

ENTRYPOINT ["dotnet", "WebApp.dll"]