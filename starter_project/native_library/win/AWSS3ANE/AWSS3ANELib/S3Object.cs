using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

public class S3Object
{
    public FileInfo JsonFile { get; set; }
    public FileInfo ImageFile { get; set; }
    public string Key { get; set; }
    public string Bucket { get; set; }
    public IEnumerable<JProperty> Metadata { get; set; }


}

