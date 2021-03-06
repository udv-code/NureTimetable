﻿using Microsoft.AppCenter.Analytics;
using NureTimetable.Core.Localization;
using NureTimetable.Core.Models.Consts;
using NureTimetable.DAL;
using NureTimetable.DAL.Models.Local;
using Plugin.Calendars;
using Plugin.Calendars.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Calendar = Plugin.Calendars.Abstractions.Calendar;

namespace NureTimetable.BL
{
    public static class CalendarService
    {
        public const string CustomCalendarName = "NURE Timetable";

        public static async Task<bool> CheckPermissions()
        {
            PermissionStatus readStatus = await Permissions.CheckStatusAsync<Permissions.CalendarRead>();
            PermissionStatus writeStatus = await Permissions.CheckStatusAsync<Permissions.CalendarWrite>();
            return readStatus == PermissionStatus.Granted && writeStatus == PermissionStatus.Granted;
        }

        public static async Task<bool> RequestPermissions()
        {
            PermissionStatus readStatus = await Permissions.CheckStatusAsync<Permissions.CalendarRead>();
            PermissionStatus writeStatus = await Permissions.CheckStatusAsync<Permissions.CalendarWrite>();
            if (readStatus != PermissionStatus.Granted)
            {
                readStatus = await Permissions.RequestAsync<Permissions.CalendarRead>();
            }
            if (writeStatus != PermissionStatus.Granted && readStatus == PermissionStatus.Granted)
            {
                writeStatus = await Permissions.RequestAsync<Permissions.CalendarWrite>();
            }
            return readStatus == PermissionStatus.Granted && writeStatus == PermissionStatus.Granted;
        }

        public static async Task<Calendar> GetCalendar()
        {
            if (!await RequestPermissions())
            {
                return null;
            }

            IList<Calendar> calendars = await GetAllCalendars();

            Calendar defaultCalendar = calendars.SingleOrDefault(c => c.ExternalID == SettingsRepository.Settings.DefaultCalendarId);
            if (defaultCalendar is not null)
            {
                return defaultCalendar;
            }

            if (calendars.Count == 1)
            {
                return calendars.First();
            }

            string targetCalendarName = await Shell.Current.DisplayActionSheet(LN.ChooseCalendar, LN.Cancel, null, calendars.Select(c => c.Name).ToArray());
            if (targetCalendarName is null)
            {
                return null;
            }
            Calendar selectedCalendar = calendars.First(c => c.Name == targetCalendarName);
            return selectedCalendar;
        }

        public static async Task<IList<Calendar>> GetAllCalendars()
        {
            if (!await RequestPermissions())
            {
                return null;
            }

            // Getting Calendar list
            IList<Calendar> calendars = await CrossCalendars.Current.GetCalendarsAsync();
            calendars = calendars
                .Where(c => c.Name.ToLower() == c.AccountName.ToLower() || c.AccountName.ToLower() == CustomCalendarName.ToLower())
                .ToList();

            // Getting our custom calendar
            Calendar customCalendar = calendars.FirstOrDefault(c => c.AccountName.ToLower() == CustomCalendarName.ToLower());
            if (customCalendar is null)
            {
                customCalendar = new Calendar
                {
                    Name = CustomCalendarName,
                    Color = "#56a5de"
                };
                await CrossCalendars.Current.AddOrUpdateCalendarAsync(customCalendar);
                calendars.Add(customCalendar);
            }
            else if (calendars.Count(c => c.AccountName == customCalendar.AccountName) > 1)
            {
                MessagingCenter.Send(Application.Current, MessageTypes.ExceptionOccurred, new IndexOutOfRangeException($"There are {calendars.Count(c => c.AccountName == customCalendar.AccountName)} calendars with AccountName {customCalendar.AccountName}"));
            }

            return calendars;
        }

        public static CalendarEvent GenerateCalendarEvent(Event ev, int eventNumber, int eventsCount)
        {
            CalendarEvent calendarEvent = new()
            {
                Start = ev.StartUtc,
                End = ev.EndUtc,
                Name = $"{ev.Lesson.ShortName} - {ev.Type.ShortName} ({eventNumber}/{eventsCount})",
                Description = $"{string.Format(LN.EventClassroom, ev.RoomName)}\n" +
                    $"{string.Format(LN.EventTeachers, string.Join(", ", ev.Teachers.Select(t => t.Name)))}\n" +
                    $"{string.Format(LN.EventGroups, string.Join(", ", ev.Groups.Select(t => t.Name)))}\n",
                Location = $"KHNURE -\"{ev.RoomName}\"",
                Reminders = new CalendarEventReminder[]
                {
                    new()
                    {
                        Method = CalendarReminderMethod.Alert,
                        TimeBefore = TimeSpan.FromMinutes(30)
                    }
                }
            };
            return calendarEvent;
        }

        public static async Task<bool> AddOrUpdateEvent(Calendar calendar, CalendarEvent calendarEvent)
        {
            if (!await RequestPermissions())
            {
                return false;
            }

            Analytics.TrackEvent("Add To Calendar");

            static string GetUniqueNamePart(string n)
            {
                int lastSpace = n.LastIndexOf(" ");
                return lastSpace > 0 ? n[..lastSpace] : n;
            }

            IList<CalendarEvent> events = await CrossCalendars.Current.GetEventsAsync(calendar, calendarEvent.Start, calendarEvent.End);
            foreach (var existingEvent in events)
            {
                if (GetUniqueNamePart(existingEvent.Name) != GetUniqueNamePart(calendarEvent.Name))
                {
                    continue;
                }

                existingEvent.Name = calendarEvent.Name;
                existingEvent.Description = calendarEvent.Description;
                existingEvent.Start = calendarEvent.Start;
                existingEvent.End = calendarEvent.End;
                existingEvent.Location = calendarEvent.Location;
                existingEvent.Reminders = calendarEvent.Reminders;
                calendarEvent = existingEvent;
                break;
            }

            try
            {
                await CrossCalendars.Current.AddOrUpdateEventAsync(calendar, calendarEvent);
            }
            catch (Exception ex)
            {
                MessagingCenter.Send(Application.Current, MessageTypes.ExceptionOccurred, ex);
                return false;
            }

            return true;
        }
    }
}
