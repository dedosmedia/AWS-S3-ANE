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



namespace AWSS3Lib {
   public class MainController : FreSharpMainController {
        private Hwnd _airWindow;


        /*
         *    AWS S3 Variables 
         */
        private bool _uploadInProgress = false;
        private DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private AmazonS3Client client;


        // Must have this function. It exposes the methods to our entry C++.
        public string[] GetFunctions() {
            FunctionsDict =
                new Dictionary<string, Func<FREObject, uint, FREObject[], FREObject>> {
                     {"init", InitController}
                    ,{"startS3Uploading", StartS3Uploading}



                };
            return FunctionsDict.Select(kvp => kvp.Key).ToArray();
        }

      


        /*
         *  
         * BEGIN AWS S3 FUNCTIONS
         *
         */
        public FREObject StartS3Uploading(FREContext ctx, uint argc, FREObject[] argv)
        {
            if (argv[0] == FREObject.Zero) return FREObject.Zero;
            if (_uploadInProgress == true) return false.ToFREObject();

            JObject parent;
            FileInfo imageFile;
            string key;
            FileInfo jsonFile = new FileInfo(argv[0].AsString());
            Dictionary<string,string> metadataDict;
            IEnumerable<JProperty> metadata = null;

            try
            {
                parent = JObject.Parse(File.ReadAllText(jsonFile.FullName));
                var filename = parent.Value<String>("file");
                imageFile = new FileInfo(jsonFile.Directory.FullName + "\\" + filename);  // y si es null??
                if (filename == null || imageFile.Exists == false)
                {
                    FileHelper.moveFileToSubdirectory(jsonFile,"error");
                    FreSharpHelper.ThrowFreException(FreResultSharp.FreInvalidArgument, "Property 'file' is missing in json or Image file does not exist. JSON moved to 'error' folder", FREObject.Zero);
                }

                key = parent.Value<String>("key");
                if (key == null)
                {
                    key = filename;
                }
            }
            catch (FileNotFoundException ex)
            {
                // Intentaron enviar un json que no existe
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
               Trace("Metadata key is missing");
            }

            uploadObject(jsonFile, imageFile, key, metadata);
            return true.ToFREObject();
        }

        async void uploadObject(FileInfo jsonFile, FileInfo imageFile, string key, IEnumerable<JProperty> metadata)
        {
            
            _uploadInProgress = true;
            int returnCode = await uploadObjectAsync(jsonFile, imageFile, key, metadata);
            _uploadInProgress = false;

            if (returnCode == 0)
            {
                Trace("> UPLOAD PROCESS: [FINISHED (1)] - " + jsonFile.Name);
                FileHelper.moveFileToSubdirectory(jsonFile, "done");
                FileHelper.moveFileToSubdirectory(imageFile, "done");
            }
            else if (returnCode < 0)
            {
                Trace("> UPLOAD PROCESS: [FAILED] - " + jsonFile.Name);

                FileHelper.moveFileToSubdirectory(jsonFile, "error");
                FileHelper.moveFileToSubdirectory(imageFile, "error");

            }
            else
            {
                Trace("> UPLOAD PROCESS: [NETWORK ERROR - RETRY LATER]");
            }

            // NOTIFICAR EVENTO, PARA INICIAR NUEVAMENTE EL PROCESO, SOLO CUANDO NO HAY ERROR DE RED
            if (returnCode <= 0)
            {
                SendEvent("UPLOAD_COMPLETE", "");
            }

        }


       

        async Task<int> uploadObjectAsync(FileInfo jsonFile, FileInfo imageFile, string key, IEnumerable<JProperty> metadata)
        {
            int returnCode = 0;
            try
            {
                
                var bucket = "keshot-dedosmedia";
               
                TransferUtility fileTransferUtility = new TransferUtility(client);
                TransferUtilityUploadRequest fileTransferUtilityRequest = new TransferUtilityUploadRequest
                {
                    BucketName = bucket,
                    FilePath = imageFile.FullName,
                    Key = "temp/photo.jpg",  
                    StorageClass = S3StorageClass.StandardInfrequentAccess,
                    CannedACL = S3CannedACL.PublicRead
                };

                var metadataDict = metadata
                            .ToDictionary(
                                k => k.Name,
                                v => v.Value.ToString());

                foreach (KeyValuePair<string, string> pair in metadataDict)
                {
                    Trace(string.Format("Key = {0}, Value = {1}", pair.Key, pair.Value));
                    fileTransferUtilityRequest.Metadata.Add("x-amz-meta-" + pair.Key, pair.Value.ToString());
                }
                await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
            }

            catch (AmazonS3Exception amazonS3Exception)
            {
                returnCode = 1;  // Credencials invalidas
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                    ||
                    amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    Trace("[ERROR] - Check the provided AWS Credentials.");

                }
                else
                {
                    Trace("[ERROR] - " + amazonS3Exception);
                }
            }
            catch (AmazonServiceException ex)
            {
                Trace("[ERROR] - AMAZON SERVICE EXCEPTION " + ex);
                returnCode = 2; // Error de red, no hay internet posiblemnte?
            }
            catch (KeyNotFoundException ex)
            {
                returnCode = -1;  // json mal formado... mover a error
                Trace("[ERROR] - METADATA KEY NOT FOUND " + ex);
            }
            catch (Exception ex)
            {
                returnCode = -2; // Error generic, no sabemos, mover a error
                Trace("[ERROR] - UNKNOWN ERROR " + ex);
            }

            return returnCode;
        }

        /*
        *  
        * END AWS S3 FUNCTIONS
        *
        */




        public FREObject InitController(FREContext ctx, uint argc, FREObject[] argv) {

            FileHelper.Context = Context;

            // get a reference to the AIR Window HWND
            _airWindow = Process.GetCurrentProcess().MainWindowHandle;

            if (argv[0] == FREObject.Zero) return FREObject.Zero;
            if (argv[1] == FREObject.Zero) return FREObject.Zero;

            var accessKey = argv[0].AsString();
            var secretKey = argv[1].AsString();


            var region    = argv[2].AsString();

            // TODO: EndPoint configurable
            client = new AmazonS3Client(accessKey, secretKey, Amazon.RegionEndpoint.USEast1);

            Trace(" > AWS Client initialized.");
            return FREObject.Zero;



        }

       public override void OnFinalize() {
           
       }
        
   }


}
