using System;
using System.Drawing;
using System.Windows.Forms;

namespace IceArena.Client
{
    partial class SupportForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // --- ИСПРАВЛЕНИЕ: Используем BufferedFlowLayoutPanel вместо FlowLayoutPanel ---
            // Это устраняет ошибку CS0266
            chatHistory = new BufferedFlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.FromArgb(245, 248, 250),
                Padding = new Padding(10)
            };
            this.Controls.Add(chatHistory);

            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            this.Controls.Add(bottomPanel);

            txtMessage = new TextBox
            {
                Location = new Point(10, 15),
                Size = new Size(380, 30),
                Font = new Font("Segoe UI", 10)
            };
            bottomPanel.Controls.Add(txtMessage);

            Button btnSend = new Button
            {
                Text = "➤",
                Location = new Point(400, 10),
                Size = new Size(70, 40),
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };
            btnSend.Click += async (s, e) => await SendMessage();
            bottomPanel.Controls.Add(btnSend);

            // Basic form settings
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 600);
            this.Text = "Техническая поддержка";
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
        }

        #endregion
    }
}