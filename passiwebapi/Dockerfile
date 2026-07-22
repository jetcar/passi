FROM jetcar/common_image:latest AS build

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /myapp
COPY --from=build /src/passiwebapi/publish/publish .
# Kerberos/GSSAPI runtime lib so Npgsql can negotiate auth without the
# "libgssapi_krb5.so.2: cannot open shared object file" startup error.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
RUN useradd -m -u 1001 appuser
USER appuser
CMD ["chattr", "+i", "/"]
EXPOSE 5004

ENTRYPOINT ["dotnet", "passiwebapi.dll"]