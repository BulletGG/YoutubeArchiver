using System;
using System.Windows.Forms;

namespace YoutubeArchiver
{

    //this is just a class to store the info, self explanatory
    public class VideoInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Thumbnail { get; set; }
        public string UploadDate { get; set; }
        public int Duration { get; set; }

        public bool Selected { get; set; }

        public Label StatusLabel { get; set; }
    }
}