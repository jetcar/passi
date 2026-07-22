package com.passi.cloud.passi_android.data.remote

import com.google.gson.FieldNamingPolicy
import com.google.gson.Gson
import com.google.gson.GsonBuilder
import com.google.gson.JsonSyntaxException
import com.google.gson.annotations.SerializedName
import java.io.BufferedReader
import java.io.IOException
import java.io.OutputStreamWriter
import java.net.ConnectException
import java.net.HttpURLConnection
import java.net.SocketTimeoutException
import java.net.URL
import java.net.UnknownHostException
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

data class ApiErrorResponse(
    @SerializedName("errors")
    val errors: String? = null,
)

data class ApiResult(
    val isSuccessful: Boolean,
    val statusCode: Int,
    val body: String,
)

class PassiApiClient(
    private val gson: Gson = GsonBuilder()
        .setFieldNamingPolicy(FieldNamingPolicy.UPPER_CAMEL_CASE)
        .disableHtmlEscaping()
        .create(),
    private val logger: (String) -> Unit = { message -> println(message) },
) {
    suspend fun postJson(baseUrl: String, path: String, payload: Any): ApiResult {
        return executeJson(baseUrl = baseUrl, path = path, method = "POST", payload = payload)
    }

    suspend fun delete(baseUrl: String, path: String): ApiResult {
        return executeJson(baseUrl = baseUrl, path = path, method = "DELETE", payload = null)
    }

    suspend fun get(baseUrl: String, path: String): ApiResult {
        return executeJson(baseUrl = baseUrl, path = path, method = "GET", payload = null)
    }

    private suspend fun executeJson(baseUrl: String, path: String, method: String, payload: Any?): ApiResult {
        return withContext(Dispatchers.IO) {
            val url = URL(baseUrl.trimEnd('/') + path)
            val requestId = System.nanoTime().toString()
            val startedAtMs = System.currentTimeMillis()
            val payloadJson = payload?.let {
                runCatching { gson.toJson(it) }
                    .getOrElse { error -> "<payload serialization failed: ${error.message}>" }
            }
            log("[$requestId] -> $method ${url}")
            if (payloadJson != null) {
                log("[$requestId] payload=${payloadJson.truncateForLog()}")
            }

            val connection = (url.openConnection() as HttpURLConnection).apply {
                requestMethod = method
                connectTimeout = 30_000
                readTimeout = 30_000
                doInput = true
                doOutput = payload != null
                setRequestProperty("Content-Type", "application/json")
                setRequestProperty("Accept", "application/json")
            }

            try {
                if (payload != null) {
                    OutputStreamWriter(connection.outputStream, Charsets.UTF_8).use { writer ->
                        writer.write(payloadJson ?: gson.toJson(payload))
                        writer.flush()
                    }
                }

                val statusCode = connection.responseCode
                val body = readResponseBody(connection, statusCode)
                val durationMs = System.currentTimeMillis() - startedAtMs
                log("[$requestId] <- $statusCode (${durationMs}ms) body=${body.truncateForLog()}")
                ApiResult(
                    isSuccessful = statusCode in 200..299,
                    statusCode = statusCode,
                    body = body,
                )
            } catch (error: Throwable) {
                if (error is CancellationException) {
                    throw error
                }
                val durationMs = System.currentTimeMillis() - startedAtMs
                val mappedMessage = mapNetworkErrorMessage(error)
                log("[$requestId] !! ${error.javaClass.simpleName} after ${durationMs}ms: ${error.message.orEmpty()}")
                ApiResult(
                    isSuccessful = false,
                    statusCode = 0,
                    body = gson.toJson(ApiErrorResponse(errors = mappedMessage)),
                )
            } finally {
                connection.disconnect()
            }
        }
    }

    fun extractErrorMessage(body: String): String? {
        if (body.isBlank()) {
            return null
        }

        return try {
            gson.fromJson(body, ApiErrorResponse::class.java)?.errors
        } catch (_: JsonSyntaxException) {
            null
        }
    }

    fun <T> parseBody(body: String, type: Class<T>): T? {
        if (body.isBlank()) {
            return null
        }

        return try {
            gson.fromJson(body, type)
        } catch (_: JsonSyntaxException) {
            null
        }
    }

    private fun readResponseBody(connection: HttpURLConnection, statusCode: Int): String {
        val stream = if (statusCode in 200..299) connection.inputStream else connection.errorStream
        if (stream == null) {
            return ""
        }

        return stream.bufferedReader().use(BufferedReader::readText)
    }

    private fun log(message: String) {
        logger("PassiApiClient $message")
    }

    private fun mapNetworkErrorMessage(error: Throwable): String {
        return when (error) {
            is SocketTimeoutException -> "Request timed out. Check your connection and try again"
            is UnknownHostException -> "Cannot reach server. Check your connection and provider URL"
            is ConnectException -> "Unable to connect to server. Try again later"
            is IOException -> "Network error. Try again"
            else -> error.message?.takeIf { it.isNotBlank() } ?: "Unexpected error. Try again"
        }
    }

    private fun String.truncateForLog(limit: Int = 2_000): String {
        if (length <= limit) {
            return this
        }
        return take(limit) + "..."
    }
}