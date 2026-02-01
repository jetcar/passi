FROM jetcar/common_image:latest AS build

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /myapp
COPY --from=build /src/SampleApi/publish/publish .

ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/myapp/cert/aspnetcore.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=mypassword

RUN useradd -m -u 1001 appuser
USER appuser
CMD ["chattr", "+i", "/"]
EXPOSE 5006

ENTRYPOINT ["dotnet", "SampleApi.dll"]
