﻿using System;

using Foundation;
using AVFoundation;

namespace Jammit.Audio
{
  public class MacOSAVAudioPlayer : IAVAudioPlayer
  {
    #region private members

    AVAudioPlayer player;

    #endregion // private members

    public MacOSAVAudioPlayer(Model.PlayableTrackInfo track, System.IO.Stream stream)
    {
      NSError error;
      player = AVAudioPlayer.FromData(NSData.FromStream(stream), out error);

      player.FinishedPlaying += delegate
      {
        player.Dispose();
        player = null;
      };
      player.NumberOfLoops = 0;

      player.PrepareToPlay();
    }

    #region IAvAudioPlayer members

    public void Play()
    {
      player.Play();
    }

    public void PlayAtTime(double time)
    {
      player.PlayAtTime(time);
    }

    public void Stop()
    {
      player.Stop();
    }

    public void Dispose()
    {
      player.Dispose();
    }

    public double Duration => player.Duration;

    public double CurrentTime
    {
      get
      {
        return player.CurrentTime;
      }

      set
      {
        player.CurrentTime = value;
      }
    }

    public float Volume
    {
      get
      {
        return player.Volume;
      }

      set
      {
        player.Volume = value;
      }
    }

    public int NumberOfLoops
    {
      get
      {
        return (int)player.NumberOfLoops;
      }

      set
      {
        player.NumberOfLoops = (nint)value;
      }
    }

    #endregion // IAvAudioPlayer members
  }
}