using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IceArena.Client
{
    // Класс для плавной отрисовки чата без мерцания
    public partial class BufferedFlowLayoutPanel : FlowLayoutPanel
    {
        public BufferedFlowLayoutPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            this.UpdateStyles();
        }
    }

    public partial class SupportForm : Form
    {
        private int userId;
        private string userEmail;
        private ClientForm parentForm;

        // UI элементы
        private Panel chatContainer;
        private BufferedFlowLayoutPanel chatHistory;
        private TextBox txtMessage;
        private Button btnSend;
        private System.Windows.Forms.Timer refreshTimer;

        // Цветовая схема
        private Color primaryColor = Color.FromArgb(0, 136, 204); // Telegram Blue style
        private Color backgroundColor = Color.FromArgb(240, 242, 245);
        private Color userBubbleColor = Color.FromArgb(220, 248, 198); // Светло-зеленый для своих
        private Color adminBubbleColor = Color.White;

        // Переменные для логики чата
        private int lastMessageCount = 0;
        private bool isFirstLoad = true;

        public SupportForm(int userId, string username, ClientForm parent)
        {
            this.userId = userId;
            this.userEmail = username.Contains("@") ? username : "";
            this.parentForm = parent;

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);

            // Таймер автообновления (3 сек)
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 3000;
            refreshTimer.Tick += async (s, e) => await LoadChatHistory(true);

            InitializeUI();

            _ = LoadChatHistory(false);
            refreshTimer.Start();
        }

        private void InitializeUI()
        {
            // 1. Настройка формы
            this.Text = "Поддержка Ice Arena";
            this.Size = new Size(500, 750);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = backgroundColor;
            this.MinimumSize = new Size(400, 500);
            this.Font = new Font("Segoe UI", 10);

            // 2. Верхняя шапка (Header)
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = primaryColor,
                Padding = new Padding(20, 0, 20, 0)
            };
            Label lblTitle = new Label
            {
                Text = "Техническая поддержка",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 12)
            };
            Label lblSubtitle = new Label
            {
                Text = "Среднее время ответа: 5 минут",
                ForeColor = Color.FromArgb(230, 230, 230),
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(20, 40)
            };
            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(lblSubtitle);
            this.Controls.Add(headerPanel);

            // 3. Нижняя панель ввода
            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.White,
                Padding = new Padding(15) // Отступы со всех сторон
            };

            // Рисуем серую линию сверху панели ввода
            bottomPanel.Paint += (s, e) => {
                e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 220)), 0, 0, bottomPanel.Width, 0);
            };

            // КНОПКА ОТПРАВКИ
            btnSend = new Button
            {
                Dock = DockStyle.Right, // Прижимаем вправо
                Width = 60,             // Фиксированная ширина
                Text = "",              // Убираем текст, будем рисовать
                BackColor = primaryColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(10, 0, 0, 0) // Отступ слева от поля ввода
            };
            btnSend.FlatAppearance.BorderSize = 0;

            // --- ИСПРАВЛЕНИЕ: Обновляем форму кнопки при изменении размера ---
            btnSend.SizeChanged += (s, e) =>
            {
                if (btnSend.Width > 0 && btnSend.Height > 0)
                {
                    GraphicsPath btnPath = new GraphicsPath();
                    int r = 15; // Радиус скругления
                    btnPath.AddArc(0, 0, r, r, 180, 90);
                    btnPath.AddArc(btnSend.Width - r, 0, r, r, 270, 90);
                    btnPath.AddArc(btnSend.Width - r, btnSend.Height - r, r, r, 0, 90);
                    btnPath.AddArc(0, btnSend.Height - r, r, r, 90, 90);
                    btnPath.CloseFigure();
                    btnSend.Region = new Region(btnPath);
                }
            };

            // РИСУЕМ СТРЕЛОЧКУ (ИКОНКУ)
            btnSend.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // Центруем стрелку по вертикали и горизонтали относительно реального размера кнопки
                int centerX = btnSend.Width / 2;
                int centerY = btnSend.Height / 2;

                // Смещения точек стрелки относительно центра (примерно 22x20 px)
                Point[] arrowPoints = {
                    new Point(centerX - 10, centerY - 10), // Верхний левый
                    new Point(centerX + 12, centerY),      // Носик
                    new Point(centerX - 10, centerY + 10), // Нижний левый
                    new Point(centerX - 5, centerY)        // Выемка
                };

                using (Brush brush = new SolidBrush(Color.White))
                {
                    e.Graphics.FillPolygon(brush, arrowPoints);
                }
            };
            btnSend.Click += async (s, e) => await SendMessage();

            // ПОЛЕ ВВОДА
            Panel inputContainer = new Panel
            {
                Dock = DockStyle.Fill, // Занимает всё оставшееся место
                Padding = new Padding(0, 0, 10, 0), // Отступ справа до кнопки
                BackColor = Color.White
            };
            txtMessage = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11),
                PlaceholderText = "Напишите сообщение...",
                BackColor = Color.FromArgb(245, 245, 245)
            };

            // Декоративная рамка для поля ввода
            Panel textBorderPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 12, 5, 10),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            // Скругление поля ввода
            textBorderPanel.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, textBorderPanel.Width - 1, textBorderPanel.Height - 1);
                using (GraphicsPath path = GetRoundedRectPath(r, 15))
                using (Pen pen = new Pen(Color.FromArgb(220, 220, 220)))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            // Сборка низа
            textBorderPanel.Controls.Add(txtMessage);
            inputContainer.Controls.Add(textBorderPanel);

            // ВАЖНО: Порядок добавления влияет на стыковку
            // Сначала добавляем кнопку (она прижмется вправо), потом контейнер (он заполнит остаток)
            bottomPanel.Controls.Add(inputContainer);
            bottomPanel.Controls.Add(btnSend);
            // Примечание: В WinForms Z-order работает хитро. Если Dock=Right добавить последним в Controls,
            // он имеет приоритет стыковки перед Dock=Fill, добавленным ранее.

            this.Controls.Add(bottomPanel);

            // 4. Область чата (Центр)
            chatContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = backgroundColor,
                Padding = new Padding(0)
            };
            chatHistory = new BufferedFlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = backgroundColor,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10, 20, 10, 20)
            };

            // Хак для прокрутки
            chatHistory.HorizontalScroll.Maximum = 0;
            chatHistory.AutoScroll = false;
            chatHistory.VerticalScroll.Visible = true;
            chatHistory.AutoScroll = true;

            // Ресайз сообщений при изменении окна
            chatHistory.SizeChanged += (s, e) => {
                foreach (Control c in chatHistory.Controls)
                {
                    c.Width = chatHistory.ClientSize.Width - 25;
                }
            };

            chatContainer.Controls.Add(chatHistory);
            this.Controls.Add(chatContainer);

            // Enter для отправки
            txtMessage.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift && !string.IsNullOrWhiteSpace(txtMessage.Text))
                {
                    e.SuppressKeyPress = true;
                    await SendMessage();
                }
            };
            this.Shown += (s, e) => txtMessage.Focus();
        }

        // --- ЛОГИКА ---

        private async Task LoadChatHistory(bool isBackgroundRefresh)
        {
            try
            {
                var response = await parentForm.SendServerRequest(new
                {
                    Command = "get_support_chat",
                    UserId = userId
                });
                if (response.TryGetProperty("Success", out var success) && success.GetBoolean())
                {
                    if (response.TryGetProperty("Messages", out var messagesElem) &&
                        messagesElem.ValueKind == JsonValueKind.Array)
                    {
                        var messages = new List<(string Text, bool IsFromUser, string Time)>();
                        foreach (var msgElem in messagesElem.EnumerateArray())
                        {
                            string text = msgElem.TryGetProperty("Message", out var t) ? t.GetString() : "";
                            bool isFromUser = msgElem.TryGetProperty("IsFromUser", out var u) && u.GetBoolean();
                            string time = msgElem.TryGetProperty("Timestamp", out var tm) ? tm.GetString() : DateTime.Now.ToString("HH:mm");

                            if (!string.IsNullOrWhiteSpace(text))
                                messages.Add((text, isFromUser, time));
                        }

                        // Добавляем только новые сообщения
                        if (messages.Count > lastMessageCount)
                        {
                            var newMessagesToAdd = isFirstLoad ? messages : messages.Skip(lastMessageCount).ToList();

                            if (newMessagesToAdd.Count > 5) chatHistory.SuspendLayout();
                            foreach (var msg in newMessagesToAdd)
                            {
                                AddMessageToUI(msg.Text, msg.IsFromUser, msg.Time);
                            }

                            if (newMessagesToAdd.Count > 5) chatHistory.ResumeLayout();
                            ScrollToBottom();
                            lastMessageCount = messages.Count;
                        }

                        if (messages.Count == 0 && isFirstLoad) AddWelcomeMessage();
                        isFirstLoad = false;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!isBackgroundRefresh) Console.WriteLine(ex.Message);
            }
        }

        private async Task SendMessage()
        {
            string text = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            txtMessage.Clear();

            // Сразу показываем в чате
            string currentTime = DateTime.Now.ToString("HH:mm");
            AddMessageToUI(text, true, currentTime);
            ScrollToBottom();
            lastMessageCount++;

            try
            {
                var response = await parentForm.SendServerRequest(new
                {
                    Command = "send_support_message",
                    UserId = userId,
                    Email = userEmail,
                    Message = text
                });
                if (response.TryGetProperty("Success", out var success) && success.GetBoolean())
                {
                    // Успешно отправлено
                    await LoadChatHistory(true);
                }
                else
                {
                    AddSystemMessage("Ошибка доставки");
                }
            }
            catch (Exception ex)
            {
                AddSystemMessage($"Ошибка сети: {ex.Message}");
            }
            finally
            {
                txtMessage.Focus();
            }
        }

        private void AddMessageToUI(string text, bool isFromUser, string time)
        {
            int maxWidth = (int)(chatHistory.ClientSize.Width * 0.75);
            Panel rowPanel = new Panel
            {
                Width = chatHistory.ClientSize.Width - 25,
                AutoSize = true,
                Padding = new Padding(0, 5, 0, 5),
                BackColor = Color.Transparent
            };

            Panel bubble = new Panel
            {
                AutoSize = true,
                MaximumSize = new Size(maxWidth, 0),
                Padding = new Padding(12, 10, 12, 10),
                BackColor = Color.Transparent
            };
            Font msgFont = new Font("Segoe UI", 10);

            Label lblText = new Label
            {
                Text = text,
                Font = msgFont,
                ForeColor = Color.Black,
                AutoSize = true,
                MaximumSize = new Size(maxWidth - 24, 0),
                BackColor = Color.Transparent
            };
            Label lblTime = new Label
            {
                Text = time,
                Font = new Font("Segoe UI", 7),
                ForeColor = Color.Gray,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            bubble.Controls.Add(lblText);
            bubble.Controls.Add(lblTime);

            lblText.Location = new Point(12, 10);
            lblTime.Location = new Point(lblText.Right - lblTime.Width, lblText.Bottom + 2);

            // Отрисовка красивого пузыря
            bubble.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, bubble.Width - 1, bubble.Height - 1);

                Color bubColor = isFromUser ? userBubbleColor : adminBubbleColor;
                int r = 15; // Радиус скругления

                using (GraphicsPath path = GetRoundedRectPath(rect, r))
                using (var brush = new SolidBrush(bubColor))
                {
                    e.Graphics.FillPath(brush, path);
                    if (!isFromUser) // Рамка для админа
                    {
                        using (var pen = new Pen(Color.FromArgb(220, 220, 220)))
                            e.Graphics.DrawPath(pen, path);
                    }
                }
            };
            rowPanel.Controls.Add(bubble);

            // Выравнивание
            if (isFromUser)
            {
                bubble.Location = new Point(rowPanel.Width - bubble.PreferredSize.Width - 5, 0);
            }
            else
            {
                bubble.Location = new Point(5, 0);
            }

            // Фикс размера
            bubble.Size = new Size(bubble.PreferredSize.Width, bubble.PreferredSize.Height + 15);
            rowPanel.Height = bubble.Height + 10;

            chatHistory.Controls.Add(rowPanel);
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void AddWelcomeMessage()
        {
            AddSystemMessage("👋 Добро пожаловать! Мы скоро ответим.");
        }

        private void AddSystemMessage(string text)
        {
            Label lbl = new Label
            {
                Text = text,
                AutoSize = false,
                Width = chatHistory.ClientSize.Width - 40,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                Margin = new Padding(20, 5, 20, 5)
            };
            chatHistory.Controls.Add(lbl);
        }

        private void ScrollToBottom()
        {
            if (chatHistory.Controls.Count > 0)
            {
                chatHistory.ScrollControlIntoView(chatHistory.Controls[chatHistory.Controls.Count - 1]);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}