using System;
using System.IO;
using System.Reflection;

namespace AWSS3Lib
{
    public class AssemblyResolver
    {
        private const string NewtonsoftName = "Newtonsoft.Json";

        public static Assembly MyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            var directoryAssembly = Path.GetDirectoryName(Assembly.GetAssembly(typeof(AssemblyResolver)).Location);

            var nameAssembly = args.Name.Split(',')[0];

            if (nameAssembly.StartsWith(NewtonsoftName, StringComparison.InvariantCultureIgnoreCase))
            {
                var plugin = Path.Combine(directoryAssembly, nameAssembly) + ".dll";

                if (File.Exists(plugin))
                    return Assembly.LoadFile(plugin);
            }

            return null;
        }
    }
}