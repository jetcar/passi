# OpenIDC Migration Summary

## Overview
Successfully migrated the OpenIDC project from using the OpenIddict library to a custom Redis-based OAuth2/OIDC implementation.

## Changes Made

### 1. Dependencies Removed
- `OpenIddict.AspNetCore`
- `OpenIddict.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.SqlServer`
- All OpenIddict-related packages

### 2. Dependencies Added
- Project reference to `RedisClient`
- Using existing `ConfigurationManager` and `Services` projects
- `System.IdentityModel.Tokens.Jwt` (8.5.0)

### 3. New Models Created

#### `Models/OidcClient.cs`
- Client configuration storage
- Properties: ClientId, ClientSecret, DisplayName, RedirectUris, AllowedScopes, RequiresPkce, CreatedAt
- Stored in Redis with key: `oidc:client:{clientId}`

#### `Models/AuthorizationCode.cs`
- Authorization code storage
- Properties: Code, ClientId, RedirectUri, CodeChallenge, CodeChallengeMethod, Claims, Scopes, ExpiresAt
- Stored in Redis with 5-minute expiry
- Consumed after first use

#### `Models/RefreshToken.cs`
- Refresh token storage
- Properties: Token, ClientId, Subject, Claims, Scopes, IssuedAt, ExpiresAt
- Stored in Redis with 30-day expiry

### 4. New Services Created

#### `Services/ClientStore.cs`
- Redis-based client management
- Methods: FindByClientIdAsync, CreateClientAsync, DeleteClientAsync, ValidateClientAsync
- Stores clients in Redis with 365-day expiry

#### `Services/AuthorizationCodeStore.cs`
- Authorization code lifecycle management
- Methods: StoreAuthorizationCodeAsync (5min TTL), GetAuthorizationCodeAsync, ConsumeAuthorizationCodeAsync
- Ensures codes are single-use

#### `Services/RefreshTokenStore.cs`
- Refresh token management
- Methods: StoreRefreshTokenAsync (30 day TTL), GetRefreshTokenAsync, RevokeRefreshTokenAsync

#### `Services/TokenService.cs`
- JWT token generation and validation
- Uses RSA signing with X509Certificate2 from PEM files
- Methods: GenerateAccessToken (1hr), GenerateIdToken (1hr), GenerateRefreshToken (30 days), ValidateToken
- Certificate path: `/myapp/cert/certs/`

### 5. New Helpers Created

#### `Helpers/PkceHelper.cs`
- PKCE code challenge validation
- Methods: ValidateCodeChallenge, ComputeS256CodeChallenge
- Supports SHA256 and plain code challenge methods

### 6. Controllers Updated

#### `Controllers/TokenController.cs` (NEW)
- OAuth2 token endpoint: `POST /connect/token`
- Supported grants:
  - `authorization_code` (with PKCE validation)
  - `refresh_token`
- Returns: access_token, id_token, refresh_token, expires_in, token_type

#### `Controllers/AuthorizationController.cs` (REWRITTEN)
- **New Endpoints:**
  - `GET/POST /connect/authorize` - Authorization endpoint
    - Validates client_id, redirect_uri, response_type
    - Checks PKCE requirements
    - Generates authorization code with claims
    - Redirects to client with code
  - `GET /connect/userinfo` - User information endpoint (uses JWT bearer auth)
  - `GET/POST /connect/logout` - Logout endpoint
  - `GET /.well-known/openid-configuration` - Discovery document

#### `Controllers/ApiController.cs` (UPDATED)
- Removed OpenIddict dependencies
- Uses ClientStore instead of IOpenIddictApplicationManager
- Uses AuthorizationCodeStore for code generation
- Endpoints: `/api/login`, `/api/check`

### 7. Startup Configuration (COMPLETELY REWRITTEN)

#### Removed:
- OpenIddict server configuration
- Entity Framework DbContext
- Quartz scheduler
- Database migrations

#### Added:
- RedisClient service registration
- Custom stores (ClientStore, AuthorizationCodeStore, RefreshTokenStore)
- TokenService for JWT generation
- JWT Bearer authentication with certificate validation
- Simplified middleware pipeline

