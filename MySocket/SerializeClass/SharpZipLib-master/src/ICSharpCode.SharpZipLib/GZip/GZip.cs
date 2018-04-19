using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.GZip
{
    /// <summary>
    /// An example class to demonstrate compression and decompression of GZip streams.
    /// </summary>
    public static class GZip
    {
        /// <summary>
        /// Decompress the <paramref name="inStream">input</paramref> writing 
        /// uncompressed data to the <paramref name="outStream">output stream</paramref>
        /// </summary>
        /// <param name="inStream">The readable stream containing data to decompress.</param>
        /// <param name="outStream">The output stream to receive the decompressed data.</param>
        /// <param name="isStreamOwner">Both streams are closed on completion if true.</param>
        public static void Decompress(Stream inStream, Stream outStream, bool isStreamOwner)
        {
            if (inStream == null || outStream == null)
            {
                throw new Exception("Null Stream");
            }

            try
            {
                using (GZipInputStream bzipInput = new GZipInputStream(inStream))
                {
                    bzipInput.IsStreamOwner = isStreamOwner;
                    Core.StreamUtils.Copy(bzipInput, outStream, new byte[4096]);
                }
            }
            finally
            {
                if (isStreamOwner)
                {
                    // inStream is closed by the GZipInputStream if stream owner
                    outStream.Dispose();
                }
            }
        }

        /// <summary>
        /// Compress the <paramref name="inStream">input stream</paramref> sending 
        /// result data to <paramref name="outStream">output stream</paramref>
        /// </summary>
        /// <param name="inStream">The readable stream to compress.</param>
        /// <param name="outStream">The output stream to receive the compressed data.</param>
        /// <param name="isStreamOwner">Both streams are closed on completion if true.</param>
        /// <param name="level">Block size acts as compression level (1 to 9) with 1 giving 
        /// the lowest compression and 9 the highest.</param>
        public static void Compress(Stream inStream, Stream outStream, bool isStreamOwner, int level)
        {
            if (inStream == null || outStream == null)
            {
                throw new Exception("Null Stream");
            }

            try
            {
                using (GZipOutputStream bzipOutput = new GZipOutputStream(outStream, level))
                {
                    bzipOutput.IsStreamOwner = isStreamOwner;
                    Core.StreamUtils.Copy(inStream, bzipOutput, new byte[4096]);
                }
            }
            finally
            {
                if (isStreamOwner)
                {
                    // outStream is closed by the GZipOutputStream if stream owner
                    inStream.Dispose();
                }
            }
        }

        /// <summary>
        /// 压缩字节数组
        /// </summary>
        /// <param name="str"></param>
        public static byte[] ByteCompress(byte[] inputBytes)
        {
            MemoryStream ms = new MemoryStream();
            GZipOutputStream gzip = new GZipOutputStream(ms);
            gzip.Write(inputBytes, 0, inputBytes.Length);
            gzip.Close();
            byte[] press = ms.ToArray();
            return press;
        }

        /// <summary>
        /// 解压缩字节数组
        /// </summary>
        /// <param name="str"></param>
        public static byte[] ByteDecompress(byte[] inputBytes)
        {
            GZipInputStream gzi = new GZipInputStream(new MemoryStream(inputBytes));
            MemoryStream re = new MemoryStream();
            int count = 0;
            byte[] data = new byte[4096];
            while ((count = gzi.Read(data, 0, data.Length)) != 0)
            {
                re.Write(data, 0, count);
            }
            byte[] depress = re.ToArray();
            return depress;
        }



    }
}
