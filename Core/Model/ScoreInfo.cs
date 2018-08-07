﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Jammit.Model
{
  public class ScoreInfo
  {
    public Model.NotatedTrackInfo Track;
    public string Type;

    public override string ToString() => $"{Track.Title} - {Type}";
  }
}