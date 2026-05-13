package com.passi.cloud.passi_android.data.remote

import com.google.gson.FieldNamingPolicy
import com.google.gson.Gson
import com.google.gson.GsonBuilder
import com.google.gson.JsonSyntaxException
import com.google.gson.annotations.SerializedName
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.io.BufferedReader
import java.io.OutputStreamWriter
import java.net.HttpURLConnection
import java.net.URL

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
) {
    suspend fun postJson(baseUrl: String, path: String, payload: Any): ApiResult {
        return executeJson(baseUrl = baseUrl, path = path, method = "POST", payload = payload)
    }

    suspend fun delete(baseUrl: String, path: String): ApiResult {
        return executeJson(baseUrl = baseUrl, path = path, method = "DELETE", payload = null)
    }

    private suspend fun executeJson(baseUrl: String, path: String, method: String, payload: Any?): ApiResult {
        return withContext(Dispatchers.IO) {
            val url = URL(baseUrl.trimEnd('/') + path)
            val requestBody = if (payload != null) gson.toJson(payload) else null
            println("[API] --> $method ${url}")
            if (requestBody != null) println("[API]     body: $requestBody")

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
                if (requestBody != null) {
                    OutputStreamWriter(connection.outputStream, Charsets.UTF_8).use { writer ->
                        writer.write(requestBody)
                        writer.flush()
                    }
                }

                val statusCode = connection.responseCode
                val body = readResponseBody(connection, statusCode)
                println("[API] <-- $statusCode $method ${url}")
                if (body.isNotEmpty()) println("[API]     body: $body")
                ApiResult(
                    isSuccessful = statusCode in 200..299,
                    statusCode = statusCode,
                    body = body,
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

    private fun readResponseBody(connection: HttpURLConnection, statusCode: Int): String {
        val stream = if (statusCode in 200..299) connection.inputStream else connection.errorStream
        if (stream == null) {
            return ""
        }

        return stream.bufferedReader().use(BufferedReader::readText)
    }
}
