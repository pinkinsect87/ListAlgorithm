using System;
using System.IO;
using System.IO.Compression;

namespace CultureSurveyShared.BadgeDownloads
{
    public static class ZipHelpers
    {

        public static void AddStreamToZipArchive(ZipArchive archive, Stream streamedFile, string filename)
        {

            // Add a streamed file to the archive
            ZipArchiveEntry newFile = archive.CreateEntry(filename);
            using (Stream entryStream = newFile.Open())
            {
                using (Stream fileToCompressStream = streamedFile)
                {
                    fileToCompressStream.CopyTo(entryStream);
                }
            }
        }

        public static void AddStringToZipArchive(ZipArchive archive, string textString, string filename)
        {
            Stream streamedString = GenerateStreamFromString(textString);
            ZipHelpers.AddStreamToZipArchive(archive, streamedString, filename);
        }

        private static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

    }
}
