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
                if (u.Contains($".{mediaExt[i]}")) // .png .gif .jpg   and so on
                {
                    return true;
                }
            }

            return false;
        }

        public static string RemoveInvalids(string p)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();

            if (p.IndexOfAny(invalidChars) > 0)
            {
                for (int i = 0; i < invalidChars.Length; i++)
                {
                    p = p.Replace(invalidChars[i], '-');
                }
            }

            return p;
        }

        public static string FixGarbageUrlsIntoPath(string savePath, string filename, string extension, string? fileIndex = null)
        {
            fileIndex = fileIndex == null ? "" : "_" + fileIndex;
            string p = string.Format(@"{0}/{1}{3}.{2}", savePath, filename, extension, fileIndex);

            if (p.Length > 200)
            {
                // without filename
                if (p.Length - filename.Length < 200 )
                {
                    filename = filename.Length > 30 ? filename.Substring(0, 30) : filename;
                    p = string.Format(@"{0}/{1}{3}.{2}", savePath, filename, extension, fileIndex);
                }

                // without extension
                if (p.Length - extension.Length < 200)
                {
                    string newExt = "png";

                    for (int i = 0; i < mediaExt.Length; i++)
                    {
                        if (extension.Contains(mediaExt[i]))
                        {
                            newExt = mediaExt[i];
                        }
                    }
                    p = string.Format(@"{0}/{1}{3}.{2}", savePath, filename, newExt, fileIndex);
                }
            }

            return p;
        }

    }
}
