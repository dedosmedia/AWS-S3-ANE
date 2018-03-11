using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TuaRua.FreSharp;
using TuaRua.FreSharp.Exceptions;
using FREObject = System.IntPtr;
using FREContext = System.IntPtr;
using Hwnd = System.IntPtr;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3.Transfer;
using Amazon.S3;
using Amazon.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;


namespace AWSS3Lib {
   public class MainController : FreSharpMainController {
        private Hwnd _airWindow;
        private bool _debug = false;

        /*
         *    AWS S3 Variables 
         */
        private List<S3Object> _queue = new List<S3Object>();
        private AmazonS3Client client;
        private bool _uploadInProgress = false;
        private bool _clientInitialized = false;



        // Must have this function. It exposes the methods to our entry C++.
        public string[] GetFunctions() {
            FunctionsDict =
                new Dictionary<string, Func<FREObject, uint, FREObject[], FREObject>> {
                     {"init", InitController}
                    ,{"enqueue", Enqueue}
                    ,{"upload", Upload}

                };
            return FunctionsDict.Select(kvp => kvp.Key).ToArray();
        }


        public FREObject InitController(FREContext ctx, uint argc, FREObject[] argv)
        {
            Amazon.RegionEndpoint region;
            FileHelper.Context = Context;

            // get a reference to the AIR Window HWND
            _airWindow = Process.GetCurrentProcess().MainWindowHandle;

            if (argc > 3)
            {
                _debug = argv[3].AsBool();
            }

            if (argv[0] == FREObject.Zero || argv[1] == FREObject.Zero)
            {
                SendEvent(S3Event.CREDENTIALS_INVALID, "AWS Access Key and/or Secret key are missing.");
                return FREObject.Zero;
            } 

            var accessKey = argv[0].AsString();
            var secretKey = argv[1].AsString();

            switch (argv[2].AsString())
            {
                case "us-east-2":
                    region = Amazon.RegionEndpoint.USEast2;
                    break;
                case "us-west-1":
                    region = Amazon.RegionEndpoint.USWest1;
                    break;
                case "us-west-2":
                    region = Amazon.RegionEndpoint.USWest2;
                    break;
                case "ca-central-1":
                    region = Amazon.RegionEndpoint.CACentral1;
                    break;
                case "ap-south-1":
                    region = Amazon.RegionEndpoint.APSouth1;
                    break;
                case "ap-northeast-1":
                    region = Amazon.RegionEndpoint.APNortheast1;
                    break;
                case "ap-northeast-2":
                    region = Amazon.RegionEndpoint.APNortheast2;
                    break;
                case "ap-southeast-1":
                    region = Amazon.RegionEndpoint.APSoutheast1;
                    break;
                case "ap-southeast-2":
                    region = Amazon.RegionEndpoint.APSoutheast2;
                    break;
                case "cn-north-1":
                    region = Amazon.RegionEndpoint.CNNorth1;
                    break;
                case "cn-northwest-1":
                    region = Amazon.RegionEndpoint.CNNorthWest1;
                    break;
                case "eu-central-1":
                    region = Amazon.RegionEndpoint.EUCentral1;
                    break;
                case "eu-west-1":
                    region = Amazon.RegionEndpoint.EUWest1;
                    break;
                case "eu-west-2":
                    region = Amazon.RegionEndpoint.EUWest2;
                    break;
                case "eu-west-3":
                    region = Amazon.RegionEndpoint.EUWest3;
                    break;
                case "sa-east-1":
                    region = Amazon.RegionEndpoint.SAEast1;
                    break;
                default:
                    region = Amazon.RegionEndpoint.USEast1;
                    break;
            }

            client = new AmazonS3Client(accessKey, secretKey, region);
            _clientInitialized = true;

            SendEvent(S3Event.CLIENT_INITIALIZED, "AWS Client Initialized in region: "+region.DisplayName);
            return true.ToFREObject();
        }

