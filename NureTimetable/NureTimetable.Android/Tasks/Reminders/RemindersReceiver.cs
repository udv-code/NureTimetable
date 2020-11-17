using System;
using Android.App;
using Android.Content;
using AndroidX.Core.App;
using NureTimetable.Droid;

namespace NureTimetable.Droid.Tasks.Reminders
{
    [BroadcastReceiver]
    public class RemindersReceiver : BroadcastReceiver
    {
        public const String ExtraKeyMessage = "message";
        public const String ExtraKeyTitle = "title";
        public const int ReminderNotificationId = 3783;
        
        public const String RemindersChannelId = "com.nure.timetable.Reminders";
        public const String RemindersChannelName = "Reminders";
        public const String RemindersChannelDesc = "Reminders for upcoming lessons";
        
        public static void CreateNotificationChannel(Context context)
        {
            const NotificationImportance importance = NotificationImportance.Default;
            var channel = new NotificationChannel(RemindersChannelId, RemindersChannelName, importance)
            {
                Description = RemindersChannelDesc
            };

            var notificationManager =
                context.GetSystemService(Context.NotificationService) as NotificationManager;
            notificationManager.CreateNotificationChannel(channel);
        }
        
        public override void OnReceive (Context context, Intent intent)
        {
            var title = intent.GetStringExtra(ExtraKeyTitle);
            var message = intent.GetStringExtra(ExtraKeyMessage);
            
            var resultIntent = new Intent(context, typeof(MainActivity));
            resultIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);

            var pending = PendingIntent.GetActivity(context, 0, resultIntent, PendingIntentFlags.CancelCurrent);

            var builder = new NotificationCompat.Builder(context, RemindersChannelId)
                .SetContentTitle(title)
                .SetContentText(message)
                .SetPriority(NotificationCompat.PriorityHigh)
                .SetSmallIcon(Resource.Drawable.icon)
                .SetCategory(NotificationPriorityCategory.Reminders.ToString())
                .SetDefaults(NotificationCompat.DefaultAll);

            builder.SetContentIntent(pending);
            var notification = builder.Build();
            
            // TODO: add snooze?
            var notificationManager = NotificationManagerCompat.From(context);
            notificationManager.Notify(ReminderNotificationId, notification);
        }
    }
}