﻿using ElastiBuild.Commands;

namespace ElastiBuild.Infra
{
    public class ArtifactFilter
    {
        public string ContainerId { get; set; }
        public bool ShowOss { get; set; }
        public eBitness Bitness { get; set; }
    }
}