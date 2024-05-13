
# Simple Token Service

A simple token service that generates, validates, refreshes the JWT tokens.

## Features

  
- **Generate Token Pair:** Creates a new access token and refresh token pair based on the provided claims.  
- **Refresh Token Pair:** Generates a new access token and refresh token pair using an existing refresh token.  
- **Validate Access Token:** Validates the provided access token.  
- **Validate Refresh Token:** Validates the provided refresh token.  
- **Validate Token Pair:** Validates both the access token and the refresh token.  
- **Disable Refresh Token:** Disables a given refresh token to prevent its use for generating new access tokens.  
- **Ping:** A simple endpoint to check the service health.  


## Getting Started

### Prerequisites

- .NET 8.0
- Redis (optional, used for reload valid refreshTokens when re-deploy the project)

### Configuration

In `appsettings.json`, the configuration is as follows:

```json
"TokenServiceConfig": {
  "S2S_KEY": "[set_s2s_key_with_any_length]",
  "SIGN_TOKEN_KEY": "[set_HMAC_key_at_least_32_bytes]",
  "UseEnvironmentVariablesFirst": true,
  "DefaultAccessTokenExpireMinutes": 30,
  "DefaultRefreshTokenExpireHours": 144,
  "RedisConfig": {
    "EnableRedis": false,
    "ConnectionString": "",
    "ValidRefreshTokenIdKeyName": "ValidRefreshTokenId"
  }
}
```

- `SIGN_TOKEN_KEY`: The key used to sign the tokens.
- `S2S_KEY`: The service-to-service key. Used to call generateTokenPair and disableRefreshToken APIs. Filled in "Authorization" header.
- `UseEnvironmentVariablesFirst`: If true, you can set or update `S2S_KEY` or `SIGN_TOKEN_KEY` in environment variables without redeploying the service.
- `DefaultAccessTokenExpireMinutes`: The default expiration time for access tokens in minutes.
- `DefaultRefreshTokenExpireHours`: The default expiration time for refresh tokens in hours.
- `RedisConfig`: Configuration for Redis, if you want to use Redis to store valid refresh token ids.
    - `EnableRedis`: If true, the service will use Redis to store valid refresh token ids.
    - `ConnectionString`: The connection string to the Redis server.
    - `ValidRefreshTokenIdKeyName`: The key name to store valid refresh token ids in a Redis Set.


### Run the tests
See and run `TokenServiceTest.cs`.

### Swagger UI tests
Run the service locally with Debug mode, then the swagger UI will auto open in the browser.
To test the APIs, remember to fill the `S2S_KEY` use the `Authorize` button in swagger UI, then swagger will set it in the `Authorization` for your requests. Otherwise you will get a `401 Unauthorized`.


## APIs Usage

### POST /TokenService/generateTokenPair

Generates a new access token and refresh token pair.

- **Authorization:** S2S_KEY (Service to service auth key)
- **Request Body:**
  ```json
  {
    "claims": {
      "username": "username",
      "infomation": "infomation"
    }
  }
  ```
- **Response Body:**
  ```json
  {
    "accessToken": "GeneratedAccessToken",
    "refreshToken": "GeneratedRefreshToken"
  }
  ```

### POST /TokenService/refreshTokenPair

Generates a new pair of access and refresh tokens using an existing refresh token.

- **Request Body:**
  ```json
  {
    "accessToken": "YourCurrentAccessToken",    
    "refreshToken": "YourCurrentRefreshToken"
  }
  ```
  ```
- **Response Body:**
  ```json
  {
    "accessToken": "NewAccessToken",
    "refreshToken": "NewRefreshToken"
  }
  ```

### POST /TokenService/validateAccessToken

Validates the provided access token.

- **Request Body:**
  ```json
  {
    "accessToken": "YourAccessToken"
  }
  ```
- **Response Body:**
  ```json
  {
    "isValid": true
  }
  ```

### POST /TokenService/validateRefreshToken

Validates the provided refresh token.

- **Request Body:**
  ```json
  {
    "refreshToken": "YourRefreshToken"
  }
  ```
- **Response Body:**
  ```json
  {
    "isValid": true
  }
  ```

### POST /TokenService/validateTokenPair

Validates both the access token and the refresh token.

- **Request Body:**
  ```json
  {
    "accessToken": "YourAccessToken",
    "refreshToken": "YourRefreshToken"
  }
  ```
- **Response Body:**
  ```json
  {
    "isValid": true
  }
  ```
### POST /TokenService/disableRefreshToken

Disables a given refresh token.

- **Authorization:** S2S_KEY
- **Request Body:**
  ```json
  {
    "refreshToken": "YourRefreshToken"
  }
  ```
- **Response** Http status code 200 if success

### GET /TokenService/ping

Returns "pong" as a response. Used for checking the service's health.
  

