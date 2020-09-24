﻿using System.Xml.Linq;
using JenkinsNET.Models;

namespace JenkinsNotifier
{
    public class JenkinsBuildWithProgress : JenkinsBuildBase
    {
        public long Progress { get; set; }

        public JenkinsBuildWithProgress(XNode node) : base(node)
        {
        }
    }
}