        /*
         *  
         * BEGIN AWS S3 FUNCTIONS
         *
         */
        public FREObject Enqueue(FREContext ctx, uint argc, FREObject[] argv)
        {
            if (argv[0] == FREObject.Zero) return FREObject.Zero;

            JObject parent;
            FileInfo imageFile;
            string key;
            string bucket;
            IEnumerable<JProperty> metadata = null;
            FileInfo jsonFile = new FileInfo(argv[0].AsString());

            try
            {
                parent = JObject.Parse(File.ReadAllText(jsonFile.FullName));
                var filename = parent.Value<String>("file");
                imageFile = new FileInfo(jsonFile.Directory.FullName + "\\" + filename);
                if (filename == null || imageFile.Exists == false)
                {
                    FileHelper.moveFileToSubdirectory(jsonFile, "error");
                    FreSharpHelper.ThrowFreException(FreResultSharp.FreInvalidArgument, "Property 'file' is missing in json or Image file does not exist. JSON moved to 'error' folder", FREObject.Zero);
                }

                key = parent.Value<String>("key");
                if (key == null)
                {
                    key = filename;
                }

                bucket = parent.Value<String>("bucket");
                if (bucket == null)
                {
                    if (argc > 1)
                    {
                        bucket = argv[1].AsString();
                    }
                    else
                    {
                        //FreSharpHelper.ThrowFreException(FreResultSharp.FreInvalidArgument, "Bucket name is null", FREObject.Zero);
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                // JSON file missing
                return new FreException(ex).RawValue;
            }
            catch (JsonReaderException ex) {
                // el JSON está mal formado
                FileHelper.moveFileToSubdirectory(jsonFile, "error");
                return new FreException(ex).RawValue;
            }
            catch (Exception ex)
            {
                return new FreException(ex).RawValue;
            }

            try {
                metadata = parent.Value<JObject>("metadata").Properties();
            }
            catch (NullReferenceException ex)
            {
                trace("There is not Metadata");
            }

            S3Object item   = new S3Object();
            item.ImageFile  = imageFile;
            item.JsonFile   = jsonFile;
            item.Bucket = bucket;
            item.Key        = key;
            item.Metadata   = metadata;
            _queue.Add(item);
            return true.ToFREObject();
        }

        public FREObject Upload(FREContext ctx, uint argc, FREObject[] argv)
        {

            if (_uploadInProgress == false)
            {
                try {
                    if (_clientInitialized == false)   FreSharpHelper.ThrowFreException(FreResultSharp.FreActionscriptError, "[ERROR] AWS Client has not been initilized properly.", FREObject.Zero);
                }
                catch (Exception ex) {                    
                    return new FreException(ex).RawValue;
                }
                
                UploadObject();
            }
            return FREObject.Zero;

        }

        async void UploadObject()
        {
            if (_queue.Count == 0)
                return;

            _uploadInProgress = true;
            S3Object item = _queue[0];
            string returnCode = await UploadObjectAsync(item);


            switch (returnCode)
            {
                case S3Event.OBJECT_UPLOADED:
                    SendEvent(S3Event.OBJECT_UPLOADED, item.JsonFile.FullName);
                    _queue.Remove(item);
                    FileHelper.moveFileToSubdirectory(item.JsonFile, "done");
                    FileHelper.moveFileToSubdirectory(item.ImageFile, "done");
                    break;
                case S3Event.UNKNOWN_ERROR:
                    SendEvent(S3Event.UNKNOWN_ERROR, item.JsonFile.FullName);
                    _queue.Remove(item);
                    FileHelper.moveFileToSubdirectory(item.JsonFile, "error");
                    FileHelper.moveFileToSubdirectory(item.ImageFile, "error");
                    break;
                case S3Event.CREDENTIALS_INVALID:
                    SendEvent(S3Event.CREDENTIALS_INVALID, "Check the provided AWS Credentials.");
                    _queue.Remove(item);
                    break;
                case S3Event.FILE_IO_ERROR:
                    SendEvent(S3Event.FILE_IO_ERROR, item.ImageFile.FullName);
                    _queue.Remove(item);
                    break;
                case S3Event.NETWORK_ERROR:
                    SendEvent(S3Event.NETWORK_ERROR, "Network error");
                    break;
                case S3Event.REGION_ERROR:
                    SendEvent(S3Event.REGION_ERROR, "S3 Bucket region is incorrect");
                    _queue.Remove(item);
                    break;
                case S3Event.BUCKET_ERROR:
                    SendEvent(S3Event.BUCKET_ERROR, "S3 Bucket name is incorrect");
                    _queue.Remove(item);
                    break;
            }

            _uploadInProgress = false;
            if(_queue.Count > 0)
                UploadObject();

        }

        async Task<string> UploadObjectAsync(S3Object item)
        {
            string returnCode = S3Event.OBJECT_UPLOADED;
            try
            {
                TransferUtility fileTransferUtility = new TransferUtility(client);
                TransferUtilityUploadRequest fileTransferUtilityRequest = new TransferUtilityUploadRequest
                {
                    BucketName = item.Bucket,
                    FilePath = item.ImageFile.FullName,
                    Key = item.Key,
                    StorageClass = S3StorageClass.StandardInfrequentAccess,
                    CannedACL = S3CannedACL.PublicRead
                };

                if (item.Metadata != null)
                {
                    var metadataDict = item.Metadata
                                .ToDictionary(
                                    k => k.Name,
                                    v => v.Value.ToString());

                    foreach (KeyValuePair<string, string> pair in metadataDict)
                    {

                        var bytes = Encoding.UTF8.GetBytes(pair.Value);
                        var asBase64Str = Convert.ToBase64String(bytes);

                        // decode on NODEJS
                        // var utf8encoded = (new Buffer("VjAwMDEwNDE4MTE1MTI3", 'base64')).toString('utf8');

                        trace(string.Format("Key = {0}, Value = {1}, B64 {2}", pair.Key, pair.Value, asBase64Str));
                        fileTransferUtilityRequest.Metadata.Add("x-amz-meta-" + pair.Key, asBase64Str);
                    }
                }
                await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
            }

            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                    ||
                    amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    trace(amazonS3Exception.Message);
                    returnCode = S3Event.CREDENTIALS_INVALID;
                }
                else
                {
                    trace(amazonS3Exception.Message);
                    returnCode = S3Event.REGION_ERROR;
                }
            }
            catch (AmazonServiceException ex)
            {
                trace(ex.Message);
                returnCode = S3Event.NETWORK_ERROR;
            }
            catch (ArgumentException ex)
            {
                trace(ex.Message);
                returnCode = S3Event.FILE_IO_ERROR;
            }
            catch (NullReferenceException ex)
            {
                trace(ex.Message);
                returnCode = S3Event.BUCKET_ERROR; 
            }
            catch (InvalidOperationException ex)
            {
                trace(ex.Message);
                returnCode = S3Event.BUCKET_ERROR;
            }
            catch (Exception ex)
            {
                trace(ex.Message);
                returnCode = S3Event.UNKNOWN_ERROR;
            }

            return returnCode;
        }

        /*
        *  
        * END AWS S3 FUNCTIONS
        *
        */



        

        public void trace(params object[] values)
        {
            if (_debug)
            {
                var traceStr = values.Aggregate("", (current, value) => current + value + " ");
                Debug.WriteLine(traceStr);
                Trace(values);
            } 
        }



       public override void OnFinalize() {
           
       }
        
   }


}
