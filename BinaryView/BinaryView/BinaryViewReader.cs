﻿using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace GGL.IO;

public class BinaryViewReader : IDisposable
{

    private byte[] readBuffer = new byte[16];
    private Stream baseStream;
    private Stream readStream;

    private bool ownStream = true;
    private BinaryFormatter formatter = new BinaryFormatter();

    public long Position
    {
        get => readStream.Position;
        set => readStream.Position = value;
    }
    public long Length
    {
        get => readStream.Length;
        set => readStream.SetLength(value);
    }

    /// <summary>Initialize BinaryView with a empty MemoryStream</summary>
    public BinaryViewReader()
    {
        baseStream = new MemoryStream();
        readStream = baseStream;
    }
    /// <summary>Initialize BinaryView with a FileStream</summary>
    /// <param name="path">File path</param>
    public BinaryViewReader(string path)
    {
        baseStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        readStream = baseStream;
    }
    /// <summary>Initialize BinaryView with a MemoryStream filled with bytes from array</summary>
    /// <param name="bytes">Base array</param>
    public BinaryViewReader(byte[] bytes)
    {
        baseStream = new MemoryStream(bytes);
        readStream = baseStream;
    }
    /// <summary>Initialize BinaryView with a Stream</summary>
    public BinaryViewReader(Stream stream)
    {
        baseStream = stream;
        readStream = baseStream;
        ownStream = false;
    }

    #region read
    /// <summary>Reads a primitive or unmanaged struct from the stream and increases the position by the size of the struct</summary>
    /// <typeparam name="T"></typeparam> Type of unmanaged struct
    public unsafe T Read<T>() where T : unmanaged
    {
        int size = sizeof(T);
        var obj = new T();
        var ptr = new IntPtr(&obj);
        for (int i = 0; i < size; i++) Marshal.WriteByte(ptr, i, ReadByte());
        return obj;
    }

    /// <summary>Reads an serialized object from the stream and increases the position by the size of the data</summary>
    /// <typeparam name="T"></typeparam> Type
    public T Deserialize<T>()
    {
        return (T)formatter.Deserialize(readStream);
    }


    /// <summary>Reads a array of unmanaged structs from the stream and increases the position by the size of the array elements, and 4 bytes for the length</summary>
    /// <typeparam name="T"></typeparam> Type of unmanaged struct
    public unsafe T[] ReadArray<T>() where T : unmanaged
    {
        int length = ReadInt32();
        T[] array = new T[length];
        for (int i = 0; i < array.Length; i++) array[i] = Read<T>();
        return array;
    }

    /// <summary>Reads a array of unmanaged structs from the stream and increases the position by the size of the array elements, and 4 bytes for the length</summary>
    /// <typeparam name="T"></typeparam> Type of unmanaged struct
    /// <param name="array">Pointer to existing array to write in</param>
    /// <param name="offset">Offset in array</param>
    public unsafe void ReadArray<T>(T[] array, int offset = 0) where T : unmanaged
    {
        int length = ReadInt32();
        for (int i = 0; i < length; i++) array[i + offset] = Read<T>();
    }

    /// <summary>Reads a char from the stream and increases the position by two bytes</summary>
    public char ReadChar()
    {
        readStream.Read(readBuffer, 0, sizeof(char));
        return BitConverter.ToChar(readBuffer, 0);
    }

    /// <summary>Reads a byte from the stream and increases the position by one byte</summary>
    public byte ReadByte() => (byte)readStream.ReadByte();

    /// <summary>Reads a sbyte from the stream and increases the position by one byte</summary>
    public sbyte ReadSByte() => (sbyte)readStream.ReadByte();

    /// <summary>Reads a ushort from the stream and increases the position by two bytes</summary>
    public ushort ReadUInt16()
    {
        readStream.Read(readBuffer, 0, sizeof(ushort));
        return BitConverter.ToUInt16(readBuffer, 0);
    }

    /// <summary>Reads a short from the stream and increases the position by two bytes</summary>
    public short ReadInt16()
    {
        readStream.Read(readBuffer, 0, sizeof(short));
        return BitConverter.ToInt16(readBuffer, 0);
    }

