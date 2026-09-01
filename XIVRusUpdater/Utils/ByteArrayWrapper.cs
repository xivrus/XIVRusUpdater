using System;
using System.Runtime.InteropServices;
using System.Text;

namespace XIVRusUpdater.Utils;

public unsafe class ByteArrayWrapper : IDisposable
{
    private bool disposed;

    public unsafe byte* Pointer { get; private set; }
    public int Length { get; }
    public string? Error { get; }
    public bool IsError => Error is not null;

    public string Value =>
        Encoding.UTF8.GetString(AsReadOnlySpan());

    public ByteArrayWrapper(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        Length = bytes.Length;

        if (Length == 0)
            return;

        Pointer = (byte*)Marshal.AllocHGlobal(Length);

        fixed (byte* src = bytes)
        {
            Buffer.MemoryCopy(src, Pointer, Length, Length);
        }
    }

    public ByteArrayWrapper(string error)
    {
        ArgumentException.ThrowIfNullOrEmpty(error);
        Error = error;
    }

    public ReadOnlySpan<byte> AsReadOnlySpan()
    {
        ThrowIfDisposed();
        return new ReadOnlySpan<byte>(Pointer, Length);
    }

    public Span<byte> AsSpan()
    {
        ThrowIfDisposed();
        return new Span<byte>(Pointer, Length);
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(ByteArrayWrapper));
    }

    public void Dispose()
    {
        if (disposed)
            return;

        Marshal.FreeHGlobal((nint)Pointer);
        Pointer = null;
        disposed = true;
    }
}
