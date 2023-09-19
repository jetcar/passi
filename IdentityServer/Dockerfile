FROM common_image:latest AS build

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /myapp
COPY --from=build /src/IdentityServer/publish/publish .
RUN useradd -m -u 1001 appuser
USER appuser
CMD ["chattr", "+i", "/"]
EXPOSE 5003

ENTRYPOINT ["dotnet", "IdentityServer.dll"]