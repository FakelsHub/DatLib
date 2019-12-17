﻿using System;
using System.IO;

namespace DATLib
{
    internal class DATFile
    {
        internal BinaryReader br  { get; set; }
        internal String Path      { get; set; } // path and name in lower case
        internal String FileName  { get; set; }

        internal bool Compression { get; set; }
        internal int UnpackedSize { get; set; }
        internal int PackedSize   { get; set; }
        internal int Offset       { get; set; }
        internal int FileNameSize { get; set; }

        //internal long FileIndex { get; set; } // index of file in DAT
        internal string ErrorMsg  { get; set; }

        internal byte[] dataBuffer { get; set; } // Whole file

        private byte[] compressStream(MemoryStream mem)
        {
            MemoryStream outStream = new MemoryStream();
            zlib.ZOutputStream outZStream = new zlib.ZOutputStream(outStream, zlib.zlibConst.Z_BEST_COMPRESSION);
            byte[] data;
            try
            {
                byte[] buffer = new byte[512];
                int len;
                while ((len = mem.Read(buffer, 0, 512)) > 0)
                {
                    outZStream.Write(buffer, 0, len);
                }
                outZStream.finish();
                data = outStream.ToArray();
            }
            finally
            {
                outZStream.Close();
                outStream.Close();
            }
            return data;
        }

        internal virtual byte[] decompressStream(MemoryStream mem)
        {
            byte[] data;
            using (MemoryStream outStream = new MemoryStream())
            using (zlib.ZOutputStream outZStream = new zlib.ZOutputStream(outStream))
            try
            {
                byte[] buffer = new byte[512];
                int len;
                while ((len = mem.Read(buffer, 0, 512)) > 0)
                {
                    outZStream.Write(buffer, 0, len);
                }
                outZStream.Flush();
                data = outStream.ToArray();
            }
            catch(zlib.ZStreamException ex)
            {
                ErrorMsg = ex.Message;
                return null;
            }
            finally
            {
                outZStream.finish();
            }
            return data;
        }

        internal byte[] GetCompressedData()
        {
            if (Compression)
                return dataBuffer;
            else
            {
                using (MemoryStream st = new MemoryStream(dataBuffer)) {
                    byte[] compressed = compressStream(st);
                    PackedSize = compressed.Length;
                    return compressed;
                }
            }
        }

        private byte[] GetData()
        {
            if (Compression)
            {
                using (MemoryStream st = new MemoryStream(dataBuffer)) {
                    return decompressStream(st);
                }
            }
            return dataBuffer;
        }

        // Read whole file into a buffer
        internal byte[] GetFileData()
        {
            if (dataBuffer == null) {
                br.BaseStream.Seek(Offset, SeekOrigin.Begin);
                int size = (Compression) ? PackedSize : UnpackedSize;
                dataBuffer = new Byte[size];
                br.Read(dataBuffer, 0, size);
            }
            return GetData();
        }
    }
}
