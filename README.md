# Passi
Passwordless 2FA solution, server and android app


try using
App: https://play.google.com/store/apps/details?id=com.passi.cloud.passi_android
<br>
Sample web App: https://passi.cloud <br>
How to:<br>
1. Get android app, open it and enter your mail.<br>
2. Check email for confirmation code and enter it in app. You can close app for now.<br>
3. Next you can protect your account by PIN, but it's not nessesary and you can skipt it. If you have PIN created, then you can also add fingerprint to simplify login.<br>
4. Go to https://passi.cloud and try to login using email.<br>
5. Check phone for notification, if not received just open the app you will see notification with confirmation colors and description where initial session is started.<br>
6. Select correct color and enter PIN or fingerprint if needed.


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
