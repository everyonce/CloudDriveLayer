﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CloudDriveLayer.CloudDriveModels
{
    #region Conversions
    public static class Convert
    {
        public static CloudDriveNode createCloudDriveNode(CloudDriveFile x)
        {
            return JsonConvert.DeserializeObject<CloudDriveNode>(JsonConvert.SerializeObject(x));
        }
        public static CloudDriveNode createCloudDriveNode(CloudDriveFolder x)
        {
            return JsonConvert.DeserializeObject<CloudDriveNode>(JsonConvert.SerializeObject(x));
        }
        public static CloudDriveFile createCloudDriveFile(CloudDriveFolder x)
        {
            return JsonConvert.DeserializeObject<CloudDriveFile>(JsonConvert.SerializeObject(x));
        }
        public static CloudDriveFile createCloudDriveFile(CloudDriveNode x)
        {
            return JsonConvert.DeserializeObject<CloudDriveFile>(JsonConvert.SerializeObject(x));
        }
        public static CloudDriveFolder createCloudDriveFolder(CloudDriveNode x)
        {
            return JsonConvert.DeserializeObject<CloudDriveFolder>(JsonConvert.SerializeObject(x));
        }
        public static CloudDriveFolder createCloudDriveFolder(CloudDriveFile x)
        {
            return JsonConvert.DeserializeObject<CloudDriveFolder>(JsonConvert.SerializeObject(x));
        }
    }
    #endregion Conversions

    public class CloudDriveNodeRequest
    {
        public string name;
        public string kind;
        public List<string> parents;
        public List<string> labels;
        public List<KeyValuePair<string, string>> properties;
        public string createdBy;

        public CloudDriveNodeRequest()
        {
            parents = new List<string>();
            labels = new List<string>();
            properties = new List<KeyValuePair<string, string>>();
        }
    }
    public class Video
    {
        String audioCodec;
        float rotate;
        int width;
        int audioChannels;
        float videoFrameRate;
        long videoBitRate;
        float duration;
        int height;
        long bitrate;
        int audioSampleRate;
        string videoCodec;
        string audioChannelLayout;
        int audioBitrate;
        string encoder;

    }
    public class ContentProperties
    {
        public UInt64 size;
        public int version;
        public String contentType;
        public string extension;
        public string md5;
        public Video video;

        public ContentProperties()
        {
            video = new Video();
        }
    }
    public class CloudDriveNode : CloudDriveNodeRequest
    {
        public string id;
        public string version;
        public DateTime modifiedDate;
        public DateTime createdDate;
        public string status;
        public ContentProperties contentProperties;
        public Dictionary<string, JObject> properties { get; set; }
        public CloudDriveNode()
        {
            contentProperties = new ContentProperties();
            properties = new Dictionary<string, JObject>();
        }
    }
    public class CloudDriveFolder : CloudDriveNode
    {
        public List<CloudDriveNode> children;
        public CloudDriveFolder()
        {
            children = new List<CloudDriveNode>();
        }
    }
    public class CloudDriveFile : CloudDriveNode
    {
        public FileDownloadStatus dlStatus;
        public CloudDriveFile()
        {
            dlStatus = new FileDownloadStatus();
        }

    }
    public class CloudDriveListResponse<T>
    {
        public Int32 count;
        public String nextToken;
        public List<T> data;
    }
    public class FileDownloadStatus
    {
        public SemaphoreSlim writeLock;
       // public Stream writingStream;
        public long lastReqBlockSize;
        public long largestSizeReq;
        public long tempFileMaxReq;
        public String tempFileName;
        public Boolean tempFileDone;
        public MemoryMappedFile mmf;
        public byte[] mmfKbSegments;
        public long mmfSize;
        public FileDownloadStatus()
        {
            writeLock = new SemaphoreSlim(1,1);
            //writingStream = new MemoryStream();
            mmf = null;
        }
    }
    public class propertySet
    {

    }
    
    public class Range<T> where T : IComparable
    {
        private readonly T start;

        private readonly T end;

        public Range(T start, T end)
        {
            if (start.CompareTo(end) < 0)
            {
                this.start = start;
                this.end = end;
            }
            else
            {
                this.start = end;
                this.end = start;
            }
        }

        public T Start
        {
            get
            {
                return this.start;
            }
        }

        public T End
        {
            get
            {
                return this.end;
            }
        }

        public static bool Intersect(Range<T> a, Range<T> b)
        {
            return !(b.Start.CompareTo(a.End) > 0 || a.Start.CompareTo(b.End) > 0);
        }

        public bool Intersect(Range<T> other)
        {
            return Intersect(this, other);
        }
    }
}
