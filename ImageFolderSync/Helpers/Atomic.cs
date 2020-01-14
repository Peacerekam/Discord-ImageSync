using System;
using System.IO;

namespace ImageFolderSync.Helpers
{
    internal static class Atomic
    {
        public static void WriteFile(string filePath, Stream fileStream)
        {
            string tempFilePath = Path.GetTempFileName();
            using (FileStream tempFile = File.OpenWrite(tempFilePath))
            {
                fileStream.CopyTo(tempFile);
            }

            try
            {
                File.Move(tempFilePath, filePath);
            }
            catch
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch
                {
                    // total fail, whatever
                }

                throw;
            }
        }

        public static void OverwriteFile(string filePath, Stream fileStream, string backupName)
        {
            string tempFilePath = Path.GetTempFileName();
            using (FileStream tempFile = File.OpenWrite(tempFilePath))
            {
                fileStream.CopyTo(tempFile);
            }

            try
            {
                File.Replace(tempFilePath, filePath, backupName);
                //File.Move(tempFilePath, filePath);
            }
            catch
            {
                try
                {
                    //File.Delete(tempFilePath);
                }
                catch
                {
                    // total fail, whatever
                }

                throw;
            }
        }
    }
}
