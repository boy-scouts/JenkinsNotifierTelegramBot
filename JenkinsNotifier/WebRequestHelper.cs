﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JenkinsNotifier
{
    public class WebRequestHelper
    {
        public static readonly HttpClient Client = new HttpClient();
    }
}