### 8. Worker.cs (UPDATED)
- Removed IOpenIddictApplicationManager dependency
- Removed ApplicationDbContext dependency
- Uses ClientStore for client initialization
- Seeds two clients on startup:
  1. **SampleApp** - with PKCE requirement
  2. **Mailu** - without PKCE requirement

### 9. Files Deleted
- `Data/ApplicationDbContext.cs` - Entity Framework context (no longer needed)
- `Data/` folder - Removed entirely
- `Controllers/ErrorController.cs` - Old OpenIddict error handler
- `Controllers/UserinfoController.cs` - Duplicate userinfo endpoint
- All database migration files (none existed)

## OAuth2/OIDC Flow

### Authorization Code Flow with PKCE
1. Client redirects user to `/connect/authorize` with:
   - client_id
   - redirect_uri
   - response_type=code
   - scope
   - state
   - code_challenge
   - code_challenge_method
2. User authenticates (redirected to `/api/login` if not authenticated)
3. Server generates authorization code and stores it in Redis with:
   - User claims
   - PKCE challenge
   - 5-minute expiry
4. Server redirects back to client with code and state
5. Client POSTs to `/connect/token` with:
   - grant_type=authorization_code
   - code
   - redirect_uri
   - client_id
   - client_secret
   - code_verifier (for PKCE)
6. Server validates:
   - Client credentials
   - Authorization code
   - Redirect URI
   - PKCE code verifier
7. Server returns:
   - access_token (JWT, 1hr expiry)
   - id_token (JWT, 1hr expiry)
   - refresh_token (random string, 30 day expiry)
   - expires_in
   - token_type=Bearer

### Refresh Token Flow
1. Client POSTs to `/connect/token` with:
   - grant_type=refresh_token
   - refresh_token
   - client_id
   - client_secret
2. Server validates refresh token and client credentials
3. Server returns new tokens

## Configuration

### Required Environment Variables (via AppSetting)
- `ClientId` - SampleApp client ID
- `ClientSecret` - SampleApp client secret
- `MailuClientId` - Mailu client ID
- `MailluSecret` - Mailu client secret
- `IdentityUrlBase` - Base URL for issuer (e.g., https://passi.cloud)

### Required Certificates
- `/myapp/cert/certs/cert.pem` - Public key for token signing
- `/myapp/cert/certs/key.pem` - Private key for token signing

### Redis Configuration
- Configured via RedisClient service
- All OAuth2 data stored in Redis (clients, codes, tokens)
- TTL automatically managed

## Token Specifications

### Access Token (JWT)
- Algorithm: RS256
- Issuer: `{IdentityUrlBase}/openidc`
- Audience: Client ID
- Expiry: 1 hour
- Claims: sub, email, name, roles, thumbprint, sessionId

### ID Token (JWT)
- Algorithm: RS256
- Issuer: `{IdentityUrlBase}/openidc`
- Audience: Client ID
- Expiry: 1 hour
- Claims: sub, email, name, iat, exp

### Refresh Token
- Format: GUID (random)
- Storage: Redis
- Expiry: 30 days
- Associated with client and user

## Benefits of Migration

1. **No External Dependencies**: Removed heavy OpenIddict library and Entity Framework
2. **Simplified Architecture**: Direct Redis storage, no database migrations
3. **Better Performance**: Redis-based storage is faster than database queries
4. **Full Control**: Complete control over OAuth2/OIDC flow and token generation
5. **Lightweight**: Smaller dependency footprint
6. **Consistent Storage**: All session/auth data in Redis (matching eid-openidc pattern)

## Testing Recommendations

1. Test Authorization Code Flow with PKCE (SampleApp client)
2. Test Authorization Code Flow without PKCE (Mailu client)
3. Test Refresh Token Flow
4. Test Token Validation at protected endpoints
5. Test UserInfo endpoint with valid access token
6. Verify PKCE validation rejects invalid code_verifier
7. Verify authorization codes are single-use
8. Verify token expiry is enforced

## Security Features

- PKCE support (required for SampleApp, optional for Mailu)
- Client secret validation
- Redirect URI validation
- Authorization code single-use enforcement
- Token expiry enforcement
- RSA token signing
- Claim-based authorization
- Session validation via thumbprint and sessionId claims
