﻿using System;
using System.Windows.Forms;
using Jam.NET.Audio;
using Jam.NET.Controls;
using Jam.NET.Model;

namespace Jam.NET
{
  
  public partial class SongWindow : Form
  {
    private ISong _song;
    private ISongPlayer _player;
    private Timer _timer;

    public SongWindow(SongMeta meta)
    {
      _song = SongLoader.Load(meta);
      InitializeComponent();
      Text = $"Score: {meta.Artist} - {meta.Name} [{meta.Instrument}]";
      albumArtwork.Image = _song.GetCover();
      score1.SetSong(_song);

      _player = _song.GetSongPlayer();
      for (int x = 0; x < _player.Channels; x++)
        AddFader(x);

      
      seekBar1.Samples = (long) (_player.Length.TotalSeconds*44100.0 + 0.5);
      seekBar1.OnSliderDrop += i => _player.Position = TimeSpan.FromSeconds(i);
      _timer = new Timer();
      _timer.Interval = 30;
      _timer.Tick += TimerTick;
      _timer.Start();
      waveform1.WaveData = _song.GetWaveform();
      waveform1.Sections = _song.Sections;
      waveform1.Beats = _song.Beats;
      waveform1.Looper.OnLoop += d => _player.Position = TimeSpan.FromSeconds(d);
    }

    private void AddFader(int channel)
    {
      var fader = new Fader();
      fader.Text = _player.GetChannelName(channel);
      fader.OnFaderChange += value => _player.SetChannelVolume(channel, value == 0 ? 0.00001f : value / 100.0f);
      if (fader.Text == "Click") fader.Mute = true;
      mixerFlowPanel.Controls.Add(fader);
    }

    private void TimerTick(object sender, EventArgs e)
    {
      seekBar1.SamplePosition = _player.PositionSamples;
      waveform1.SamplePosition = seekBar1.Moving ? seekBar1.Value * 44100 : _player.PositionSamples;
      waveform1.Invalidate();
      score1.SamplePosition = _player.PositionSamples;
    }

    private void SongWindow_FormClosing(object sender, FormClosingEventArgs e)
    {
      _timer.Stop();
      _timer.Dispose();
      _player.Stop();
    }

    private void playPauseButton_Click(object sender, EventArgs e)
    {
      if (_player.State == NAudio.Wave.PlaybackState.Playing)
      {
        _player.Pause();
        button1.Text = "Play";
      }
      else
      {
        _player.Play();
        button1.Text = "Pause";
      }
    }

    private void closeToolStripMenuItem_Click(object sender, EventArgs e)
    {
      Close();
    }

    private void leftSeekBtn_Click(object sender, EventArgs e)
    {
      var seekTime = 0.0;
      foreach (var s in _song.Sections)
      {
        if (s.Beat.Time > _player.Position.TotalSeconds - 1)
          break;
        seekTime = s.Beat.Time;
      }
      _player.Position = TimeSpan.FromSeconds(seekTime);
    }

    private void rightSeekBtn_Click(object sender, EventArgs e)
    {
      var seekTime = _player.Length.TotalSeconds;
      foreach (var s in _song.Sections)
      {
        if (s.Beat.Time > _player.Position.TotalSeconds + 0.1)
        {
          seekTime = s.Beat.Time;
          break;
        }
      }
      _player.Position = TimeSpan.FromSeconds(seekTime);
    }

    private void button2_Click(object sender, EventArgs e)
    {
      int i;
      for (i = 0; i <_song.Beats.Count; i++)
      {
        if (_song.Beats[i].Time >= _player.Position.TotalSeconds)
          break;
      }
      waveform1.Looper.LoopStart = _song.Beats[i].Time;
      waveform1.Looper.LoopEnd = _song.Beats[i+4].Time;
      waveform1.Looper.Enabled = !waveform1.Looper.Enabled;
    }
  }
}
