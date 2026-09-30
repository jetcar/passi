# Gson maps these classes by reflection and derives the JSON keys from their field names, so neither the
# classes nor their fields may be renamed or removed. StoredAccount/StoredProvider are additionally
# persisted on the device: renaming a field would make already-enrolled accounts unreadable after an update.
-keep class com.passi.cloud.passi_android.data.remote.dto.** { *; }
-keep class com.passi.cloud.passi_android.data.remote.ApiErrorResponse { *; }
-keep class com.passi.cloud.passi_android.data.local.StoredAccount { *; }
-keep class com.passi.cloud.passi_android.data.local.StoredProvider { *; }
-keep class com.passi.cloud.passi_android.data.certificate.CertificateUpdateRequestDto { *; }
-keep class com.passi.cloud.passi_android.data.notifications.DeviceTokenUpdateRequestDto { *; }
-keep class com.passi.cloud.passi_android.notifications.FirebaseNotificationPayload { *; }
-keep enum com.passi.cloud.passi_android.domain.model.ConfirmationColor { *; }

# The Bouncy Castle provider registers its algorithm implementations by class name and instantiates them
# reflectively, so R8 cannot see that they are used.
-keep class org.bouncycastle.jcajce.provider.** { *; }
-keep class org.bouncycastle.jce.provider.** { *; }
# Bouncy Castle's LDAP certificate/CRL stores use JNDI, which Android does not have; the app never uses them.
-dontwarn javax.naming.**

# Keep line numbers so Play Console can retrace crash stack traces with the uploaded mapping file.
-keepattributes SourceFile,LineNumberTable
-renamesourcefileattribute SourceFile
