using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YoutubeArchiver
{
    public partial class Form1 : Form
    {


        /*
         
         Really sorry if anyone is actually reading this source and trying to improve it
         i made this project for my own needs of archiving youtube channels, since every convertor
         i found only supports single videos or atleast i think so
         and i wanted to download entire channels or playlists at once, so i made this

         the code is uhh peak
         the ui is probably the worst part, could be heavily improved but i just wanted it to work so its ugly as fuck
         
         i learned that yt-dlp is a fucking nightmare, i mean it works but looking for the arguments and stuff is awful
         Also needed alot of dependencies like ffmpeg and deno, but i got it working so im happy

         using this program is pretty simple, just paste the link of a video, channel or playlist, click the scan button and select the videos
         you want to download, choose the format mp3 or mp4 for now, and click download
         
         feel free to heavily improve this and maybe even fix the problems ive had with yt-dlp
         this is enough for my needs atleast for now so this probably wont be updated by me
         or maybe if i add something i need

        */


        public Form1()
        {
            InitializeComponent();
            checkBox1.Appearance = Appearance.Button;
            checkBox2.Appearance = Appearance.Button;
            flowLayoutPanelVideos.AutoScroll = true;
            flowLayoutPanelVideos.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanelVideos.WrapContents = false;
        }


        //this is the button that gets the videos list from the channel and displays the videos in the flowlayoutpanel
        private async void button2_Click(object sender, EventArgs e)
        {
            string channelUrl = textBox1.Text.Trim();

            if (string.IsNullOrWhiteSpace(channelUrl))
            {
                
                MessageBox.Show("Paste a channel URL first.");
                return;
            }

            button2.Enabled = false;

            try
            {
                flowLayoutPanelVideos.Controls.Clear();

                string json = await GetChannelJson(channelUrl);

                if (string.IsNullOrEmpty(json))
                {
                    MessageBox.Show("Could not get channel information.");
                    return;
                }

                List<VideoInfo> videos = ParseVideos(json);

                if (videos.Count == 0)
                {
                    MessageBox.Show("No videos found.");
                    return;
                }

                foreach (VideoInfo video in videos)
                {
                    Panel panel = CreateVideoPanel(video);
                    flowLayoutPanelVideos.Controls.Add(panel);
                }

                MessageBox.Show("Found " + videos.Count + " videos.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:\n\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                button2.Enabled = true;
            }
        }




        //self explanatory, gets the needed json and parses the videos from it
        private async Task<string> GetChannelJson(string channelUrl)
        {
            var processInfo = new ProcessStartInfo();

            processInfo.FileName = "yt-dlp.exe";
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;
            processInfo.CreateNoWindow = true;

            processInfo.Arguments = "--flat-playlist --dump-single-json --no-warnings \"" + channelUrl.Replace("\"", "\\\"") + "\"";

            Process process = new Process();
            process.StartInfo = processInfo;

            try
            {
                process.Start();

                string output = await Task.Run(() => process.StandardOutput.ReadToEnd());

                string error = await Task.Run(() => process.StandardError.ReadToEnd());

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    MessageBox.Show("yt-dlp failed.\n\n" + "Exit code: " + process.ExitCode + "\n\n" + error, "yt-dlp error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }

                if (string.IsNullOrWhiteSpace(output))
                {
                    MessageBox.Show("yt-dlp returned no data.\n\n" + error, "yt-dlp error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }

                return output;
            }
            finally
            {
                process.Dispose();
            }
        }




        //all the bullshit below is just parsing the json from yt-dlp, also the most important part
        private List<VideoInfo> ParseVideos(string json)
        {
            List<VideoInfo> videos =
                new List<VideoInfo>();

            JObject root =
                JObject.Parse(json);

            //single video stuff
            if (root["entries"] == null)
            {
                VideoInfo video = new VideoInfo();

                video.Id = (string)root["id"];

                video.Title = (string)root["title"];

                video.Url = (string)root["webpage_url"];

                video.Thumbnail = (string)root["thumbnail"];

                if (string.IsNullOrEmpty(video.Url) && !string.IsNullOrEmpty(video.Id))
                {
                    video.Url = "https://www.youtube.com/watch?v=" + video.Id;
                }

                if (!string.IsNullOrEmpty(video.Id))
                {
                    videos.Add(video);
                }

                return videos;
            }

            //this is peak cinema
            //this is the part where it gets the list of videos from the channel or playlist
            //i didnt find any yttomp3 or mp4 convertors that can convert whole channels at once,
            //probably exist but im just blind, basically the whole point of this project

            JToken entries =
                root["entries"];

            foreach (JToken entry in entries)
            {
                if (entry == null)
                    continue;

                VideoInfo video = new VideoInfo();

                video.Id = (string)entry["id"];

                video.Title = (string)entry["title"];

                video.Url = (string)entry["webpage_url"];

                video.Thumbnail = (string)entry["thumbnail"];

                if (string.IsNullOrEmpty(video.Url) && !string.IsNullOrEmpty(video.Id))
                {
                    video.Url = "https://www.youtube.com/watch?v=" + video.Id;
                }

                if (!string.IsNullOrEmpty(video.Id))
                {
                    videos.Add(video);
                }
            }

            return videos;
        }



        //this panel code is a bit messy, not that bad ig but it works basically perfectly
        //it just creates a panel for each video with a thumbnail, title, checkbox, and status label,
        //this is just used just for downloading playlists or entire channels
        //also does it for single videos but this can be fixed pretty easily im just lazy
        //status label is also good cus sometimes the videos dont download on the first try even if yt-dlp has the argument for 3 retries
        //idk why, probably a problem on youtube or yt-dlp, could be mine but i doubt it, ive tried everything
        //but now theres less of a chance it happens so gambling sim ig
        //thats the comments for this part of the awful code, goodluck, have fun ig


        //also a reminder to make the panels look better
        private Panel CreateVideoPanel(VideoInfo video)
        {
            Panel panel = new Panel();

            panel.Width = flowLayoutPanelVideos.ClientSize.Width - 30;

            panel.Height = 90;
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.Margin = new Padding(5);

            PictureBox thumbnail = new PictureBox();

            thumbnail.Location = new Point(8, 10);
            thumbnail.Size = new Size(120, 68);
            thumbnail.SizeMode = PictureBoxSizeMode.Zoom;
            thumbnail.BorderStyle = BorderStyle.FixedSingle;

            if (!string.IsNullOrEmpty(video.Id))
            {
                string thumbnailUrl = "https://i.ytimg.com/vi/" + video.Id + "/hqdefault.jpg";

                Task.Run(async () =>
                {
                    try
                    {
                        using (WebClient client = new WebClient())
                        {
                            byte[] data =
                                await client.DownloadDataTaskAsync(
                                    thumbnailUrl
                                );

                            using (MemoryStream stream = new MemoryStream(data))
                            {
                                using (Image image = Image.FromStream(stream))
                                {
                                    Image copy = new Bitmap(image);

                                    if (!thumbnail.IsDisposed)
                                    {
                                        thumbnail.Invoke(
                                            new Action(() =>
                                            {
                                                thumbnail.Image = copy;
                                            })
                                        );
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        //ignore thumbnail loading errors, not that important so
                    }
                });
            }

            CheckBox checkBox = new CheckBox();

            checkBox.Location = new Point(140, 32);

            checkBox.AutoSize = true;
            checkBox.Tag = video;

            checkBox.CheckedChanged +=
                (sender, e) =>
                {
                    video.Selected =
                        checkBox.Checked;
                };

            
            Label title = new Label();

            title.Text = video.Title;
            title.Location = new Point(165, 10);
            title.Width = panel.Width - 180;
            title.Height = 25;
            title.AutoEllipsis = true;

            title.Font = new Font(title.Font, FontStyle.Bold);

            Label info = new Label();

            info.Text = "ID: " + video.Id;

            info.Location = new Point(165, 35);

            info.Width = panel.Width - 180;

            info.Height = 20;

            info.ForeColor = Color.Gray;

            Label status = new Label();

            status.Text = "Ready";
            status.Location = new Point(165, 58);
            status.Width = panel.Width - 180;
            status.Height = 20;

            status.ForeColor = Color.Gray;

            video.StatusLabel = status;

            
            panel.Controls.Add(thumbnail);
            panel.Controls.Add(checkBox);
            panel.Controls.Add(title);
            panel.Controls.Add(info);
            panel.Controls.Add(status);

            return panel;
        }




        //download button clicked, do the checks and if all good, download the selected videos
        private async void button1_Click(object sender, EventArgs e)
        {
            if (!checkBox1.Checked && !checkBox2.Checked)
            {
                MessageBox.Show("Select MP4, MP3, or both.");

                return;
            }

            List<VideoInfo> selectedVideos = new List<VideoInfo>();

            foreach (Control control in flowLayoutPanelVideos.Controls)
            {
                Panel panel = control as Panel;

                if (panel == null)
                    continue;

                foreach (Control child in panel.Controls)
                {
                    CheckBox checkBox = child as CheckBox;

                    if (checkBox != null && checkBox.Checked && checkBox.Tag is VideoInfo)
                    {
                        selectedVideos.Add((VideoInfo)checkBox.Tag);
                    }
                }
            }

            if (selectedVideos.Count == 0)
            {
                MessageBox.Show("Select at least one video.");
                return;
            }

            button1.Enabled = false;

            try
            {
                foreach (VideoInfo video in selectedVideos)
                {
                    try
                    {
                        await DownloadVideo(video.Url);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed: " + video.Title + "\n" + ex.Message);
                    }
                }

                MessageBox.Show(
                    "All selected videos have been downloaded"
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show("Download error:\n\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                button1.Enabled = true;
            }
        }



        //downloading the video, this fails sometimes, probably yt-dlp issue
        private async Task DownloadVideo(string url)
        {
            VideoInfo video = null;

            foreach (Control control in flowLayoutPanelVideos.Controls)
            {
                Panel panel = control as Panel;

                if (panel == null)
                    continue;

                foreach (Control child
                    in panel.Controls)
                {
                    CheckBox checkBox = child as CheckBox;

                    if (checkBox != null && checkBox.Tag is VideoInfo)
                    {
                        VideoInfo possibleVideo = (VideoInfo)checkBox.Tag;

                        if (possibleVideo.Url == url)
                        {
                            video = possibleVideo;
                            break;
                        }
                    }
                }

                if (video != null)
                    break;
            }

            if (video == null)
                return;

            if (video.StatusLabel != null)
            {
                video.StatusLabel.Text = "Downloading...";
                video.StatusLabel.ForeColor = Color.DarkOrange;
            }

            bool mp4Success = true;
            bool mp3Success = true;

            if (checkBox1.Checked)
            {
                if (video.StatusLabel != null)
                    video.StatusLabel.Text = "Downloading MP4...";

                mp4Success = await RunYtDlp(
                    "-f \"bestvideo*+bestaudio/best\" " +
                    "--merge-output-format mp4 " +
                    "--retries 3 " +
                    "--fragment-retries 3",
                    url
                );
            }

            if (checkBox2.Checked)
            {
                if (video.StatusLabel != null)
                    video.StatusLabel.Text = "Downloading MP3...";

                mp3Success = await RunYtDlp(
                    "-x --audio-format mp3 " +
                    "--audio-quality 0 " +
                    "--retries 3 " +
                    "--fragment-retries 3",
                    url
                );
            }

            //label bs
            if (video.StatusLabel != null)
            {
                if (mp4Success && mp3Success)
                {
                    video.StatusLabel.Text = "Downloaded successfully";
                    video.StatusLabel.ForeColor = Color.Green;
                }
                else if (mp4Success && !checkBox2.Checked)
                {
                    video.StatusLabel.Text = "MP4 downloaded";

                    video.StatusLabel.ForeColor = Color.Green;
                }
                else if (mp3Success && !checkBox1.Checked)
                {
                    video.StatusLabel.Text =
                        "MP3 downloaded";

                    video.StatusLabel.ForeColor =
                        Color.Green;
                }
                else
                {
                    string status = "";

                    if (checkBox1.Checked)
                    {
                        status += mp4Success
                            ? "MP4"
                            : "MP4 failed";
                    }

                    if (checkBox2.Checked)
                    {
                        if (status.Length > 0)
                            status += "   ";

                        status += mp3Success
                            ? "MP3"
                            : "MP3 failed";
                    }

                    video.StatusLabel.Text = status;

                    video.StatusLabel.ForeColor = Color.Red;
                }
            }
        }

        private void checkBoxSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            bool selectAll = checkBoxSelectAll.Checked;

            foreach (Control control in flowLayoutPanelVideos.Controls)
            {
                Panel panel = control as Panel;

                if (panel == null)
                    continue;

                foreach (Control child in panel.Controls)
                {
                    CheckBox videoCheckBox = child as CheckBox;

                    if (videoCheckBox != null && videoCheckBox.Tag is VideoInfo)
                    {
                        videoCheckBox.Checked = selectAll;
                    }
                }
            }
        }



        //run yt-dlp
        private async Task<bool> RunYtDlp(
            string arguments,
            string url)
        {
            var processInfo = new ProcessStartInfo();

            processInfo.FileName = "yt-dlp.exe";
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;
            processInfo.CreateNoWindow = true;

            string denoPath = Path.Combine(
                Application.StartupPath,
                "deno.exe"
            );

            if (!File.Exists(denoPath))
            {
                MessageBox.Show(
                    "deno.exe was not found\n\n" +
                    "Expected location:\n" +
                    denoPath,
                    "Missing Deno",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                return false;
            }

            processInfo.Arguments =
                "--ffmpeg-location \"" +
                Application.StartupPath +
                "\" " +
                "--js-runtimes deno:\"" +
                denoPath +
                "\" " +
                arguments +
                " \"" +
                url.Replace("\"", "\\\"") +
                "\"";

            var process = new Process();

            process.StartInfo = processInfo;

            StringBuilder error = new StringBuilder();

            process.OutputDataReceived += Process_OutputDataReceived;

            process.ErrorDataReceived +=
                (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        error.AppendLine(e.Data);
                        Console.WriteLine(e.Data);
                    }
                };

            try
            {
                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(
                    () => process.WaitForExit()
                );

                await Task.Delay(100);

                if (process.ExitCode != 0)
                {
                    Console.WriteLine(
                        "yt-dlp failed:\n" + error.ToString()
                    );

                    return false;
                }

                return true;
            }
            finally
            {
                process.Dispose();
            }
        }



        private void Process_OutputDataReceived(
            object sender,
            DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine(e.Data);
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

    }
}



