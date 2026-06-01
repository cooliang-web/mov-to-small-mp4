using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using WpfControls = System.Windows.Controls;

namespace MovToSmallMp4
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly WpfControls.MediaElement media;
        private readonly TrackBar timeline;
        private readonly Label pathLabel;
        private readonly Label currentLabel;
        private readonly Label durationLabel;
        private readonly Label startLabel;
        private readonly Label endLabel;
        private readonly Label outputLabel;
        private readonly Label statusLabel;
        private readonly Button playButton;
        private readonly Button exportButton;
        private readonly ComboBox presetBox;
        private readonly ProgressBar progressBar;
        private readonly System.Windows.Forms.Timer playbackTimer;

        private string inputPath = "";
        private string outputPath = "";
        private TimeSpan videoDuration = TimeSpan.Zero;
        private TimeSpan startTime = TimeSpan.Zero;
        private TimeSpan endTime = TimeSpan.Zero;
        private bool isPlaying;
        private bool isSeeking;

        public MainForm()
        {
            Text = "MOV 转小体积 MP4";
            Width = 1040;
            Height = 680;
            MinimumSize = new Size(900, 560);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 4;
            root.ColumnCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            Controls.Add(root);

            var topBar = new FlowLayoutPanel();
            topBar.Dock = DockStyle.Fill;
            topBar.FlowDirection = FlowDirection.LeftToRight;
            topBar.WrapContents = false;
            topBar.Padding = new Padding(10, 8, 10, 6);
            root.Controls.Add(topBar, 0, 0);

            var openButton = new Button();
            openButton.Text = "选择视频";
            openButton.Width = 100;
            openButton.Height = 30;
            openButton.Click += delegate { OpenVideo(); };
            topBar.Controls.Add(openButton);

            playButton = new Button();
            playButton.Text = "播放";
            playButton.Width = 80;
            playButton.Height = 30;
            playButton.Enabled = false;
            playButton.Click += delegate { TogglePlay(); };
            topBar.Controls.Add(playButton);

            pathLabel = new Label();
            pathLabel.Text = "未选择视频";
            pathLabel.AutoEllipsis = true;
            pathLabel.TextAlign = ContentAlignment.MiddleLeft;
            pathLabel.Width = 760;
            pathLabel.Height = 30;
            pathLabel.Margin = new Padding(12, 3, 0, 0);
            topBar.Controls.Add(pathLabel);

            var split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.SplitterDistance = 690;
            split.Panel1MinSize = 480;
            split.Panel2MinSize = 250;
            root.Controls.Add(split, 0, 1);

            var videoPanel = new Panel();
            videoPanel.Dock = DockStyle.Fill;
            videoPanel.BackColor = Color.Black;
            videoPanel.Padding = new Padding(0);
            split.Panel1.Controls.Add(videoPanel);

            media = new WpfControls.MediaElement();
            media.LoadedBehavior = System.Windows.Controls.MediaState.Manual;
            media.UnloadedBehavior = System.Windows.Controls.MediaState.Manual;
            media.Stretch = System.Windows.Media.Stretch.Uniform;
            media.MediaOpened += delegate { OnMediaOpened(); };
            media.MediaEnded += delegate { OnMediaEnded(); };
            media.MediaFailed += delegate(object sender, System.Windows.ExceptionRoutedEventArgs e)
            {
                SetStatus("预览失败：这个 MOV 可能需要系统视频解码器，但仍可尝试导出 MP4。");
            };

            var host = new ElementHost();
            host.Dock = DockStyle.Fill;
            host.Child = media;
            videoPanel.Controls.Add(host);

            var side = new TableLayoutPanel();
            side.Dock = DockStyle.Fill;
            side.ColumnCount = 2;
            side.RowCount = 12;
            side.Padding = new Padding(14, 10, 14, 10);
            side.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84));
            side.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 12; i++)
            {
                side.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 10 ? 62 : 36));
            }
            split.Panel2.Controls.Add(side);

            AddText(side, "开始", 0, 0);
            startLabel = AddValue(side, "00:00:00.000", 1, 0);
            var setStartButton = AddSideButton(side, "设为开始", 0, 1, 2);
            setStartButton.Click += delegate { SetStartFromCurrent(); };

            AddText(side, "结束", 0, 2);
            endLabel = AddValue(side, "00:00:00.000", 1, 2);
            var setEndButton = AddSideButton(side, "设为结束", 0, 3, 2);
            setEndButton.Click += delegate { SetEndFromCurrent(); };

            AddText(side, "压缩", 0, 4);
            presetBox = new ComboBox();
            presetBox.Dock = DockStyle.Fill;
            presetBox.DropDownStyle = ComboBoxStyle.DropDownList;
            presetBox.Items.Add("极小体积 540p");
            presetBox.Items.Add("小体积 720p");
            presetBox.Items.Add("清晰优先 原尺寸");
            presetBox.SelectedIndex = 0;
            side.Controls.Add(presetBox, 1, 4);

            AddText(side, "输出", 0, 5);
            var chooseOutputButton = AddSideButton(side, "选择位置", 1, 5, 1);
            chooseOutputButton.Click += delegate { ChooseOutputPath(); };

            outputLabel = new Label();
            outputLabel.Text = "未设置";
            outputLabel.AutoEllipsis = true;
            outputLabel.Dock = DockStyle.Fill;
            outputLabel.TextAlign = ContentAlignment.TopLeft;
            side.Controls.Add(outputLabel, 0, 6);
            side.SetColumnSpan(outputLabel, 2);

            exportButton = AddSideButton(side, "导出 MP4", 0, 8, 2);
            exportButton.Height = 34;
            exportButton.Enabled = false;
            exportButton.Click += delegate { ExportMp4(); };

            var hint = new Label();
            hint.Text = "提示：先播放预览，到需要的位置后点击“设为开始”或“设为结束”。";
            hint.Dock = DockStyle.Fill;
            hint.ForeColor = Color.DimGray;
            hint.AutoEllipsis = true;
            side.Controls.Add(hint, 0, 10);
            side.SetColumnSpan(hint, 2);

            var timelinePanel = new TableLayoutPanel();
            timelinePanel.Dock = DockStyle.Fill;
            timelinePanel.ColumnCount = 3;
            timelinePanel.RowCount = 2;
            timelinePanel.Padding = new Padding(10, 8, 10, 6);
            timelinePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            timelinePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            timelinePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            timelinePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            timelinePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            root.Controls.Add(timelinePanel, 0, 2);

            currentLabel = new Label();
            currentLabel.Dock = DockStyle.Fill;
            currentLabel.Text = "00:00:00.000";
            currentLabel.TextAlign = ContentAlignment.MiddleLeft;
            timelinePanel.Controls.Add(currentLabel, 0, 0);

            timeline = new TrackBar();
            timeline.Dock = DockStyle.Fill;
            timeline.Minimum = 0;
            timeline.Maximum = 10000;
            timeline.TickStyle = TickStyle.None;
            timeline.Enabled = false;
            timeline.MouseDown += delegate { isSeeking = true; };
            timeline.MouseUp += delegate { SeekFromTimeline(); isSeeking = false; };
            timeline.Scroll += delegate { if (isSeeking) SeekFromTimeline(); };
            timelinePanel.Controls.Add(timeline, 1, 0);

            durationLabel = new Label();
            durationLabel.Dock = DockStyle.Fill;
            durationLabel.Text = "00:00:00.000";
            durationLabel.TextAlign = ContentAlignment.MiddleRight;
            timelinePanel.Controls.Add(durationLabel, 2, 0);

            var rangeLabel = new Label();
            rangeLabel.Dock = DockStyle.Fill;
            rangeLabel.Text = "选择片段后导出；不设置则导出完整视频。";
            rangeLabel.ForeColor = Color.DimGray;
            rangeLabel.TextAlign = ContentAlignment.MiddleLeft;
            timelinePanel.Controls.Add(rangeLabel, 1, 1);

            var statusPanel = new TableLayoutPanel();
            statusPanel.Dock = DockStyle.Fill;
            statusPanel.ColumnCount = 2;
            statusPanel.RowCount = 1;
            statusPanel.Padding = new Padding(10, 4, 10, 6);
            statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            root.Controls.Add(statusPanel, 0, 3);

            statusLabel = new Label();
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.Text = "准备就绪";
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusPanel.Controls.Add(statusLabel, 0, 0);

            progressBar = new ProgressBar();
            progressBar.Dock = DockStyle.Fill;
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            statusPanel.Controls.Add(progressBar, 1, 0);

            playbackTimer = new System.Windows.Forms.Timer();
            playbackTimer.Interval = 250;
            playbackTimer.Tick += delegate { UpdatePlaybackState(); };
            playbackTimer.Start();
        }

        private static Label AddValue(TableLayoutPanel panel, string text, int col, int row)
        {
            var label = new Label();
            label.Dock = DockStyle.Fill;
            label.Text = text;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.AutoEllipsis = true;
            panel.Controls.Add(label, col, row);
            return label;
        }

        private static void AddText(TableLayoutPanel panel, string text, int col, int row)
        {
            var label = new Label();
            label.Dock = DockStyle.Fill;
            label.Text = text;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = Color.DimGray;
            panel.Controls.Add(label, col, row);
        }

        private static Button AddSideButton(TableLayoutPanel panel, string text, int col, int row, int colSpan)
        {
            var button = new Button();
            button.Text = text;
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(0, 3, 0, 3);
            panel.Controls.Add(button, col, row);
            panel.SetColumnSpan(button, colSpan);
            return button;
        }

        private void OpenVideo()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "视频文件|*.mov;*.mp4;*.m4v;*.qt|所有文件|*.*";
                dialog.Title = "选择 MOV 视频";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                inputPath = dialog.FileName;
                outputPath = Path.Combine(
                    Path.GetDirectoryName(inputPath) ?? "",
                    Path.GetFileNameWithoutExtension(inputPath) + "_small.mp4");

                pathLabel.Text = inputPath;
                outputLabel.Text = outputPath;
                videoDuration = TimeSpan.Zero;
                startTime = TimeSpan.Zero;
                endTime = TimeSpan.Zero;
                progressBar.Value = 0;
                timeline.Value = 0;
                timeline.Enabled = false;
                playButton.Enabled = true;
                exportButton.Enabled = true;
                isPlaying = false;
                playButton.Text = "播放";
                SetTimeLabels();

                try
                {
                    media.Source = new Uri(inputPath);
                    media.Position = TimeSpan.Zero;
                    media.Play();
                    media.Pause();
                    SetStatus("已载入视频，可以预览和截切。");
                }
                catch (Exception ex)
                {
                    SetStatus("视频载入失败：" + ex.Message);
                }
            }
        }

        private void OnMediaOpened()
        {
            if (media.NaturalDuration.HasTimeSpan)
            {
                videoDuration = media.NaturalDuration.TimeSpan;
                endTime = videoDuration;
                timeline.Enabled = videoDuration.TotalMilliseconds > 0;
                SetTimeLabels();
                UpdateTimeline(media.Position);
                SetStatus("已载入视频，可以预览和截切。");
            }
        }

        private void OnMediaEnded()
        {
            isPlaying = false;
            playButton.Text = "播放";
            if (startTime < endTime)
            {
                media.Position = startTime;
            }
        }

        private void TogglePlay()
        {
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                return;
            }

            if (isPlaying)
            {
                media.Pause();
                isPlaying = false;
                playButton.Text = "播放";
            }
            else
            {
                if (endTime > startTime && media.Position >= endTime)
                {
                    media.Position = startTime;
                }
                media.Play();
                isPlaying = true;
                playButton.Text = "暂停";
            }
        }

        private void UpdatePlaybackState()
        {
            if (string.IsNullOrWhiteSpace(inputPath) || isSeeking)
            {
                return;
            }

            var pos = media.Position;
            if (endTime > startTime && pos >= endTime && isPlaying)
            {
                media.Pause();
                isPlaying = false;
                playButton.Text = "播放";
                media.Position = startTime;
                pos = startTime;
            }

            UpdateTimeline(pos);
        }

        private void UpdateTimeline(TimeSpan position)
        {
            currentLabel.Text = FormatTime(position);
            if (videoDuration.TotalMilliseconds <= 0)
            {
                return;
            }

            var ratio = Math.Max(0, Math.Min(1, position.TotalMilliseconds / videoDuration.TotalMilliseconds));
            var value = (int)Math.Round(ratio * timeline.Maximum);
            if (value < timeline.Minimum) value = timeline.Minimum;
            if (value > timeline.Maximum) value = timeline.Maximum;
            timeline.Value = value;
        }

        private void SeekFromTimeline()
        {
            if (videoDuration.TotalMilliseconds <= 0)
            {
                return;
            }

            var ratio = (double)timeline.Value / timeline.Maximum;
            var next = TimeSpan.FromMilliseconds(videoDuration.TotalMilliseconds * ratio);
            media.Position = next;
            currentLabel.Text = FormatTime(next);
        }

        private void SetStartFromCurrent()
        {
            if (videoDuration.TotalMilliseconds <= 0)
            {
                return;
            }

            startTime = media.Position;
            if (endTime <= startTime)
            {
                endTime = videoDuration;
            }
            SetTimeLabels();
        }

        private void SetEndFromCurrent()
        {
            if (videoDuration.TotalMilliseconds <= 0)
            {
                return;
            }

            endTime = media.Position;
            if (endTime <= startTime)
            {
                startTime = TimeSpan.Zero;
            }
            SetTimeLabels();
        }

        private void SetTimeLabels()
        {
            startLabel.Text = FormatTime(startTime);
            endLabel.Text = FormatTime(endTime);
            durationLabel.Text = FormatTime(videoDuration);
        }

        private void ChooseOutputPath()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "MP4 文件|*.mp4";
                dialog.Title = "选择导出位置";
                dialog.FileName = string.IsNullOrWhiteSpace(outputPath) ? "output_small.mp4" : Path.GetFileName(outputPath);
                dialog.InitialDirectory = string.IsNullOrWhiteSpace(inputPath)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
                    : Path.GetDirectoryName(inputPath);

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    outputPath = dialog.FileName;
                    outputLabel.Text = outputPath;
                }
            }
        }

        private void ExportMp4()
        {
            if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
            {
                MessageBox.Show(this, "请先选择一个 MOV 视频。", "无法导出", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                ChooseOutputPath();
                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    return;
                }
            }

            var ffmpeg = FindFfmpeg();
            if (string.IsNullOrWhiteSpace(ffmpeg))
            {
                MessageBox.Show(this, "没有找到 ffmpeg.exe。请使用打包版，或把 ffmpeg.exe 放到软件同一目录。", "缺少 FFmpeg", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var cutEnd = endTime > startTime ? endTime : videoDuration;
            var cutDuration = cutEnd - startTime;
            if (cutDuration.TotalSeconds <= 0.2)
            {
                MessageBox.Show(this, "截取片段太短，请重新设置开始和结束。", "无法导出", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            media.Pause();
            isPlaying = false;
            playButton.Text = "播放";
            exportButton.Enabled = false;
            progressBar.Value = 0;
            SetStatus("正在导出 MP4...");

            var args = BuildFfmpegArgs(inputPath, outputPath, startTime, cutDuration);
            var worker = new Thread(delegate()
            {
                RunFfmpeg(ffmpeg, args, cutDuration);
            });
            worker.IsBackground = true;
            worker.Start();
        }

        private void RunFfmpeg(string ffmpeg, string args, TimeSpan cutDuration)
        {
            var lastLines = new StringBuilder();
            try
            {
                var startInfo = new ProcessStartInfo();
                startInfo.FileName = ffmpeg;
                startInfo.Arguments = args;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        throw new InvalidOperationException("FFmpeg 启动失败。");
                    }

                    var timeRegex = new Regex(@"time=(\d+):(\d+):(\d+)\.(\d+)", RegexOptions.Compiled);
                    string line;
                    while ((line = process.StandardError.ReadLine()) != null)
                    {
                        if (lastLines.Length > 12000)
                        {
                            lastLines.Remove(0, 6000);
                        }
                        lastLines.AppendLine(line);

                        var match = timeRegex.Match(line);
                        if (match.Success)
                        {
                            var seconds =
                                int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) * 3600 +
                                int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) * 60 +
                                int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) +
                                double.Parse("0." + match.Groups[4].Value, CultureInfo.InvariantCulture);
                            var percent = (int)Math.Max(0, Math.Min(100, seconds / cutDuration.TotalSeconds * 100));
                            BeginInvoke((MethodInvoker)delegate
                            {
                                progressBar.Value = percent;
                                SetStatus("正在导出 MP4... " + percent + "%");
                            });
                        }
                    }

                    process.WaitForExit();
                    BeginInvoke((MethodInvoker)delegate
                    {
                        exportButton.Enabled = true;
                        if (process.ExitCode == 0 && File.Exists(outputPath))
                        {
                            progressBar.Value = 100;
                            SetStatus("导出完成：" + outputPath);
                            MessageBox.Show(this, "MP4 已导出完成。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            SetStatus("导出失败，请检查视频文件。");
                            MessageBox.Show(this, "FFmpeg 导出失败。\n\n" + TrimForMessage(lastLines.ToString()), "导出失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    exportButton.Enabled = true;
                    SetStatus("导出失败：" + ex.Message);
                    MessageBox.Show(this, ex.Message, "导出失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
        }

        private string BuildFfmpegArgs(string input, string output, TimeSpan start, TimeSpan duration)
        {
            var preset = presetBox.SelectedItem == null ? "" : presetBox.SelectedItem.ToString();
            string videoArgs;
            string audioArgs;

            if (preset.IndexOf("540p", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                videoArgs = "-vf \"scale=w='if(gt(ih,540),-2,iw)':h='if(gt(ih,540),540,ih)',fps=24\" -c:v libx264 -preset veryslow -crf 33 -pix_fmt yuv420p";
                audioArgs = "-c:a aac -b:a 80k";
            }
            else if (preset.IndexOf("720p", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                videoArgs = "-vf \"scale=w='if(gt(ih,720),-2,iw)':h='if(gt(ih,720),720,ih)',fps=30\" -c:v libx264 -preset slow -crf 30 -pix_fmt yuv420p";
                audioArgs = "-c:a aac -b:a 96k";
            }
            else
            {
                videoArgs = "-c:v libx264 -preset medium -crf 24 -pix_fmt yuv420p";
                audioArgs = "-c:a aac -b:a 128k";
            }

            return "-y -hide_banner -i " + Quote(input) +
                   " -ss " + FormatForFfmpeg(start) +
                   " -t " + FormatForFfmpeg(duration) +
                   " " + videoArgs +
                   " " + audioArgs +
                   " -movflags +faststart " + Quote(output);
        }

        private string FindFfmpeg()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(baseDir, "ffmpeg.exe"),
                Path.Combine(baseDir, "tools", "ffmpeg", "bin", "ffmpeg.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), "tools", "ffmpeg", "bin", "ffmpeg.exe")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var part in path.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    var candidate = Path.Combine(part.Trim(), "ffmpeg.exe");
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
                catch
                {
                }
            }

            return "";
        }

        private void SetStatus(string text)
        {
            statusLabel.Text = text;
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time < TimeSpan.Zero)
            {
                time = TimeSpan.Zero;
            }
            return string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2}.{3:D3}",
                (int)time.TotalHours, time.Minutes, time.Seconds, time.Milliseconds);
        }

        private static string FormatForFfmpeg(TimeSpan time)
        {
            return FormatTime(time);
        }

        private static string TrimForMessage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "没有错误详情。";
            }

            text = text.Trim();
            if (text.Length <= 1800)
            {
                return text;
            }

            return text.Substring(text.Length - 1800);
        }
    }
}
