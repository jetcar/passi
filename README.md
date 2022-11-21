# Пassi
Passwordless 2FA solution, server and android app


try using
App: https://play.google.com/store/apps/details?id=com.passi.cloud.passi_android
<br>
Sample web App: https://passi.cloud 

Sample
<br>
../passi_config/dev.env<br>
<b>
ClientId=SampleApp<br>
ClientSecret=secret<br>
IdentityUrl=https://internalIP/identity<br>
IdentityUrlBase=https://internalIP<br>
PassiUrl=https://internalIP/passiapi<br>
DOTNET_RUNNING_IN_CONTAINER=true<br>
DOTNET_GENERATE_ASPNET_CERTIFICATE=false<br>
DOTNET_USE_POLLING_FILE_WATCHER=true<br>
DoNotSendMail=false #true will redirect all mails to testMail<br>
POSTGRES_USER=postgres<br>
POSTGRES_PASSWORD=postgres_password<br>
POSTGRES_DB=test<br>
DbHost=database<br>
DbPassword=postgres_password<br>
DbSslMode=Allow<br>
SendgridApiKey=own_sendgrid_apikey<br>
testMail=your@mail.com<br>
</b>

own certificates put here<br>
../passi_cert<br>

AppConfig/ConfigSettings.cs -> WebApiUrlLocal is for internalIp so mobile app can access it