    /// <summary>Reads a uint from the stream and increases the position by four bytes</summary>
    public uint ReadUInt32()
    {
        readStream.Read(readBuffer, 0, sizeof(uint));
        return BitConverter.ToUInt32(readBuffer, 0);
    }

    /// <summary>Reads a int from the stream and increases the position by four bytes</summary>
    public int ReadInt32()
    {
        readStream.Read(readBuffer, 0, sizeof(int));
        return BitConverter.ToInt32(readBuffer, 0);
    }

    /// <summary>Reads a ulong from the stream and increases the position by eight bytes</summary>
    public ulong ReadUInt64()
    {
        readStream.Read(readBuffer, 0, sizeof(ulong));
        return BitConverter.ToUInt64(readBuffer, 0);
    }

    /// <summary>Reads a long from the stream and increases the position by eight bytes</summary>
    public long ReadInt64()
    {
        readStream.Read(readBuffer, 0, sizeof(long));
        return BitConverter.ToInt64(readBuffer, 0);
    }

    /// <summary>Reads a float from the stream and increases the position by four bytes</summary>
    public float ReadSingle()
    {
        readStream.Read(readBuffer, 0, sizeof(float));
        return BitConverter.ToSingle(readBuffer, 0);
    }

    /// <summary>Reads a double from the stream and increases the position by eight bytes</summary>
    public double ReadDouble()
    {
        readStream.Read(readBuffer, 0, sizeof(double));
        return BitConverter.ToDouble(readBuffer, 0);
    }

    /// <summary>Reads a decimal from the stream and increases the position by sixteen bytes</summary>
    public decimal ReadDecimal()
    {
        return Read<decimal>();
    }

    /// <summary>Reads a string from the stream</summary>
    public string ReadString()
    {
        byte meta = ReadByte();

        int lengthSizeBit = (meta >> 0) & 1;
        int charSizeBit = (meta >> 1) & 1;

        int length;
        if (lengthSizeBit == 1) length = ReadInt32();
        else length = ReadByte();

        char[] retData = new char[length];
        if (charSizeBit == 1)
            for (int i = 0; i < retData.Length; i++)
                retData[i] = (char)ReadChar();
        else
            for (int i = 0; i < retData.Length; i++)
                retData[i] = (char)ReadByte();

        return new string(retData);
    }
    /// <summary>Reads a array of string from the stream</summary>
    public string[] ReadStringArray()
    {
        int length = ReadInt32();
        string[] retData = new string[length];
        for (int i = 0; i < retData.Length; i++) retData[i] = ReadString();
        return retData;
    }
    #endregion

    /// <summary>Decompress all data with DeflateStream, must be executet before any read operation</summary>
    public void DecompressAll()
    {
        var decompressedStream = new MemoryStream();

        using (var decompressor = new DeflateStream(baseStream, CompressionMode.Decompress, true))
        {
            //readStream.Seek(0, SeekOrigin.Begin);
            decompressor.CopyTo(decompressedStream);
        }
        decompressedStream.Seek(0, SeekOrigin.Begin);

        baseStream.Dispose();
        baseStream = decompressedStream;
        readStream = baseStream;
    }
    /// <summary>Decompress data with DeflateStream, position will reset</summary>
    public void BeginDeflateSection()
    {
        long length = ReadInt64();
        var DecompressedStream = new MemoryStream();

        using (var compressedSection = new SubStream(readStream, readStream.Position, length))
        {
            using (var decompressStream = new DeflateStream(compressedSection, CompressionMode.Decompress, true))
            {
                decompressStream.CopyTo(DecompressedStream);
            }
        }
        readStream = DecompressedStream;
        readStream.Seek(0, SeekOrigin.Begin);
    }

    public void EndDeflateSection()
    {
        // Dispose DecompressedStream
        readStream.Dispose();
        readStream = baseStream;
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing) { }
            baseStream.Dispose();

            disposedValue = true;
        }
    }

    ~BinaryViewReader()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion


}

