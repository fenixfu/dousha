using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Dousha.Windows.Core;

internal sealed class WinMmDefaultMicrophoneInput : IAudioInput
{
    private const uint WaveMapper = 0xFFFFFFFF;
    private const uint CallbackFunction = 0x00030000;
    private const uint WimData = 0x3C0;
    private const int BufferMilliseconds = 20;
    private const int BufferCount = 3;

    private readonly object _gate = new();
    private readonly WaveInProc _callback;
    private readonly List<WaveBuffer> _buffers = [];
    private readonly Dictionary<IntPtr, WaveBuffer> _buffersByHeader = [];
    private readonly Stopwatch _stopwatch = new();
    private IntPtr _waveIn;
    private bool _isRecording;

    public WinMmDefaultMicrophoneInput(AudioCaptureFormat format)
    {
        Format = format;
        _callback = OnWaveIn;
    }

    public event EventHandler<AudioFrameCapturedEventArgs>? FrameCaptured;

    public AudioCaptureFormat Format { get; }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_waveIn != IntPtr.Zero)
            {
                return Task.CompletedTask;
            }

            var waveFormat = CreateWaveFormat();
            ThrowIfFailed(waveInOpen(out _waveIn, WaveMapper, ref waveFormat, _callback, IntPtr.Zero, CallbackFunction), "waveInOpen");

            try
            {
                AllocateBuffers();
                foreach (var buffer in _buffers)
                {
                    ThrowIfFailed(waveInPrepareHeader(_waveIn, buffer.HeaderPointer, Marshal.SizeOf<WaveHeader>()), "waveInPrepareHeader");
                    buffer.Prepared = true;
                    ThrowIfFailed(waveInAddBuffer(_waveIn, buffer.HeaderPointer, Marshal.SizeOf<WaveHeader>()), "waveInAddBuffer");
                }

                _isRecording = true;
                _stopwatch.Restart();
                ThrowIfFailed(waveInStart(_waveIn), "waveInStart");
            }
            catch
            {
                CloseWaveIn();
                throw;
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IntPtr handle;

        lock (_gate)
        {
            if (_waveIn == IntPtr.Zero)
            {
                return Task.CompletedTask;
            }

            _isRecording = false;
            handle = _waveIn;
        }

        ThrowIfFailed(waveInStop(handle), "waveInStop");
        ThrowIfFailed(waveInReset(handle), "waveInReset");

        lock (_gate)
        {
            _stopwatch.Stop();
            CloseWaveIn();
        }

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeWaveInAsync();
    }

    private WaveFormatEx CreateWaveFormat()
    {
        var blockAlign = (ushort)(Format.ChannelCount * Format.BitsPerSample / 8);
        return new WaveFormatEx
        {
            FormatTag = 1,
            Channels = (ushort)Format.ChannelCount,
            SamplesPerSec = (uint)Format.SampleRateHz,
            AvgBytesPerSec = (uint)(Format.SampleRateHz * blockAlign),
            BlockAlign = blockAlign,
            BitsPerSample = (ushort)Format.BitsPerSample,
            Size = 0
        };
    }

    private void AllocateBuffers()
    {
        var bytesPerSecond = Format.SampleRateHz * Format.ChannelCount * Format.BitsPerSample / 8;
        var bufferLength = Math.Max(1, bytesPerSecond * BufferMilliseconds / 1000);

        for (var i = 0; i < BufferCount; i++)
        {
            var buffer = WaveBuffer.Allocate(bufferLength);
            _buffers.Add(buffer);
            _buffersByHeader.Add(buffer.HeaderPointer, buffer);
        }
    }

    private void OnWaveIn(IntPtr handle, uint message, IntPtr user, IntPtr headerPointer, IntPtr reserved)
    {
        if (message != WimData || headerPointer == IntPtr.Zero)
        {
            return;
        }

        byte[]? data = null;
        TimeSpan capturedAt = default;

        lock (_gate)
        {
            if (!_buffersByHeader.TryGetValue(headerPointer, out var buffer))
            {
                return;
            }

            var header = Marshal.PtrToStructure<WaveHeader>(headerPointer);
            if (header.BytesRecorded > 0)
            {
                data = new byte[header.BytesRecorded];
                Marshal.Copy(buffer.DataPointer, data, 0, data.Length);
                capturedAt = _stopwatch.Elapsed;
            }

            if (_isRecording && _waveIn != IntPtr.Zero)
            {
                waveInAddBuffer(handle, headerPointer, Marshal.SizeOf<WaveHeader>());
            }
        }

        if (data is not null)
        {
            FrameCaptured?.Invoke(this, new AudioFrameCapturedEventArgs(data, capturedAt));
        }
    }

    private Task DisposeWaveInAsync()
    {
        IntPtr handle;

        lock (_gate)
        {
            if (_waveIn == IntPtr.Zero)
            {
                return Task.CompletedTask;
            }

            _isRecording = false;
            handle = _waveIn;
        }

        waveInStop(handle);
        waveInReset(handle);

        lock (_gate)
        {
            _stopwatch.Stop();
            CloseWaveIn();
        }

        return Task.CompletedTask;
    }

    private void CloseWaveIn()
    {
        if (_waveIn != IntPtr.Zero)
        {
            foreach (var buffer in _buffers)
            {
                if (buffer.Prepared)
                {
                    waveInUnprepareHeader(_waveIn, buffer.HeaderPointer, Marshal.SizeOf<WaveHeader>());
                    buffer.Prepared = false;
                }
            }

            waveInClose(_waveIn);
            _waveIn = IntPtr.Zero;
        }

        foreach (var buffer in _buffers)
        {
            buffer.Dispose();
        }

        _buffers.Clear();
        _buffersByHeader.Clear();
    }

    private static void ThrowIfFailed(uint result, string operation)
    {
        if (result != 0)
        {
            throw new InvalidOperationException($"{operation} failed with winmm result {result}.");
        }
    }

    private delegate void WaveInProc(IntPtr handle, uint message, IntPtr user, IntPtr parameter1, IntPtr parameter2);

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveFormatEx
    {
        public ushort FormatTag;
        public ushort Channels;
        public uint SamplesPerSec;
        public uint AvgBytesPerSec;
        public ushort BlockAlign;
        public ushort BitsPerSample;
        public ushort Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveHeader
    {
        public IntPtr DataPointer;
        public uint BufferLength;
        public uint BytesRecorded;
        public IntPtr User;
        public uint Flags;
        public uint Loops;
        public IntPtr Next;
        public IntPtr Reserved;
    }

    private sealed class WaveBuffer : IDisposable
    {
        private WaveBuffer(IntPtr dataPointer, IntPtr headerPointer)
        {
            DataPointer = dataPointer;
            HeaderPointer = headerPointer;
        }

        public IntPtr DataPointer { get; }

        public IntPtr HeaderPointer { get; }

        public bool Prepared { get; set; }

        public static WaveBuffer Allocate(int byteCount)
        {
            var dataPointer = Marshal.AllocHGlobal(byteCount);
            var headerPointer = Marshal.AllocHGlobal(Marshal.SizeOf<WaveHeader>());
            var header = new WaveHeader
            {
                DataPointer = dataPointer,
                BufferLength = (uint)byteCount
            };
            Marshal.StructureToPtr(header, headerPointer, false);

            return new WaveBuffer(dataPointer, headerPointer);
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(HeaderPointer);
            Marshal.FreeHGlobal(DataPointer);
        }
    }

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint waveInOpen(
        out IntPtr handle,
        uint deviceId,
        ref WaveFormatEx format,
        WaveInProc callback,
        IntPtr instance,
        uint flags);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint waveInPrepareHeader(IntPtr handle, IntPtr header, int headerSize);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint waveInUnprepareHeader(IntPtr handle, IntPtr header, int headerSize);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint waveInAddBuffer(IntPtr handle, IntPtr header, int headerSize);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint waveInStart(IntPtr handle);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint waveInStop(IntPtr handle);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint waveInReset(IntPtr handle);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint waveInClose(IntPtr handle);
}
