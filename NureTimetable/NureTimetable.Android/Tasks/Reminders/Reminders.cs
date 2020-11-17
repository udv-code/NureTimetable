using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Java.Interop;

namespace NureTimetable.Droid.Tasks.Reminders
{
    public static class Globals
    {
        public static string TAG_REMINDERS = "Reminders";
    }

    public class Reminders
    {
        private const int HoursOffsetDefault = 9;

        private readonly Context _context;

        private Reminders(Context context)
        {
            this._context = context;
        }

        public static Reminders From(Context context)
        {
            return new Reminders(context);
        }

        public void Schedule(DateTime lessonTime, int offset = HoursOffsetDefault)
        {
            var alarmIntent = new Intent(_context, typeof(RemindersReceiver));
            alarmIntent.PutExtra(RemindersReceiver.ExtraKeyTitle, "Hello");
            alarmIntent.PutExtra(RemindersReceiver.ExtraKeyMessage, "World!");

            var pending =
                PendingIntent.GetBroadcast(_context, 0, alarmIntent, PendingIntentFlags.UpdateCurrent);

            var alarmManager = _context.GetSystemService(Context.AlarmService).JavaCast<AlarmManager>();
            alarmManager.Set(AlarmType.ElapsedRealtime, lessonTime.Millisecond + 5 * 1000, pending);

            Log.Debug(Globals.TAG_REMINDERS, $"Reminder scheduled for {lessonTime}!");
        }
    }
}