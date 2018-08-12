﻿using System;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;
using System.Windows.Forms;
using Jam.NET.Net;

namespace Jam.NET.Forms
{
  public partial class DownloadForm : Form
  {
    private ClassicApiConnector api;
    public DownloadForm()
    {
      InitializeComponent();
      api = new ClassicApiConnector(Properties.Settings.Default.Server);
      textBox1.Text = getJammitDeviceId();
    }

    // TODO: Make this cross-platform
    private string getJammitDeviceId()
    {
      ManagementObject managementObject = new ManagementObject("win32_logicaldisk.deviceid=\"C:\"");
      managementObject.Get();
      return managementObject["VolumeSerialNumber"].ToString(); ;
    }

    private async Task downloadSongs(List<Guid> songs)
    {
      progressBar1.Value = 0;
      var current = 0;
      foreach(var song in songs)
      {
        var name = System.IO.Path.Combine(Properties.Settings.Default.TrackPath,
            song.ToString("D") + ".zip");
        status.Text = $"Downloading {current+1} of {songs.Count}...";
        await api.DownloadContent(song, name, (ev,details) => {
          progressBar1.Value = current * 100 / songs.Count + (details.ProgressPercentage / songs.Count);
        });
        current++;
        progressBar1.Value = (current * 100) / songs.Count;
      }
      ControlBox = true;
      checkDownloadBtn.Enabled = true;
    }

    private async void checkDownloadBtn_Click(object sender, EventArgs e)
    {
      var username = usernameField.Text;
      var password = passwordField.Text;
      var evje = textBox1.Text;

      try
      {
        status.Text = "Checking device...";
        progressBar1.Value = 10;
        var login = await api.IdCheck(username, evje);
        if (login != 0)
        {
          status.Text = "Username / device ID incorrect (or deauthorized)!";
          return;
        }
        status.Text = "Logging in...";
        progressBar1.Value = 30;
        var id = await api.GetJammitID(username, password);
        if(id <= 0)
        {
          status.Text = "Could not log in!";
          return;
        }
        status.Text = "Checking downloads...";
        progressBar1.Value = 60;
        var guids = await api.GetGuids(id, evje, Application.ProductVersion, "Jam.NET");
        var newGuids = new List<Guid>();
        foreach(var guid in guids)
        {
          if (!Model.Library.SongExists(guid))
          {
            newGuids.Add(guid);
          }
        }
        status.Text = $"Found {guids.Count} total song(s), {newGuids.Count} not yet downloaded.";
        progressBar1.Value = 100;
        if(newGuids.Count > 0)
        {
          if (MessageBox.Show(this, $"{newGuids.Count} new songs are ready to download. Proceed?", "Confirm Song Download", MessageBoxButtons.YesNo) == DialogResult.Yes)
          {
            ControlBox = true;
            checkDownloadBtn.Enabled = false;
            await downloadSongs(newGuids);
            Close();
          }
        }
      }
      catch(Exception ex)
      {
        status.Text = ex.Message;
        return;
      }
    }

    private void DownloadForm_FormClosing(object sender, FormClosingEventArgs e)
    {
      api.Cancel();
    }
  }
}
