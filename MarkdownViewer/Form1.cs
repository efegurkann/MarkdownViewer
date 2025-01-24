using System;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using TheArtOfDev.HtmlRenderer.WinForms;
using System.Collections.Generic;
using Markdig;

namespace MarkdownViewer
{
    public partial class Form1 : Form
    {
        private TextBox markdownTextBox = null!;
        private HtmlPanel previewPanel = null!;
        private SplitContainer splitContainer = null!;
        private MenuStrip menuStrip = null!;
        private string? currentFilePath;
        private MarkdownPipeline? pipeline;
        private readonly Dictionary<string, string> emojiMap;

        public Form1()
        {
            emojiMap = new Dictionary<string, string>
            {
                {":smile:", "😊"},
                {":heart:", "❤️"},
                {":rocket:", "🚀"},
                {":wave:", "👋"},
                {":tada:", "🎉"},
                {":check:", "✅"},
                {":x:", "❌"},
                {":star:", "⭐"},
                {":warning:", "⚠️"},
                {":info:", "ℹ️"}
            };

            InitializeComponent();
            InitializeMarkdownPipeline();
            SetupUI();
        }

        private void InitializeMarkdownPipeline() => 
            pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

        private void SetupUI()
        {
            ConfigureForm();
            ConfigureMenuStrip();
            ConfigureSplitContainer();
            ConfigureEditorAndPreview();
            LoadDefaultContent();
        }

        private void ConfigureForm()
        {
            Text = "Markdown Görüntüleyici";
            Size = new Size(1400, 800);
            BackColor = Color.FromArgb(45, 45, 48);
            ForeColor = Color.White;
            Icon = new Icon(Path.Combine(Application.StartupPath, "Resources", "markdown.ico"));
            StartPosition = FormStartPosition.CenterScreen;
        }

        private void ConfigureMenuStrip()
        {
            menuStrip = new MenuStrip
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            var dosyaMenu = new ToolStripMenuItem("Dosya");
            dosyaMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("Yeni", null, YeniDosya_Click),
                new ToolStripMenuItem("Aç", null, DosyaAc_Click),
                new ToolStripMenuItem("Kaydet", null, DosyaKaydet_Click),
                new ToolStripMenuItem("Farklı Kaydet", null, FarkliKaydet_Click)
            });

            var gorunumMenu = new ToolStripMenuItem("Görünüm");
            gorunumMenu.DropDownItems.Add("Tema Değiştir", null, TemaDegistir_Click);

