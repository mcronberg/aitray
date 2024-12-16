/* SPDX-License-Identifier: GPL-2.0-only */
/*
 *  Copyright (C) 2024 Gary Sims
 */

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Security.Cryptography;
using System.Reflection;

namespace AITrayApp
{
    public class SettingsManager
    {
        private static readonly string SettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AITrayApp");
        private static readonly string SettingsFile = Path.Combine(SettingsFolder, "settings.bin");
        private static readonly string SettingsFolder2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AITrayApp");
        private static readonly string SettingsFile2 = Path.Combine(SettingsFolder, "settings2.bin");

        public static void SaveApiKey(string apiKey)
        {
            if (!Directory.Exists(SettingsFolder))
            {
                Directory.CreateDirectory(SettingsFolder);
            }

            byte[] plaintextBytes = Encoding.UTF8.GetBytes(apiKey ?? "");
            // Protect the data for the current user so that only the same user/machine can decrypt.
            byte[] encryptedBytes = ProtectedData.Protect(plaintextBytes, null, DataProtectionScope.CurrentUser);

            File.WriteAllBytes(SettingsFile, encryptedBytes);
        }

        public static string GetApiKey()
        {
            if (!File.Exists(SettingsFile))
            {
                return string.Empty;
            }

            try
            {
                byte[] encryptedBytes = File.ReadAllBytes(SettingsFile);
                byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                return string.Empty;
            }
        }
        public static void SaveCustomPrompt(string customPrompt)
        {
            if (!Directory.Exists(SettingsFolder2))
            {
                Directory.CreateDirectory(SettingsFolder2);
            }

            byte[] plaintextBytes = Encoding.UTF8.GetBytes(customPrompt ?? "");
            // Protect the data for the current user so that only the same user/machine can decrypt.
            byte[] encryptedBytes = ProtectedData.Protect(plaintextBytes, null, DataProtectionScope.CurrentUser);

            File.WriteAllBytes(SettingsFile2, encryptedBytes);
        }

        public static string GetCustomPrompt()
        {
            if (!File.Exists(SettingsFile2))
            {
                return string.Empty;
            }

            try
            {
                byte[] encryptedBytes = File.ReadAllBytes(SettingsFile2);
                byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public class GeminiApi
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public GeminiApi(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException("API Key cannot be null or empty.", nameof(apiKey));
            }

            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        }

        public string GenerateText(string prompt, string modelName = "gemini-1.5-flash")
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return "";
            }

