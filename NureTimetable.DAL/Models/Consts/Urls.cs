﻿using NureTimetable.Core.Models.Consts;
using NureTimetable.DAL.Models.Local;
using System;

namespace NureTimetable.DAL.Models.Consts
{
    public static class Urls
    {
        public static Uri CistApiEntityTimetable(TimetableEntityType entity, long entityId, DateTime dateStart, DateTime dateEnd) => 
            new($"http://cist.nure.ua/ias/app/tt/P_API_EVEN_JSON" +
                $"?type_id={(int)entity}" +
                $"&timetable_id={entityId}" +
                $"&time_from={new DateTimeOffset(dateStart.Date).ToUnixTimeSeconds()}" +
                $"&time_to={new DateTimeOffset(dateEnd.Date.AddDays(1)).ToUnixTimeSeconds()}" +
                $"&idClient={Keys.CistApiKey}");

        public static Uri CistApiAllGroups => 
            new($"http://cist.nure.ua/ias/app/tt/P_API_GROUP_JSON");

        public static Uri CistApiAllTeachers => 
            new($"http://cist.nure.ua/ias/app/tt/P_API_PODR_JSON");

        public static Uri CistApiAllRooms => 
            new($"http://cist.nure.ua/ias/app/tt/P_API_AUDITORIES_JSON");

        public static Uri CistSiteAllGroups(long? facultyId) => 
            new($"http://cist.nure.ua/ias/app/tt/WEB_IAS_TT_AJX_GROUPS?p_id_fac={facultyId}");

        public static Uri CistSiteAllTeachers(long facultyId = -1, long kafId = -1) => 
            new($"https://cist.nure.ua/ias/app/tt/WEB_IAS_TT_AJX_TEACHS?p_id_fac={facultyId}&p_id_kaf={kafId}");
    }
}
