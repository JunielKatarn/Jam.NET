﻿using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Claunia.PropertyList;
using Jam.NET.Audio;
using Jammit; // PlistExtensions

namespace Jam.NET.Model
{
  /// <summary>
  /// Represents a song backed with a standard .zip content file.
  /// </summary>
  class ZipSong : ISong
  {
    public SongMeta Metadata { get; }

    public IReadOnlyList<Track> Tracks { get; }
    public IReadOnlyList<Jammit.Model.Beat> Beats { get; }
    public IReadOnlyList<Section> Sections { get; }

    private List<ScoreNodes> _notationData;
    private string _basePath;

    public ZipSong(SongMeta metadata)
    {
      Metadata = metadata;
      // Determine internal base path.
      using (var a = OpenZip())
      {
        _basePath = Metadata.GuidString;
        if (a.GetEntry(_basePath + "/") == null)
          _basePath += ".jcf";
      }

      Tracks = InitTracks();
      Beats = InitBeats();
      Sections = InitSections();
      _notationData = InitScoreNodes();


    }

    public sbyte[] GetWaveform()
    {
      using (var a = OpenZip())
      using (var s = a.GetEntry($"{_basePath}/music.waveform").Open())
      using (var ms = new MemoryStream())
      {
        s.CopyTo(ms);
        return new UnionArray { Bytes = ms.ToArray() }.Sbytes;
      }
    }

    public Image GetCover()
    {
      using (var x = OpenZip())
      using (var stream = x.GetEntry($"{_basePath}/cover.jpg").Open())
      {
        return Image.FromStream(stream);
      }
    }

    public List<Image> GetNotation(Track t)
    {
      var ret = new List<Image>();
      if (!t.HasNotation) return null;
      using (var arc = OpenZip())
        for (var i = 0; i < t.NotationPages; i++)
          using (var img = arc.GetEntry($"{_basePath}/{t.Id}_jcfn_{i:D2}").Open())
            ret.Add(Image.FromStream(img));

      return ret;
    }

    public List<Image> GetTablature(Track t)
    {
      var ret = new List<Image>();
      if (!t.HasTablature) return null;
      using (var arc = OpenZip())
        for (var i = 0; i < t.NotationPages; i++)
          using (var img = arc.GetEntry($"{_basePath}/{t.Id}_jcft_{i:D2}").Open())
            ret.Add(Image.FromStream(img));

      return ret;
    }

    public ISongPlayer GetSongPlayer()
    {
      if (System.Type.GetType("Mono.Runtime") != null)
        return new MockSongPlayer(this);

      return new JammitNAudioSongPlayer(this);
    }

    public Stream GetContentStream(string s)
    {
      var zip = OpenZip();
      var stream = new Jammit.OnDisposeStream(zip.GetEntry(_basePath + "/" + s).Open());
      stream.OnDispose += () => zip.Dispose();
      return stream;
    }

    public Stream GetSeekableContentStream(string s)
    {
      using (var x = OpenZip())
      using (var file = x.GetEntry(_basePath + "/" + s).Open())
      {
        var ms = new MemoryStream();
        file.CopyTo(ms);
        return ms;
      }
    }
    public ScoreNodes GetNotationData(string trackName, string notationType)
    {
      return _notationData.FirstOrDefault(score => trackName == score.Title && notationType == score.Type);
    }

    private List<Track> InitTracks()
    {
      using (var x = OpenZip())
      using (var s = x.GetEntry(_basePath + "/tracks.plist").Open())
      {
        var tracksArray = (NSArray)PropertyListParser.Parse(s);
        return Track.FromNSArray(tracksArray, path => x.GetEntry($"{_basePath}/" + path) != null);
      }
    }

    private List<Jammit.Model.Beat> InitBeats()
    {
      NSArray beatArray, ghostArray;
      using (var arc = OpenZip())
      {
        using (var stream = arc.GetEntry($"{_basePath}/beats.plist").Open())
          beatArray = (NSArray)PropertyListParser.Parse(stream);
        using (var stream = arc.GetEntry($"{_basePath}/ghost.plist").Open())
          ghostArray = (NSArray) PropertyListParser.Parse(stream);
      }
      return Jammit.Model.Beat.FromNSArrays(beatArray, ghostArray);
    }

    private List<Section> InitSections()
    {
      NSArray sectionArray;
      using (var arc = OpenZip())
      using (var stream = arc.GetEntry($"{_basePath}/sections.plist").Open())
        sectionArray = (NSArray)PropertyListParser.Parse(stream);
      return sectionArray.GetArray().OfType<NSDictionary>().Select(dict => new Section
      {
        BeatIdx = dict.Int("beat") ?? 0,
        Beat = Beats[dict.Int("beat") ?? 0],
        Number = dict.Int("number") ?? 0,
        Type = dict.Int("type") ?? 0
      }).ToList();
    }

    private List<ScoreNodes> InitScoreNodes()
    {
      using (var nodes = GetContentStream("nowline.nodes"))
      {
        return ScoreNodes.FromStream(nodes);
      }
    }

    public ZipArchive OpenZip() => ZipFile.OpenRead(Metadata.SongPath);
  }
}
