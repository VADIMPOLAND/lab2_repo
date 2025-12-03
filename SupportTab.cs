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


    public class SupportUser
    {
        public int Id { get; set; }
        public string Email { get; set; }

        public override string ToString()
        {
            return Email;
        }
    }

    public partial class SupportTab : UserControl
    {
        private AdminForm parentForm;
        private ListBox userList;
        private BufferedFlowLayoutPanel chatHistory; // Используем буферизированную панель
        private TextBox txtMessage;
        private Label lblSelectedUser;
        private SupportUser currentSelectedUser;
        private Button btnSend;
        private System.Windows.Forms.Timer refreshTimer;

        // --- МОДНАЯ ЦВЕТОВАЯ ПАЛИТРА ---
        private Color accentColor = Color.FromArgb(0, 120, 215);    // Акцентный синий
        private Color bgMain = Color.FromArgb(245, 247, 251);    // Светло-серый фон чата
        private Color bgWhite = Color.White;
        private Color textDark = Color.FromArgb(30, 30, 30);
        private Color textGray = Color.FromArgb(101, 103, 107);
        private Color bubbleMy = Color.FromArgb(220, 248, 198);    // Светло-зеленый (для админа)
        private Color bubbleUser = Color.White;
        private Color selectionColor = Color.FromArgb(232, 240, 254); // Легкий голубой для выделения

        public SupportTab()
        {
            InitializeUI();

            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 3000;
            refreshTimer.Tick += async (s, e) => await RefreshCurrentChat();
        }

        public void SetParent(AdminForm parent)
        {
            this.parentForm = parent;
            _ = LoadActiveChats();
            refreshTimer.Start();
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = bgMain;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);

            // Основной контейнер
            SplitContainer splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel1,
                SplitterWidth = 1,
                BackColor = Color.FromArgb(220, 220, 220), // Цвет разделителя
            };

            // ВАЖНО: Принудительно устанавливаем минимальную ширину левой панели
            splitContainer.Panel1MinSize = 300;
            splitContainer.SplitterDistance = 320;

            this.Controls.Add(splitContainer);

            // ================== ЛЕВАЯ ПАНЕЛЬ (СПИСОК) ==================
            Panel leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = bgWhite };

            // Шапка левой панели
            Panel leftHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = accentColor,
                Padding = new Padding(15)
            };
            Label lblHeaderTitle = new Label
            {
                Text = "Обращения", // Исправлено слово
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            leftHeader.Controls.Add(lblHeaderTitle);

            // Кнопка обновления
            Button btnRefresh = new Button
            {
                Text = "🔄 Обновить",
                Dock = DockStyle.Bottom,
                Height = 45,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = textDark,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += async (s, e) => await LoadActiveChats();

            // Список пользователей (ListBox с кастомной отрисовкой)
            userList = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                ItemHeight = 70, // Увеличена высота для лучшего вида Email
                DrawMode = DrawMode.OwnerDrawFixed,
                BackColor = bgWhite,
                IntegralHeight = false
            };

            // Кастомная отрисовка элементов списка
            userList.DrawItem += (s, e) =>
            {
                e.DrawBackground();
                if (e.Index < 0 || e.Index >= userList.Items.Count) return;

                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                SupportUser user = userList.Items[e.Index] as SupportUser;
                if (user == null) return;

                // Фон элемента
                using (SolidBrush bgBrush = new SolidBrush(isSelected ? selectionColor : bgWhite))
                {
                    g.FillRectangle(bgBrush, e.Bounds);
                }

                // Индикатор выделения слева
                if (isSelected)
                {
                    using (SolidBrush barBrush = new SolidBrush(accentColor))
                    {
                        g.FillRectangle(barBrush, e.Bounds.X, e.Bounds.Y, 4, e.Bounds.Height);
                    }
                }

                // Рисуем круглую "аватарку"
                int avatarSize = 40;
                Rectangle avatarRect = new Rectangle(e.Bounds.X + 15, e.Bounds.Y + (e.Bounds.Height - avatarSize) / 2, avatarSize, avatarSize);
                using (SolidBrush avatarBrush = new SolidBrush(Color.LightGray))
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(avatarRect);
                    g.FillPath(avatarBrush, path);

                    // Первая буква email
                    string initial = string.IsNullOrEmpty(user.Email) ? "?" : user.Email.Substring(0, 1).ToUpper();
                    TextRenderer.DrawText(g, initial, new Font("Segoe UI", 12, FontStyle.Bold), avatarRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }

                // Email (теперь помещается)
                Rectangle emailRect = new Rectangle(e.Bounds.X + 70, e.Bounds.Y + 12, e.Bounds.Width - 80, 20);
                TextRenderer.DrawText(g, user.Email, new Font("Segoe UI", 11, FontStyle.Bold), emailRect, textDark, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

                // ID
                Rectangle subTextRect = new Rectangle(e.Bounds.X + 70, e.Bounds.Y + 36, e.Bounds.Width - 80, 20);
                TextRenderer.DrawText(g, $"ID: {user.Id}", new Font("Segoe UI", 9), subTextRect, textGray, TextFormatFlags.Left);

                // Линия разделитель
                using (Pen linePen = new Pen(Color.FromArgb(240, 240, 240)))
                {
                    g.DrawLine(linePen, e.Bounds.X + 70, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                }

                e.DrawFocusRectangle();
            };
            userList.SelectedIndexChanged += UserList_SelectedIndexChanged;

            leftPanel.Controls.Add(userList);
            leftPanel.Controls.Add(btnRefresh);
            leftPanel.Controls.Add(leftHeader);
            splitContainer.Panel1.Controls.Add(leftPanel);

            // ================== ПРАВАЯ ПАНЕЛЬ (ЧАТ) ==================
            Panel rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = bgMain };

            // Шапка чата
            Panel rightHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = bgWhite,
                Padding = new Padding(20, 0, 20, 0)
            };

            lblSelectedUser = new Label
            {
                Text = "Выберите диалог из списка слева",
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = textDark
            };
            rightHeader.Controls.Add(lblSelectedUser);
            rightHeader.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(220, 220, 220) });

            // Область переписки
            chatHistory = new BufferedFlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = bgMain,
                Padding = new Padding(20)
            };

            // --- ПАНЕЛЬ ВВОДА (ИСПРАВЛЕННАЯ И ПОДНЯТАЯ) ---
            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 85, // Увеличена высота
                BackColor = bgWhite,
                Padding = new Padding(15)
            };

            // Кнопка отправки
            btnSend = new Button
            {
                Text = "Отправить",
                Dock = DockStyle.Right,
                Width = 110,
                FlatStyle = FlatStyle.Flat,
                BackColor = accentColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += async (s, e) => await SendMessage();

            // Контейнер для поля ввода (имитация современной рамки)
            Panel txtContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(5),
                Margin = new Padding(0, 0, 10, 0)
            };

            // Само поле ввода
            txtMessage = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.Black,
                MaxLength = 1000,
                PlaceholderText = "Напишите сообщение...",
                Enabled = false,
                BackColor = Color.White
            };

            // Отрисовка рамки
            txtContainer.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = txtContainer.ClientRectangle;
                r.Width -= 1; r.Height -= 1;
                // Рисуем скругленную рамку
                using (Pen p = new Pen(Color.FromArgb(200, 200, 200), 1))
                {
                    using (GraphicsPath path = GetRoundedRectPath(r, 10))
                    {
                        e.Graphics.DrawPath(p, path);
                    }
                }
            };

            txtContainer.Controls.Add(txtMessage);

            bottomPanel.Controls.Add(txtContainer);
            bottomPanel.Controls.Add(btnSend);
            bottomPanel.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(220, 220, 220) });

            rightPanel.Controls.Add(chatHistory);
            rightPanel.Controls.Add(bottomPanel);
            rightPanel.Controls.Add(rightHeader);

            splitContainer.Panel2.Controls.Add(rightPanel);

            // Обработчики ввода
            txtMessage.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift && btnSend.Enabled)
                {
                    e.SuppressKeyPress = true;
                    await SendMessage();
                }
            };
            txtMessage.TextChanged += (s, e) =>
            {
                btnSend.Enabled = !string.IsNullOrWhiteSpace(txtMessage.Text) && currentSelectedUser != null;
                btnSend.BackColor = btnSend.Enabled ? accentColor : Color.LightGray;
            };
        }

        // Вспомогательный метод для рисования скругленных углов
        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }


        // ================== ЛОГИКА (БЕЗ ИЗМЕНЕНИЙ) ==================

        private async Task LoadActiveChats()
        {
            if (parentForm == null) return;
            try
            {
                var response = await parentForm.SendServerRequest(new { Command = "get_active_support_chats" });
                if (response.TryGetProperty("Success", out JsonElement successElem) &&
                    successElem.GetBoolean() &&
                    response.TryGetProperty("Users", out JsonElement usersElem))
                {
                    int selectedId = currentSelectedUser?.Id ?? -1;
                    userList.BeginUpdate();
                    userList.Items.Clear();

                    foreach (var userElem in usersElem.EnumerateArray())
                    {
                        var user = new SupportUser
                        {
                            Id = userElem.GetProperty("Id").GetInt32(),
                            Email = userElem.GetProperty("Email").GetString()
                        };
                        userList.Items.Add(user);

                        if (user.Id == selectedId)
                        {
                            userList.SelectedItem = user;
                        }
                    }
                    userList.EndUpdate();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки списка чатов: {ex.Message}");
            }
        }

        private async void UserList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (userList.SelectedItem is SupportUser user)
            {
                currentSelectedUser = user;
                lblSelectedUser.Text = $"{user.Email} (ID: {user.Id})";
                txtMessage.Enabled = true;
                btnSend.Enabled = !string.IsNullOrWhiteSpace(txtMessage.Text);
                txtMessage.Focus();

                await LoadChatHistory(user.Id);
            }
            else
            {
                currentSelectedUser = null;
                lblSelectedUser.Text = "Выберите диалог из списка слева";
                txtMessage.Enabled = false;
                btnSend.Enabled = false;
                chatHistory.Controls.Clear();
            }
        }

        private async Task RefreshCurrentChat()
        {
            if (currentSelectedUser != null)
            {
                await LoadChatHistory(currentSelectedUser.Id, true);
            }
        }

        private async Task LoadChatHistory(int targetUserId, bool isBackgroundRefresh = false)
        {
            try
            {
                var request = new { Command = "get_chat_history", TargetUserId = targetUserId };
                var response = await parentForm.SendServerRequest(request);

                if (response.TryGetProperty("Success", out JsonElement successElem) &&
                    successElem.GetBoolean() &&
                    response.TryGetProperty("Messages", out JsonElement messagesElem))
                {
                    var messages = new List<(string Text, bool IsFromUser, string Time)>();
                    foreach (var msgElem in messagesElem.EnumerateArray())
                    {
                        messages.Add((
                            msgElem.GetProperty("Message").GetString(),
                            msgElem.GetProperty("IsFromUser").GetBoolean(),
                            msgElem.GetProperty("Date").GetString()
                        ));
                    }

                    if (!isBackgroundRefresh || messages.Count != chatHistory.Controls.Count)
                    {
                        chatHistory.SuspendLayout();
                        chatHistory.Controls.Clear();

                        foreach (var msg in messages)
                        {
                            // Инвертируем: IsFromUser=true (от клиента) для админа это "Входящее" (слева)
                            AddMessageToUI(msg.Text, !msg.IsFromUser, msg.Time);
                        }

                        chatHistory.ResumeLayout();
                        ScrollToBottom();
                    }
                }
            }
            catch (Exception ex)
            {
                if (!isBackgroundRefresh)
                {
                    AddSystemMessage($"Ошибка загрузки истории: {ex.Message}");
                }
            }
        }

        private async Task SendMessage()
        {
            if (currentSelectedUser == null) return;
            string messageText = txtMessage.Text.Trim();
            if (string.IsNullOrWhiteSpace(messageText)) return;

            try
            {
                txtMessage.Enabled = false;
                btnSend.Enabled = false;

                var request = new
                {
                    Command = "send_support_message_as_admin",
                    TargetUserId = currentSelectedUser.Id,
                    Message = messageText
                };

                var response = await parentForm.SendServerRequest(request);
                if (response.TryGetProperty("Success", out JsonElement successElem) && successElem.GetBoolean())
                {
                    // Сообщение успешно отправлено, добавляем его как "мое"
                    AddMessageToUI(messageText, true, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    txtMessage.Clear();
                    ScrollToBottom(); // Плавный скролл вниз
                }
                else
                {
                    string error = response.TryGetProperty("Error", out JsonElement errorElem)
                        ? errorElem.GetString()
                        : "Неизвестная ошибка";
                    MessageBox.Show($"Ошибка отправки: {error}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сети: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                txtMessage.Enabled = true;
                btnSend.Enabled = !string.IsNullOrWhiteSpace(txtMessage.Text);
                txtMessage.Focus();
            }
        }

        // ================== ОТРИСОВКА СООБЩЕНИЙ (КРАСИВЫЙ СТИЛЬ) ==================

        private void AddMessageToUI(string text, bool isMyMessage, string time)
        {
            // Ширина контейнера (динамически)
            int maxWidth = (int)(chatHistory.ClientSize.Width * 0.7);

            // Панель-обертка для всей строки
            Panel rowPanel = new Panel
            {
                Width = chatHistory.ClientSize.Width - 25, // -scrollbar
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 5),
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };

            // "Пузырь" сообщения
            Panel bubble = new Panel
            {
                AutoSize = true,
                MaximumSize = new Size(maxWidth, 0),
                Padding = new Padding(12),
                BackColor = isMyMessage ? bubbleMy : bubbleUser,
            };

            // Текст
            Label lblText = new Label
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size(maxWidth - 24, 0),
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = textDark,
                BackColor = Color.Transparent
            };

            // Время
            Label lblTime = new Label
            {
                Text = time,
                AutoSize = true,
                Font = new Font("Segoe UI", 8),
                ForeColor = textGray,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 4, 0, 0)
            };

            // Добавляем контролы в пузырь, чтобы рассчитать размер
            bubble.Controls.Add(lblText);
            bubble.Controls.Add(lblTime);

            // Позиционирование внутри пузыря
            lblText.Location = new Point(12, 12);

            // Хак: сперва добавляем, чтобы сработал AutoSize
            bubble.Width = Math.Max(lblText.Width + 24, lblTime.Width + 24);
            bubble.Height = lblText.Height + lblTime.Height + 24 + 5;

            lblTime.Location = new Point(bubble.Width - lblTime.Width - 10, lblText.Bottom + 5);

            // Уточняем положение пузыря внутри строки
            if (isMyMessage)
            {
                bubble.Location = new Point(rowPanel.Width - bubble.Width - 10, 0); // -10 отступ справа
            }
            else
            {
                bubble.Location = new Point(10, 0); // 10 отступ слева
            }

            // Кастомная отрисовка фона пузыря (скругленные углы)
            bubble.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, bubble.Width - 1, bubble.Height - 1);

                int rVal = 16;
                GraphicsPath path = new GraphicsPath();
                path.AddArc(r.X, r.Y, rVal, rVal, 180, 90); // Top Left

                // Делаем угол с правой стороны острым, как "хвостик"
                if (isMyMessage) path.AddLine(r.Right - rVal, r.Y, r.Right, r.Y);
                else path.AddArc(r.Right - rVal, r.Y, rVal, rVal, 270, 90);

                path.AddArc(r.Right - rVal, r.Bottom - rVal, rVal, rVal, 0, 90); // Bottom Right
                path.AddArc(r.X, r.Bottom - rVal, rVal, rVal, 90, 90); // Bottom Left
                path.CloseFigure();

                using (SolidBrush b = new SolidBrush(bubble.BackColor))
                {
                    e.Graphics.FillPath(b, path);
                }

                // Легкая тень/граница
                using (Pen p = new Pen(Color.FromArgb(15, 0, 0, 0)))
                {
                    e.Graphics.DrawPath(p, path);
                }
            };

            rowPanel.Controls.Add(bubble);
            rowPanel.Height = bubble.Height + 10;

            chatHistory.Controls.Add(rowPanel);
        }

        private void AddSystemMessage(string text)
        {
            Panel systemPanel = new Panel
            {
                Width = chatHistory.ClientSize.Width - 40,
                AutoSize = true,
                Margin = new Padding(0, 15, 0, 15),
                BackColor = Color.Transparent
            };
            Label systemLabel = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = textGray,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 20
            };
            systemPanel.Controls.Add(systemLabel);
            chatHistory.Controls.Add(systemPanel);
        }

        private void ScrollToBottom()
        {
            // Плавный скролл к последнему элементу
            if (chatHistory.VerticalScroll.Visible && chatHistory.Controls.Count > 0)
            {
                chatHistory.ScrollControlIntoView(chatHistory.Controls[chatHistory.Controls.Count - 1]);
            }
        }
    }
}