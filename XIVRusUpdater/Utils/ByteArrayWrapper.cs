using System;
using System.Runtime.InteropServices;
using System.Text;

namespace XIVRusUpdater.Utils;

public unsafe class ByteArrayWrapper : IDisposable
{
    private bool disposed;

    public byte* Pointer { get; private set; }
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

        Pointer = (byte*)Marshal.AllocHGlobal(Math.Max(Length, 1));

        bytes.AsSpan().CopyTo(new Span<byte>(Pointer, Length));
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

        disposed = true;
        Marshal.FreeHGlobal((nint)Pointer);
        Pointer = null;
    }
}
