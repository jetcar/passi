FROM common_image:latest AS build

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /myapp
COPY --from=build /src/passiwebapi/publish/publish .
RUN useradd -m -u 1001 appuser
USER appuser
CMD ["chattr", "+i", "/"]
EXPOSE 5004

ENTRYPOINT ["dotnet", "passiwebapi.dll"]