            menuStrip.Items.AddRange(new ToolStripItem[] { dosyaMenu, gorunumMenu });
            MainMenuStrip = menuStrip;
            Controls.Add(menuStrip);
        }

        private void ConfigureSplitContainer()
        {
            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BackColor = Color.FromArgb(30, 30, 30),
                Panel1MinSize = 100,
                Panel2MinSize = 100,
                IsSplitterFixed = false,
                Margin = new Padding(0),
                Padding = new Padding(10)
            };

            Load += (s, e) => UpdateSplitterDistance();
            SizeChanged += (s, e) => UpdateSplitterDistance();
        }

        private void UpdateSplitterDistance()
        {
            try
            {
                if (splitContainer.Width <= splitContainer.Panel1MinSize + splitContainer.Panel2MinSize) return;

                int desiredDistance = splitContainer.Width / 2;
                int maxDistance = splitContainer.Width - splitContainer.Panel2MinSize;
                int minDistance = splitContainer.Panel1MinSize;
                splitContainer.SplitterDistance = Math.Max(minDistance, Math.Min(desiredDistance, maxDistance));
            }
            catch { }
        }

        private void ConfigureEditorAndPreview()
        {
            markdownTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Cascadia Code", 12),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Margin = new Padding(10),
                AcceptsTab = true
            };

            previewPanel = new HtmlPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Margin = new Padding(10)
            };

            var editorPanel = CreatePanel();
            var previewContainerPanel = CreatePanel();

            var editorLabel = CreateLabel("Markdown Editör");
            var previewLabel = CreateLabel("Önizleme");

            editorPanel.Controls.Add(markdownTextBox);
            editorPanel.Controls.Add(editorLabel);
            previewContainerPanel.Controls.Add(previewPanel);
            previewContainerPanel.Controls.Add(previewLabel);

            splitContainer.Panel1.Controls.Add(editorPanel);
            splitContainer.Panel2.Controls.Add(previewContainerPanel);

            var containerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            containerPanel.Controls.Add(splitContainer);

            Controls.Add(containerPanel);
            markdownTextBox.TextChanged += MarkdownTextBox_TextChanged;
        }

        private Panel CreatePanel() => new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 30, 30),
            Padding = new Padding(10)
        };

        private Label CreateLabel(string text) => new()
        {
            Text = text,
            Dock = DockStyle.Top,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Padding = new Padding(5)
        };

        private void LoadDefaultContent() => markdownTextBox.Text =
            "# Markdown Görüntüleyici'ye Hoş Geldiniz! 👋\n\n" +
            "Bu editör şunları destekler:\n\n" +
            "- Emojiler 🎉 ❤️ 🚀\n" +
            "- **Kalın** ve *italik* yazı\n" +
            "- Kod blokları:\n\n" +
            "```csharp\npublic class Program\n{\n    public static void Main()\n    {\n        Console.WriteLine(\"Merhaba Dünya!\");\n    }\n}\n```\n\n" +
            "> Alıntılar da desteklenir!\n\n" +
            "| Tablo | Desteği |\n|--------|----------|\n| Var | ✅ |";

        private string ProcessEmojis(string markdown)
        {
            foreach (var emoji in emojiMap)
            {
                markdown = markdown.Replace(emoji.Key, emoji.Value);
            }
            return markdown;
        }

        private void MarkdownTextBox_TextChanged(object? sender, EventArgs e)
        {
            if (pipeline == null) return;

            string markdown = ProcessEmojis(markdownTextBox.Text);
            string html = Markdown.ToHtml(markdown, pipeline);
            
            html = $@"
                <html>
                <head>
                    <meta charset=""UTF-8"">
                    <style>
                        body {{ font-family: 'Segoe UI', sans-serif; line-height: 1.6; padding: 20px; }}
                        pre {{ background-color: #f6f8fa; padding: 16px; border-radius: 6px; overflow: auto; }}
                        code {{ font-family: 'Cascadia Code', monospace; }}
                        table {{ border-collapse: collapse; width: 100%; margin: 16px 0; }}
                        th, td {{ border: 1px solid #ddd; padding: 12px; text-align: left; }}
                        th {{ background-color: #f6f8fa; font-weight: 600; }}
                        tr:nth-child(even) {{ background-color: #f8f9fa; }}
                        blockquote {{ border-left: 4px solid #0366d6; margin: 0; padding: 0 16px; color: #666; background-color: #f6f8fa; }}
                        img {{ max-width: 100%; height: auto; display: block; margin: 16px 0; }}
                        h1, h2, h3 {{ color: #333; border-bottom: 1px solid #eee; padding-bottom: 8px; }}
                        h1 {{ font-size: 2em; }}
                        h2 {{ font-size: 1.5em; }}
                        h3 {{ font-size: 1.2em; }}
                        ul, ol {{ padding-left: 2em; }}
                        li {{ margin: 4px 0; }}
                        a {{ color: #0366d6; text-decoration: none; }}
                        a:hover {{ text-decoration: underline; }}
                        hr {{ border: 0; border-top: 1px solid #eee; margin: 16px 0; }}
                        .task-list-item {{ list-style-type: none; }}
                        .task-list-item-checkbox {{ margin-right: 8px; }}
                    </style>
                </head>
                <body>{html}</body>
                </html>";

            previewPanel.Text = html;
        }

        private void TemaDegistir_Click(object? sender, EventArgs e)
        {
            if (previewPanel.BackColor == Color.White)
            {
                previewPanel.BackColor = Color.FromArgb(30, 30, 30);
                string html = previewPanel.Text;
                html = html.Replace("background-color: #f6f8fa", "background-color: #2d2d2d")
                         .Replace("color: #333", "color: #fff")
                         .Replace("body {", "body { color: #fff; background-color: #1e1e1e;");
                previewPanel.Text = html;
            }
            else
            {
                previewPanel.BackColor = Color.White;
                MarkdownTextBox_TextChanged(null, null);
            }
        }

        private void YeniDosya_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(markdownTextBox.Text))
            {
                var result = MessageBox.Show("Kaydedilmemiş değişiklikler var. Kaydetmek ister misiniz?", 
                    "Uyarı", MessageBoxButtons.YesNoCancel);
                
                if (result == DialogResult.Yes)
                {
                    DosyaKaydet_Click(sender, e);
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            currentFilePath = null;
            markdownTextBox.Clear();
        }

        private void DosyaAc_Click(object? sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = "Markdown Dosyaları (*.md)|*.md|Tüm Dosyalar (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                currentFilePath = openFileDialog.FileName;
                markdownTextBox.Text = File.ReadAllText(currentFilePath);
            }
        }

        private void DosyaKaydet_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                FarkliKaydet_Click(sender, e);
            }
            else
            {
                File.WriteAllText(currentFilePath, markdownTextBox.Text);
            }
        }

        private void FarkliKaydet_Click(object? sender, EventArgs e)
        {
            using var saveFileDialog = new SaveFileDialog
            {
                Filter = "Markdown Dosyası (*.md)|*.md|Tüm Dosyalar (*.*)|*.*",
                FilterIndex = 1
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                currentFilePath = saveFileDialog.FileName;
                File.WriteAllText(currentFilePath, markdownTextBox.Text);
            }
        }
    }
}