            string apiEndpoint = $"v1beta/models/{modelName}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new {
                        parts = new [] {
                            new { text = prompt }
                        }
                    }
                }
            };

            string jsonPayload = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = _httpClient.PostAsync(apiEndpoint, content).Result;
                response.EnsureSuccessStatusCode(); // Throw exception if not success

                var responseBody = response.Content.ReadAsStringAsync().Result;
                var jsonResponse = JsonSerializer.Deserialize<ResponseContent>(responseBody);

                // Extract the generated text
                var candidates = jsonResponse?.candidates;
                if (candidates != null && candidates.Length > 0)
                {
                    var parts = candidates[0].content?.parts;
                    if (parts != null && parts.Length > 0)
                    {
                        return parts[0].text;
                    }
                }
                return "No text generated";
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Error: HTTP Request Failed: {ex.Message}");
                return null;
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Error: JSON Deserialization Failed: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: An unexpected error occurred: {ex.Message}");
                return null;
            }
        }

        //Response Object
        public class ResponseContent
        {
            public Candidates[]? candidates { get; set; }
        }

        public class Candidates
        {
            public string? finishReason { get; set; }
            public Content? content { get; set; }
        }

        public class Content
        {
            public Parts[]? parts { get; set; }
        }

        public class Parts
        {
            public string? text { get; set; }
        }
    }

    static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new TrayApplicationContext());
        }
    }

    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip contextMenu;
        private string _apiKey;
        private string _customPrompt;
        private const string endOfPrompt = " Don't offer multiple options, just rewrite the text as instructed:\n\n";

        public TrayApplicationContext()
        {
            // Load the settings
            _apiKey = SettingsManager.GetApiKey();
            _customPrompt = SettingsManager.GetCustomPrompt();

            // Initialize Context Menu
            contextMenu = new ContextMenuStrip();

            var rewriteItem = new ToolStripMenuItem("Rewrite");
            rewriteItem.Click += RewriteItem_Click;

            var spellItem = new ToolStripMenuItem("Spelling/Grammar");
            spellItem.Click += SpellItem_Click;

            var proItem = new ToolStripMenuItem("Pro");
            proItem.Click += ProItem_Click;

            var friendlyItem = new ToolStripMenuItem("Friendly");
            friendlyItem.Click += FriendlyItem_Click;

            var summarizeItem = new ToolStripMenuItem("Sumarize");
            summarizeItem.Click += SummarizeItem_Click;

            var longerItem = new ToolStripMenuItem("Longer");
            longerItem.Click += LongerItem_Click;

            var extendItem = new ToolStripMenuItem("Extend");
            extendItem.Click += ExtendItem_Click;

            var shortItem = new ToolStripMenuItem("Shorter");
            shortItem.Click += ShortItem_Click;

            var customItem = new ToolStripMenuItem("Custom");
            customItem.Click += CustomItem_Click;

            var settingsItem = new ToolStripMenuItem("Settings");
            settingsItem.Click += SettingsItem_Click;

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += ExitItem_Click;

            contextMenu.Items.Add(rewriteItem);
            contextMenu.Items.Add(spellItem);
            contextMenu.Items.Add(proItem);
            contextMenu.Items.Add(friendlyItem);

            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(summarizeItem);
            contextMenu.Items.Add(longerItem);
            contextMenu.Items.Add(extendItem);
            contextMenu.Items.Add(shortItem);
            contextMenu.Items.Add(customItem);

            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(settingsItem);
            contextMenu.Items.Add(exitItem);

            Icon appIcon = LoadApplicationIcon();

            // Initialize Tray Icon
            trayIcon = new NotifyIcon()
            {
                //Icon = SystemIcons.Application,
                Icon = appIcon,
                ContextMenuStrip = contextMenu,
                Text = "AI Tray",
                Visible = true
            };

            trayIcon.DoubleClick += (s, e) => SettingsItem_Click(null, null);

            if (string.IsNullOrEmpty(_apiKey))
            {
                MessageBox.Show("Please set your API key in Settings.");
                return;
            }
        }

        private Icon LoadApplicationIcon()
        {
            // Get the assembly (your .exe)
            var assembly = Assembly.GetEntryAssembly();

            if (assembly == null)
            {
                MessageBox.Show("Error: Could not get the entry assembly. Using system application icon");
                return SystemIcons.Application;
            }

            // Get the main executable path
            string? executablePath = assembly.Location;

            if (string.IsNullOrEmpty(executablePath))
            {
                MessageBox.Show("Error: Could not get the executable path. Using system application icon");
                return SystemIcons.Application;
            }

            // Extract the icon from the executable
            try
            {
                return Icon.ExtractAssociatedIcon(executablePath)!;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: Could not load application icon from executable: {ex.Message}. Using system application icon");
                return SystemIcons.Application;
            }

        }

        private void RewriteItem_Click(object sender, EventArgs e)
        {
            string p = "Transform the text below to enhance its engagement, improve clarity, and optimize readability.";
            
            if (Clipboard.ContainsText())
            {
                string clipboardText = Clipboard.GetText();
                TalkToGemini(p + endOfPrompt + clipboardText);

            }
        }
        private void SpellItem_Click(object sender, EventArgs e)
        {
            string p = "Correct the spelling and grammar in the following text.";

            if (Clipboard.ContainsText())
            {
                string clipboardText = Clipboard.GetText();
                TalkToGemini(p + endOfPrompt + clipboardText);

            }
        }

        private void ProItem_Click(object sender, EventArgs e)
        {
            string p = "Rewrite the following text to make it sound more professional and formal while keeping the original message intact.";

            if (Clipboard.ContainsText())
            {
                string clipboardText = Clipboard.GetText();
                TalkToGemini(p + endOfPrompt + clipboardText);

            }
        }
        private void FriendlyItem_Click(object sender, EventArgs e)
        {
            string p = "Rephrase the text below to make it sound more friendly and conversational, while keeping the original message clear and unchanged.";

            if (Clipboard.ContainsText())
            {
                string clipboardText = Clipboard.GetText();
                TalkToGemini(p + endOfPrompt + clipboardText);

            }
        }
        private void SummarizeItem_Click(object sender, EventArgs e)
        {
            string p = "Summarize the following text.";

            if (Clipboard.ContainsText())
            {
                string clipboardText = Clipboard.GetText();
                TalkToGemini(p + endOfPrompt + clipboardText);

            }
        }
        private void LongerItem_Click(object sender, EventArgs e)
        {
            string p = "Expand on the text below to make it about 10% longer, while maintaining the original meaning.";

            if (Clipboard.ContainsText())
            {
                string clipboardText = Clipboard.GetText();
                TalkToGemini(p + endOfPrompt + clipboardText);

            }
        }

        private void ExtendItem_Click(object sender, EventArgs e)
        {
            string p = "Expand on the text below to make it longer, adding relevant details and examples, while maintaining the original meaning.";

            if (Clipboard.ContainsText())
            {
                string clipboardText = Clipboard.GetText();
                TalkToGemini(p + endOfPrompt + clipboardText);

            }
        }

        private void ShortItem_Click(object sender, EventArgs e)
        {
            string p = "Rewrite the following text to make it more succinct, without losing its meaning.";

            if (Clipboard.ContainsText())
            {
                string clipboardText = Clipboard.GetText();
                TalkToGemini(p + endOfPrompt + clipboardText);

            }
        }

        private void CustomItem_Click(object sender, EventArgs e)
        {
            string p = _customPrompt;

            if (Clipboard.ContainsText())
            {
                string clipboardText = Clipboard.GetText();
                TalkToGemini(p + ":\n\n" + clipboardText);

            }
        }

        private void ExitItem_Click(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

        private void SettingsItem_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm(_apiKey, _customPrompt))
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    _apiKey = settingsForm.ApiKey;
                    SettingsManager.SaveApiKey(_apiKey);
                    _customPrompt = settingsForm.CustomPrompt;
                    SettingsManager.SaveCustomPrompt(_customPrompt);
                }
            }
        }

        private void TalkToGemini(string prompt)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                MessageBox.Show("Please set your API key in Settings before using this feature.");
                return;
            }

            try
            {
                var geminiApi = new GeminiApi(_apiKey);
                string response = geminiApi.GenerateText(prompt);

                if (response != null)
                {
                    Clipboard.SetText(response);
                }
                else
                {
                    MessageBox.Show("Failed to get Gemini response.");
                }
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: An unexpected error occurred: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && trayIcon != null)
            {
                trayIcon.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class SettingsForm : Form
    {
        private TextBox apiKeyTextBox;
        private TextBox customPromptTextBox;
        private Button saveButton;
        private Button cancelButton;

        public string ApiKey { get; private set; }
        public string CustomPrompt { get; private set; }

        public SettingsForm(string currentApiKey, string currentCustomPrompt)
        {
            this.Text = "Settings";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.Width = 400;
            this.Height = 180;

            Label apiKeyLabel = new Label
            {
                Text = "API Key:",
                Left = 10,
                Top = 20,
                AutoSize = true
            };

            apiKeyTextBox = new TextBox
            {
                Left = 70,
                Top = 15,
                Width = 300,
                Text = currentApiKey
            };

            Label customPromptLabel = new Label
            {
                Text = "Custom Prompt:",
                Left = 10,
                Top = 60,
                AutoSize = true
            };

            customPromptTextBox = new TextBox
            {
                Left = 120,
                Top = 55,
                Width = 250,
                Text = currentCustomPrompt
            };

            saveButton = new Button
            {
                Text = "Save",
                Left = 210,
                Width = 75,
                Top = 100,
                DialogResult = DialogResult.OK
            };
            saveButton.Click += SaveButton_Click;

            cancelButton = new Button
            {
                Text = "Cancel",
                Left = 295,
                Width = 75,
                Top = 100,
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(apiKeyLabel);
            this.Controls.Add(apiKeyTextBox);
            this.Controls.Add(customPromptLabel);
            this.Controls.Add(customPromptTextBox);
            this.Controls.Add(saveButton);
            this.Controls.Add(cancelButton);

            this.AcceptButton = saveButton;
            this.CancelButton = cancelButton;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            ApiKey = apiKeyTextBox.Text.Trim();
            CustomPrompt = customPromptTextBox.Text.Trim();
        }
    }
}
