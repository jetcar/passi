# Passi — notes for Claude

## Redis (`RedisClient/RedisService.cs`)

`RedisService` stores values through ServiceStack's `IRedisClient.Set<string>`, which
JSON-encodes the already-Newtonsoft-serialized payload a second time (it is stored as a quoted
JSON string literal). `IRedisClient.Get<string>` undoes that encoding; most other read paths do not:

- commands queued in `CreateTransaction()` / pipelines (`QueueCommand`)
- `GetValue`, `Custom(...)`, Lua scripts, raw `redis-cli` reads

When adding or changing a read path, decode the raw value with
`RedisService.DecodeStoredString` before `JsonConvert.DeserializeObject`, and keep the write
format unchanged (live keys in production use it).

**Why:** #56 switched `GetAndDelete` to a MULTI/EXEC transaction reading `Get<string>` inside
`QueueCommand`, got the raw quoted string back, and every OIDC `/connect/token` threw — all logins
returned 500 until #62.

## Tests and fakes

The test projects replace `IRedisService` with in-memory fakes that store objects directly, so
they never exercise the real serialization. A change to `RedisService` itself needs a test that
goes through the real encode/decode (see `OpenIDCTests/RedisServiceDecodeTests.cs`), not only a
test against a fake.

## After deploying auth changes

Merging to `main` deploys automatically (`build-and-push-with-build-number-version.yml`). After a
change touching OpenIDC, `RedisClient`, or the login flow, do one real login on
https://passi.cloud and check Cloud Logging for `service=openidc` errors (see the
`passi-gcp-infra` memory for the query).
