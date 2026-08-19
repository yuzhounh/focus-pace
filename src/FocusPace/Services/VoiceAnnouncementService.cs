using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using FocusPace.Core;
using FocusPace.Models;
using FocusPace.ViewModels;

namespace FocusPace.Services;

public sealed class VoiceAnnouncementService : IDisposable
{
    private const int SampleRate = 44100;
    private readonly AppViewModel _viewModel;
    private readonly BlockingCollection<string> _announcements = new(new ConcurrentQueue<string>());
    private readonly Thread _worker;
    private SessionPhase _lastPhase;
    private bool _disposed;

    public VoiceAnnouncementService(AppViewModel viewModel)
    {
        _viewModel = viewModel;
        _lastPhase = viewModel.Phase;
        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        _viewModel.FocusEndingSoon += ViewModelOnFocusEndingSoon;

        _worker = new Thread(ProcessAnnouncements)
        {
            IsBackground = true,
            Name = "Focus Pace voice announcements"
        };
        _worker.SetApartmentState(ApartmentState.STA);
        _worker.Start();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        _viewModel.FocusEndingSoon -= ViewModelOnFocusEndingSoon;
        _announcements.CompleteAdding();
        if (_worker.Join(TimeSpan.FromSeconds(1)))
        {
            _announcements.Dispose();
        }
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppViewModel.VoiceAnnouncementsEnabled))
        {
            if (!_viewModel.VoiceAnnouncementsEnabled)
            {
                while (_announcements.TryTake(out _))
                {
                }
            }

            return;
        }

        if (e.PropertyName != nameof(AppViewModel.Phase) || _viewModel.Phase == _lastPhase)
        {
            return;
        }

        _lastPhase = _viewModel.Phase;
        var text = _lastPhase switch
        {
            SessionPhase.Focus => $"Focus started. {FormatRemaining(_viewModel.Remaining)} remaining.",
            SessionPhase.Rest => $"Rest started. {FormatRemaining(_viewModel.Remaining)} remaining.",
            _ => "Ready."
        };
        Enqueue(text);
    }

    private void ViewModelOnFocusEndingSoon(object? sender, GoalApproachingEventArgs e) =>
        Enqueue($"Focus ending soon. {FormatRemaining(e.Remaining)} remaining.");

    private void Enqueue(string text)
    {
        if (_disposed || !_viewModel.VoiceAnnouncementsEnabled || _announcements.IsAddingCompleted)
        {
            return;
        }

        try
        {
            _announcements.Add(text);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void ProcessAnnouncements()
    {
        foreach (var text in _announcements.GetConsumingEnumerable())
        {
            try
            {
                PlayChime();
                Thread.Sleep(100);
                Speak(text);
            }
            catch
            {
                // Audio must never interrupt the timer or state transitions.
            }
        }
    }

    private static string FormatRemaining(TimeSpan remaining)
    {
        var minutes = Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes));
        return minutes == 1 ? "1 minute" : $"{minutes} minutes";
    }

    private static void PlayChime()
    {
        using var stream = CreateChimeWave();
        using var player = new SoundPlayer(stream);
        player.Load();
        player.PlaySync();
    }

    private static MemoryStream CreateChimeWave()
    {
        const double durationSeconds = 0.48;
        const short channels = 1;
        const short bitsPerSample = 16;
        var sampleCount = (int)(SampleRate * durationSeconds);
        var dataLength = sampleCount * channels * (bitsPerSample / 8);
        var stream = new MemoryStream(44 + dataLength);
        using (var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataLength);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(SampleRate);
            writer.Write(SampleRate * channels * (bitsPerSample / 8));
            writer.Write((short)(channels * (bitsPerSample / 8)));
            writer.Write(bitsPerSample);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataLength);

            for (var index = 0; index < sampleCount; index++)
            {
                var time = (double)index / SampleRate;
                var attack = Math.Min(1, time / 0.012);
                var decay = Math.Exp(-6.2 * time);
                var tone = Math.Sin(2 * Math.PI * 1046.5 * time) +
                           (0.32 * Math.Sin(2 * Math.PI * 2093 * time));
                var sample = tone / 1.32 * attack * decay * 0.42;
                writer.Write((short)Math.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue));
            }
        }

        stream.Position = 0;
        return stream;
    }

    private static void Speak(string text)
    {
        var voiceType = Type.GetTypeFromProgID("SAPI.SpVoice", throwOnError: false);
        if (voiceType is null)
        {
            return;
        }

        var voice = Activator.CreateInstance(voiceType);
        if (voice is null)
        {
            return;
        }

        try
        {
            voiceType.InvokeMember(
                "Speak",
                BindingFlags.InvokeMethod,
                binder: null,
                target: voice,
                args: [text, 0]);
        }
        finally
        {
            if (Marshal.IsComObject(voice))
            {
                Marshal.FinalReleaseComObject(voice);
            }
        }
    }
}
