using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.App;
using Android.Util;
using AndroidX.Core.App;
using Firebase.Messaging;
using Java.Util;
using Newtonsoft.Json;
using WebApiDto;

namespace passi_android.Droid.Notifications
{
    [Service(Exported = false, DirectBootAware = true, ForegroundServiceType = ForegroundService.TypeDataSync, Enabled = true)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseMessagingService : FirebaseMessagingService
    {
        public MyFirebaseMessagingService()
        {
        }

        private const string TAG = "MyFirebaseMsgService";

        public override void OnMessageReceived(RemoteMessage p0)
        {
            base.OnMessageReceived(p0);
        }

        private void SendNotification(string title, string body)
        {
            CreateNotificationChannel();

            Intent launchIntent = PackageManager.GetLaunchIntentForPackage("com.passi.cloud.passi_android");
            //Intent launchIntent = PackageManager.GetLaunchIntentForPackage("com.google.android.youtube");
            if (launchIntent != null)
            {
                StartActivity(launchIntent);
            }

            Log.Debug(TAG, "Notification Message Body2: ");
            System.Diagnostics.Debug.WriteLine("test");
            NotificationManager notificationManager = (NotificationManager)ApplicationContext.GetSystemService(Context.NotificationService);

            Intent notificationIntent = new Intent(ApplicationContext, typeof(MainActivity));
            notificationIntent.PutExtra("notification", "true");

            notificationIntent.PutExtra("title", title);
            notificationIntent.PutExtra("body", body);

            notificationIntent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop | ActivityFlags.NewTask);
            ApplicationContext.StartActivity(notificationIntent);
            Log.Debug(TAG, "SetFlags: ");

            PendingIntent pendingIntent = PendingIntent.GetActivity(ApplicationContext, 0, notificationIntent, PendingIntentFlags.Immutable);
            var msg = JsonConvert.DeserializeObject<FirebaseNotificationDto>(body);
            var notificationBuilder = new NotificationCompat.Builder(ApplicationContext, MainActivity.CHANNEL_ID)
                .SetSmallIcon(Resource.Drawable.xamagonBlue)
                .SetChannelId(MainActivity.CHANNEL_ID)
                .SetContentTitle(title)
                .SetContentText(msg.ReturnHost)
                //   .SetBadgeIconType(0)
                .SetFullScreenIntent(pendingIntent, true)
                .SetTimeoutAfter(90 * 1000)
                   .SetVibrate(new long[] { 1, 0, 1, 0, 1, 0, 1, 0, 1 })
                .SetLargeIcon(BitmapFactory.DecodeResource(Resources, Resource.Drawable.xamagonBlue))
                 .SetAutoCancel(true)
                 .SetNumber(1)
                 .SetLights(16713728, 100, 100)
                .SetDefaults((int)NotificationDefaults.Sound | (int)NotificationDefaults.Vibrate)
                .SetContentIntent(pendingIntent);
            Log.Debug(TAG, "builder: ");

            //Notification notification = new Notification(Resource.Drawable.xamagonBlue, messageBody, 0);
            // notification.SetLatestEventInfo(this, "title", "message", pendingIntent);
            // notification.Flags |= NotificationFlags.AutoCancel;

            notificationManager.Notify(new Random().NextInt(100000), notificationBuilder.Build());
            Log.Debug(TAG, "notify: ");
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return;
            }

            var channel = new NotificationChannel(MainActivity.CHANNEL_ID,
                "FCM Notifications",
                NotificationImportance.Default)
            {
                Description = "Firebase Cloud Messages appear in this channel",
                LockscreenVisibility = NotificationVisibility.Public,
                Importance = NotificationImportance.High
            };
            //channel.SetAllowBubbles(true);
            var notificationManager = (NotificationManager)GetSystemService(global::Android.Content.Context.NotificationService);
            notificationManager.CreateNotificationChannel(channel);
        }

        public override void HandleIntent(Intent p0)
        {
            var title = p0.Extras.GetString("title");
            var body = p0.Extras.GetString("body");
            var keys = p0.Extras.KeySet();
            var dict = new Dictionary<string, string>();
            foreach (var key in keys)
            {
                var value = p0.Extras.GetString(key);
                dict.Add(key, value);
            }
            if (body != null)
                SendNotification(title, body);
        }
    }
}