﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace GGL.IO;
public class StreamStack : Stack<StreamStackEntry>, IDisposable
{
    public event EventHandler<StreamStackEntry> PeakChanged;

    public StreamStack(Stream stream, bool closable) : this(new StreamStackEntry(stream, closable)) { }

    public StreamStack(StreamStackEntry entry)
    {
        Push(entry);
    }

    public StreamStackEntry Peak { private set; get; }

    /// <summary>
    /// Creates and push new MemoryStream
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public StreamStackEntry Create(object args = null)
    {
        var obj = new StreamStackEntry(new MemoryStream(), true, args);
        Push(obj);
        return obj;
    }

    public void Push(Stream stream, bool closable, object args = null)
    {
        Push(new(stream, closable, args));
    }

    public new void Push(StreamStackEntry entry)
    {
        base.Push(entry);
        Peak = entry;
        PeakChanged?.Invoke(this, entry);
    }

    public new StreamStackEntry Pop()
    {
        var entry = base.Pop();
        if (Count > 0)
        {
            Peak = Peek();
            PeakChanged?.Invoke(this, Peak);
        }
        return entry;
    }

    public void CopyToPeak(Stream dataStream, bool keepPosition = false)
    {
        var peakStream = Peak.Stream;
        long pos = peakStream.Position;
        dataStream.CopyTo(peakStream);
        if (keepPosition)
            peakStream.Position = pos;
    }

    public void InsertToPeak(Stream dataStream)
    {
        var dstStream = Peak.Stream;

        int pos = (int)dstStream.Position;
        var buffer = new MemoryStream();

        dstStream.CopyTo(buffer);

        dstStream.Position = pos;
        dataStream.CopyTo(dstStream);

        buffer.Position = 0;
        buffer.CopyTo(dstStream);
    }

    /// <summary>
    /// Pop and dispose top element
    /// </summary>
    public void DisposePeak()
    {
        Pop().Dispose();
    }

    public Stream GetSubStream(long length)
    {
        var peakStream = Peek().Stream;
        var subStream = new SubStream(peakStream, peakStream.Position, length);
        return subStream;
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            while (Count > 0)
            {
                DisposePeak();
            }

            disposedValue = true;
        }
    }

    ~StreamStack()
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
