﻿using System;
using System.Collections.Generic;
using System.IO;
using TuaRua.FreSharp;

namespace AWSS3Lib
{
    class FileHelper
    {

        public static FreContextSharp Context;

        public static void moveFileToSubdirectory(FileInfo srcFile, string subdirectory)
        {
            if (srcFile.Exists)
            {
                try
                {
                    //DirectoryInfo dstDirectory = srcFile.Directory.Parent.CreateSubdirectory(subdirectory);
                    DirectoryInfo dstDirectory = srcFile.Directory.CreateSubdirectory(subdirectory);
                    string dstPath = dstDirectory.FullName + "\\" + srcFile.Name;
                    srcFile.CopyTo(dstPath, true);
                    srcFile.Delete();
                }
                catch (Exception ex)
                {                    
                    Context.SendEvent("TRACE", "<<<ERROR>>> MOVING/DELETING FILE " + ex);
                }
            }
        }
    }


}
