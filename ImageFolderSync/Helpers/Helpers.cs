using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace ImageFolderSync.Helpers
{
    internal static class Stuff
    {
        // later make a listbox with this, so you can add or remove extensions
        public static string[] mediaExt = { "png", "gif", "jpg", "jpeg", "mp4", "webm", "webp" };

        public static Boolean IsDownloadable(string u)
        {

            for (int i = 0; i < mediaExt.Length; i++)
            {
                if (u.ToLower().Contains($".{mediaExt[i]}")) // .png .gif .jpg   and so on
                {
                    return true;
                }
            }

            return false;
        }

        public static string RemoveInvalids(string p)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();

            // cut off any anti twitter garbage + anti resize garbage
            p = p.Split(":")[0].Split("?")[0].Split("&")[0]; 

            if (p.IndexOfAny(invalidChars) > -1)
            {
                for (int i = 0; i < invalidChars.Length; i++)
                {
                    p = p.Replace(invalidChars[i], '-');
                }
            }

            return p;
        }

        public static string CreateValidFilepath(string savePath, string filename, string extension, string? fileIndex = null)
        {
            string newExtension = "png";

            for (int i = 0; i < mediaExt.Length; i++)
            {
                if (extension.ToLower().Contains(mediaExt[i]))
                {
                    newExtension = mediaExt[i];
                }
            }

            fileIndex = fileIndex == null ? "" : "-duplicate-" + fileIndex;
            string p = string.Format(@"{0}/{1}{3}.{2}", savePath, filename, newExtension, fileIndex);

            if (p.Length > 200)
            {
                // without filename
                if (p.Length - filename.Length < 200)
                {
                    filename = filename.Length > 30 ? filename.Substring(0, 30) : filename;
                }

                p = string.Format(@"{0}/{1}{3}.{2}", savePath, filename, newExtension, fileIndex);
            }

            return p;
        }

    }
}
