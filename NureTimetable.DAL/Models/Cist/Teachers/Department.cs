﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace NureTimetable.DAL.Models.Cist
{
    public class Department
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("short_name")]
        public string ShortName { get; set; }

        [JsonProperty("full_name")]
        public string FullName { get; set; }

        [JsonProperty("teachers")]
        public List<Teacher> Teachers { get; set; } = new();
    }
}
