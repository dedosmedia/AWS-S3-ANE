using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSS3Lib
{
    class S3Event
    {
        public  const string CREDENTIALS_INVALID = "credentialsInvalid";
        public const string CLIENT_INITIALIZED = "clientInitialized";
        public const string OBJECT_UPLOADED = "objectUploaded";
        public const string NETWORK_ERROR = "networkError";
        public const string FILE_IO_ERROR = "fileIOError";
        public const string UNKNOWN_ERROR = "unknownError";
        public const string REGION_ERROR = "regionError";
        public const string BUCKET_ERROR = "bucketError";
    }
}
