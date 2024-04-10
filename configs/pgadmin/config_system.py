AUTHENTICATION_SOURCES = ['oauth2','internal']

OAUTH2_CONFIG = [{
     'OAUTH2_NAME': 'Passi local',
    'OAUTH2_DISPLAY_NAME': 'Passi local',
    'OAUTH2_CLIENT_ID': 'PgAdminClient',
    'OAUTH2_CLIENT_SECRET': 'secret2',
    'OAUTH2_TOKEN_URL': 'https://host.docker.internal/identity/connect/token',
    'OAUTH2_AUTHORIZATION_URL': 'https://host.docker.internal/identity/connect/authorize',
    'OAUTH2_SERVER_METADATA_URL': 'https://host.docker.internal/identity/.well-known/openid-configuration',
    'OAUTH2_API_BASE_URL': 'https://host.docker.internal/identity/',
    'OAUTH2_USERINFO_ENDPOINT': 'connect/userinfo',
    'OAUTH2_BUTTON_COLOR': '#3253a8',
    'OAUTH2_SCOPE': 'openid email',
    'OAUTH2_USERNAME_CLAIM' : 'sub',
}]
OAUTH2_AUTO_CREATE_USER = False