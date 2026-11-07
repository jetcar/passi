FROM jetcar/common_image:latest AS build

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG OS_PATCH_RUN_ID
RUN : "$OS_PATCH_RUN_ID" \
    && apt-get update \
    && apt-get upgrade -y --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /myapp
COPY --from=build /src/SampleApi/publish/publish .

ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/myapp/cert/aspnetcore.pfx

RUN useradd -m -u 1001 appuser
USER appuser
CMD ["chattr", "+i", "/"]
EXPOSE 5006

ENTRYPOINT ["dotnet", "SampleApi.dll"]
