using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace IceArena.Client
{
    public partial class ClientForm : Form
    {
        // --- ПАЛИТРА ---
        private readonly Color clrBackground = Color.FromArgb(241, 245, 249); // Светлый фон
        private readonly Color clrCard = Color.White; // Фон карточки таблицы

        // Цвета бренда
        private readonly Color clrPrimary = Color.FromArgb(37, 99, 235);    // Насыщенный синий (Action)
        private readonly Color clrHeaderBg = Color.FromArgb(30, 41, 59);    // Темный фон шапки сайта

        // Цвета заголовка таблицы (Четкое разделение описания и данных)
        private readonly Color clrGridHeaderBg = Color.FromArgb(71, 85, 105); // Серый для заголовков столбцов
        private readonly Color clrGridHeaderText = Color.White;

        // Статусы
        private readonly Color clrSuccessBg = Color.FromArgb(220, 252, 231);
        private readonly Color clrSuccessText = Color.FromArgb(21, 128, 61);
        private readonly Color clrDangerBg = Color.FromArgb(254, 226, 226);
        private readonly Color clrDangerText = Color.FromArgb(185, 28, 28);
        private readonly Color clrInfoBg = Color.FromArgb(219, 234, 254);
        private readonly Color clrInfoText = Color.FromArgb(29, 78, 216);

        // Текст
        private readonly Color clrTextMain = Color.FromArgb(15, 23, 42);

        // --- ЭЛЕМЕНТЫ УПРАВЛЕНИЯ ---
        private DataGridView dgvSchedule;
        private Button btnBooking, btnProfile, btnCancelBooking, btnExit, btnRefresh;
        private Label lblTitle, lblWelcome;
        private Panel panelMain, panelHeader, panelContent, panelButtons;
        private PictureBox picUserAvatar;
        private FlowLayoutPanel flowButtonsPanel;

        // --- ДАННЫЕ ---
        private string currentUser;
        private int currentUserId;
        public List<Booking> UserBookings { get; private set; } = new List<Booking>();
        public List<Review> UserReviews { get; private set; } = new List<Review>();
        public bool IsGuestMode { get; set; }

        private bool isLoading = false;
        private readonly object loadingLock = new object();

        public ClientForm(string username, int userId, bool isGuestMode = false)
        {
            currentUser = username;
            currentUserId = userId;
            IsGuestMode = isGuestMode;

            InitializeComponents();
            SetupModernUI();

            this.Shown += async (s, e) => await InitializeDataAsync();
            this.SizeChanged += (s, e) => CenterButtons();
        }

        private void InitializeComponents()
        {
            this.SuspendLayout();
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Name = "ClientForm";
            this.Text = IsGuestMode ? "Ледовая Арена - Гость" : $"Ледовая Арена - {currentUser}";
            this.ResumeLayout(false);
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                if (!IsGuestMode)
                {
                    UserBookings = await LoadUserBookingsFromServer();
                }
                await LoadScheduleFromServer();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupModernUI()
        {
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = clrBackground;
            this.Font = new Font("Segoe UI", 10F);

            panelMain = new Panel { Dock = DockStyle.Fill, BackColor = clrBackground };
            this.Controls.Add(panelMain);

            // 1. ШАПКА
            SetupHeader();

            // 2. ПАНЕЛЬ КНОПОК (Снизу)
            SetupBottomPanel();

            // 3. ТАБЛИЦА (По центру)
            SetupContentPanel();

            UpdateUITexts();
        }

        private void SetupHeader()
        {
            panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = clrHeaderBg,
                Padding = new Padding(20, 0, 20, 0)
            };
            panelMain.Controls.Add(panelHeader);

            picUserAvatar = new PictureBox
            {
                Size = new Size(60, 60),
                Location = new Point(30, 20),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            DrawUserAvatar();
            if (!IsGuestMode) picUserAvatar.Click += (s, e) => ShowProfile();
            panelHeader.Controls.Add(picUserAvatar);

            lblTitle = new Label
            {
                Location = new Point(110, 25),
                AutoSize = true,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White
            };
            panelHeader.Controls.Add(lblTitle);

            lblWelcome = new Label
            {
                Location = new Point(112, 60),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.LightGray
            };
            panelHeader.Controls.Add(lblWelcome);
        }

        private void SetupBottomPanel()
        {
            // Увеличил высоту панели, чтобы кнопки "дышали"
            panelButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 90,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            Panel separator = new Panel { Dock = DockStyle.Top, Height = 2, BackColor = Color.FromArgb(226, 232, 240) };
            panelButtons.Controls.Add(separator);

            flowButtonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false
            };

            // КНОПКИ УПРАВЛЕНИЯ

            // Обновить
            btnRefresh = CreateModernButton("↻ ОБНОВИТЬ", Color.White, clrTextMain, true);
            btnRefresh.Click += async (s, e) =>
            {
                btnRefresh.Enabled = false;
                btnRefresh.Text = "ЗАГРУЗКА...";
                await LoadScheduleFromServer();
                if (!IsGuestMode) await LoadUserBookingsFromServer();
                btnRefresh.Text = "↻ ОБНОВИТЬ";
                btnRefresh.Enabled = true;
            };
            flowButtonsPanel.Controls.Add(btnRefresh);

            if (!IsGuestMode)
            {
                // Забронировать (САМАЯ ЗАМЕТНАЯ КНОПКА)
                btnBooking = CreateModernButton("➕ ЗАБРОНИРОВАТЬ СЕАНС", clrPrimary, Color.White, false);
                btnBooking.Width = 240; // Делаем шире
                btnBooking.Click += (s, e) => ShowBookingForm();
                flowButtonsPanel.Controls.Add(btnBooking);

                // Кабинет
                btnProfile = CreateModernButton("👤 МОЙ КАБИНЕТ", clrHeaderBg, Color.White, false);
                btnProfile.Click += (s, e) => ShowProfile();
                flowButtonsPanel.Controls.Add(btnProfile);

                // Отменить
                btnCancelBooking = CreateModernButton("✕ ОТМЕНИТЬ ЗАПИСЬ", Color.White, clrDangerText, true);
                btnCancelBooking.Click += (s, e) => ShowCancelBookingDialog();
                flowButtonsPanel.Controls.Add(btnCancelBooking);
            }

            // Выход
            btnExit = CreateModernButton("🚪 ВЫХОД", Color.White, Color.Gray, true);
            btnExit.Click += (s, e) =>
            {
                if (MessageBox.Show("Выйти из системы?", "Выход", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    this.Close();
            };
            flowButtonsPanel.Controls.Add(btnExit);

            panelButtons.Controls.Add(flowButtonsPanel);
            panelMain.Controls.Add(panelButtons);
        }

        private void SetupContentPanel()
        {
            panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = clrBackground
            };
            panelMain.Controls.Add(panelContent);
            panelContent.BringToFront();

            Panel cardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = clrCard,
                Padding = new Padding(1)
            };

            Label lblGridHeader = new Label
            {
                Text = "РАСПИСАНИЕ СЕАНСОВ (ТЕКУЩАЯ НЕДЕЛЯ)",
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = clrTextMain,
                BackColor = Color.White,
                Padding = new Padding(15, 0, 0, 0)
            };
            cardPanel.Controls.Add(lblGridHeader);

            SetupScheduleGrid(cardPanel);
            panelContent.Controls.Add(cardPanel);
        }

        private void SetupScheduleGrid(Panel parent)
        {
            dgvSchedule = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 10),
                GridColor = Color.FromArgb(241, 245, 249),
                ColumnHeadersVisible = true, // ОБЯЗАТЕЛЬНО ВИДИМЫЕ ЗАГОЛОВКИ
                EnableHeadersVisualStyles = false
            };

            // СТИЛИЗАЦИЯ ЗАГОЛОВКОВ (ЧТОБЫ БЫЛО ВИДНО ОПИСАНИЕ)
            dgvSchedule.ColumnHeadersHeight = 50;
            dgvSchedule.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvSchedule.ColumnHeadersDefaultCellStyle.BackColor = clrGridHeaderBg; // Темный фон
            dgvSchedule.ColumnHeadersDefaultCellStyle.ForeColor = clrGridHeaderText; // Белый текст
            dgvSchedule.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvSchedule.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Стили ячеек
            dgvSchedule.RowTemplate.Height = 55;
            dgvSchedule.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);
            dgvSchedule.DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
            dgvSchedule.DefaultCellStyle.SelectionForeColor = clrTextMain;
            dgvSchedule.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // --- ОПРЕДЕЛЕНИЕ СТОЛБЦОВ (С ПОНЯТНЫМИ ОПИСАНИЯМИ) ---
            dgvSchedule.Columns.Add("Day", "ДЕНЬ НЕДЕЛИ");
            dgvSchedule.Columns.Add("Date", "ДАТА");
            dgvSchedule.Columns.Add("TimeSlot", "ВРЕМЯ СЕАНСА");
            dgvSchedule.Columns.Add("Capacity", "ВСЕГО МЕСТ"); // Понятное описание
            dgvSchedule.Columns.Add("AvailableSeats", "СВОБОДНО"); // Понятное описание
            dgvSchedule.Columns.Add("Status", "СТАТУС");

            if (!IsGuestMode)
            {
                var actionCol = dgvSchedule.Columns.Add("Action", "ДЕЙСТВИЕ");
            }

            dgvSchedule.Columns.Add("ScheduleId", "ID");
            dgvSchedule.Columns["ScheduleId"].Visible = false;

            // Настройка ширины
            dgvSchedule.Columns["Day"].FillWeight = 15;
            dgvSchedule.Columns["Date"].FillWeight = 12;
            dgvSchedule.Columns["TimeSlot"].FillWeight = 15;
            dgvSchedule.Columns["Capacity"].FillWeight = 10;
            dgvSchedule.Columns["AvailableSeats"].FillWeight = 10;
            dgvSchedule.Columns["Status"].FillWeight = 18;

            if (!IsGuestMode)
            {
                dgvSchedule.Columns["Action"].FillWeight = 15;
                dgvSchedule.CellClick += DgvSchedule_CellClick;
                dgvSchedule.CellMouseEnter += (s, e) =>
                {
                    if (e.ColumnIndex == dgvSchedule.Columns["Action"].Index && e.RowIndex >= 0)
                    {
                        string val = dgvSchedule.Rows[e.RowIndex].Cells["Action"].Value?.ToString();
                        if (!string.IsNullOrEmpty(val) && val != "-") dgvSchedule.Cursor = Cursors.Hand;
                    }
                };
                dgvSchedule.CellMouseLeave += (s, e) => dgvSchedule.Cursor = Cursors.Default;
            }

            dgvSchedule.CellPainting += DgvSchedule_CellPainting;
            parent.Controls.Add(dgvSchedule);
        }

        // --- ОТРИСОВКА ЯЧЕЕК ---
        private void DgvSchedule_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return; // Заголовки рисуем стандартно (они уже настроены выше)

            // 1. ГРУППИРОВКА ДАТ (Объединение ячеек визуально)
            if (dgvSchedule.Columns[e.ColumnIndex].Name == "Day" || dgvSchedule.Columns[e.ColumnIndex].Name == "Date")
            {
                e.AdvancedBorderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.None;
                bool isDuplicate = false;
                if (e.RowIndex > 0)
                {
                    var currentVal = e.Value?.ToString();
                    var prevVal = dgvSchedule.Rows[e.RowIndex - 1].Cells[e.ColumnIndex].Value?.ToString();
                    if (currentVal == prevVal) isDuplicate = true;
                }

                e.PaintBackground(e.CellBounds, true);

                if (!isDuplicate)
                {
                    using (Brush brush = new SolidBrush(clrTextMain))
                    {
                        Font f = (dgvSchedule.Columns[e.ColumnIndex].Name == "Day")
                            ? new Font("Segoe UI", 10, FontStyle.Bold)
                            : new Font("Segoe UI", 10);

                        e.Graphics.DrawString(e.Value?.ToString(), f, brush, e.CellBounds,
                            new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center });
                    }
                    // Линия-разделитель дней
                    if (e.RowIndex > 0)
                    {
                        using (Pen p = new Pen(Color.FromArgb(203, 213, 225)))
                            e.Graphics.DrawLine(p, e.CellBounds.Left, e.CellBounds.Top, e.CellBounds.Right, e.CellBounds.Top);
                    }
                }
                e.Handled = true;
                return;
            }

            // 2. СТАТУС (Бейджи)
            if (dgvSchedule.Columns[e.ColumnIndex].Name == "Status")
            {
                e.PaintBackground(e.CellBounds, true);
                string status = e.Value?.ToString() ?? "";
                Color bg = clrInfoBg;
                Color txt = clrInfoText;

                if (status.Contains("ЗАБРОНИРОВАНО") || status.Contains("ВАШЕ"))
                {
                    bg = clrInfoBg; txt = clrInfoText;
                    status = "Вы записаны";
                }
                else if (status.Contains("НЕТ МЕСТ"))
                {
                    bg = clrDangerBg; txt = clrDangerText;
                    status = "Нет мест";
                }
                else
                {
                    bg = clrSuccessBg; txt = clrSuccessText;
                    status = "Доступно";
                }

                var rect = new Rectangle(e.CellBounds.X + (e.CellBounds.Width - 100) / 2, e.CellBounds.Y + 12, 100, 30);
                DrawRoundedBox(e.Graphics, rect, bg, 15);
                TextRenderer.DrawText(e.Graphics, status, new Font("Segoe UI", 9, FontStyle.Bold),
                    rect, txt, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                e.Handled = true;
                return;
            }

            // 3. ДЕЙСТВИЕ (Яркие кнопки)
            if (!IsGuestMode && dgvSchedule.Columns[e.ColumnIndex].Name == "Action")
            {
                e.PaintBackground(e.CellBounds, true);
                string action = e.Value?.ToString();

                if (!string.IsNullOrEmpty(action) && action != "-")
                {
                    Color btnColor = (action == "ЗАБРОНИРОВАТЬ") ? clrPrimary : clrDangerText;
                    string btnText = (action == "ЗАБРОНИРОВАТЬ") ? "Записаться" : "Отменить";

                    var btnRect = new Rectangle(e.CellBounds.X + (e.CellBounds.Width - 110) / 2, e.CellBounds.Y + 10, 110, 35);
                    DrawRoundedBox(e.Graphics, btnRect, btnColor, 8); // Скругленная кнопка

                    TextRenderer.DrawText(e.Graphics, btnText, new Font("Segoe UI", 9, FontStyle.Bold),
                        btnRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                e.Handled = true;
                return;
            }

            e.Paint(e.CellBounds, DataGridViewPaintParts.All);
            e.Handled = true;
        }

        private void DrawRoundedBox(Graphics g, Rectangle r, Color c, int radius)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = new GraphicsPath())
            using (Brush brush = new SolidBrush(c))
            {
                path.AddArc(r.X, r.Y, radius, radius, 180, 90);
                path.AddArc(r.Right - radius, r.Y, radius, radius, 270, 90);
                path.AddArc(r.Right - radius, r.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(r.X, r.Bottom - radius, radius, radius, 90, 90);
                path.CloseAllFigures();
                g.FillPath(brush, path);
            }
        }

        private Button CreateModernButton(string text, Color backColor, Color foreColor, bool isOutline)
        {
            Button btn = new Button
            {
                Text = text,
                Size = new Size(200, 50), // Увеличил размер для видимости
                BackColor = isOutline ? Color.White : backColor,
                ForeColor = foreColor,
                Font = new Font("Segoe UI", 11, FontStyle.Bold), // Увеличил шрифт
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(10, 15, 0, 0)
            };
            btn.FlatAppearance.BorderSize = isOutline ? 1 : 0;
            btn.FlatAppearance.BorderColor = isOutline ? Color.LightGray : backColor;

            btn.MouseEnter += (s, e) =>
            {
                if (isOutline) btn.BackColor = Color.WhiteSmoke;
                else btn.BackColor = ControlPaint.Light(backColor);
            };
            btn.MouseLeave += (s, e) =>
            {
                if (isOutline) btn.BackColor = Color.White;
                else btn.BackColor = backColor;
            };

            return btn;
        }

        private void DrawUserAvatar()
        {
            Bitmap bmp = new Bitmap(picUserAvatar.Width, picUserAvatar.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 0, bmp.Width, bmp.Height),
                    IsGuestMode ? Color.LightGray : Color.FromArgb(99, 102, 241),
                    IsGuestMode ? Color.Gray : Color.FromArgb(67, 56, 202), 45F))
                {
                    g.FillEllipse(brush, 0, 0, bmp.Width - 1, bmp.Height - 1);
                }

                string icon = "👤";
                using (Font iconFont = new Font("Segoe UI Emoji", 26))
                using (SolidBrush textBrush = new SolidBrush(Color.White))
                {
                    SizeF size = g.MeasureString(icon, iconFont);
                    g.DrawString(icon, iconFont, textBrush, (bmp.Width - size.Width) / 2, (bmp.Height - size.Height) / 2 + 2);
                }
            }
            picUserAvatar.Image = bmp;
        }

        private void CenterButtons()
        {
            if (flowButtonsPanel == null || flowButtonsPanel.Controls.Count == 0) return;
            int totalWidth = 0;
            foreach (Control c in flowButtonsPanel.Controls) totalWidth += c.Width + c.Margin.Left + c.Margin.Right;
            int p = (flowButtonsPanel.ClientSize.Width - totalWidth) / 2;
            flowButtonsPanel.Padding = new Padding(Math.Max(0, p), 10, 0, 0);
        }

        private void UpdateUITexts()
        {
            if (IsGuestMode)
            {
                lblTitle.Text = "Гостевой доступ";
                lblWelcome.Text = "Вы просматриваете расписание в режиме чтения";
            }
            else
            {
                lblTitle.Text = "Личный кабинет";
                lblWelcome.Text = $"Добро пожаловать, {currentUser}!";
            }
        }

        // --- ЛОГИКА СЕРВЕРА ---
        private async Task LoadScheduleFromServer()
        {
            lock (loadingLock) { if (isLoading) return; isLoading = true; }
            try
            {
                if (dgvSchedule.InvokeRequired) { dgvSchedule.Invoke(new Action(async () => await LoadScheduleFromServer())); return; }

                dgvSchedule.Rows.Clear();
                DateTime currentDateTime = DateTime.Now;
                DateTime startDate = currentDateTime.Date;
                DateTime endDate = startDate.AddDays(7);

                try
                {
                    var scheduleData = await SendServerRequest(new { Command = "get_schedule" });
                    if (scheduleData.ValueKind == JsonValueKind.Object &&
                        scheduleData.TryGetProperty("Success", out var successElement) && successElement.GetBoolean())
                    {
                        if (scheduleData.TryGetProperty("Schedule", out var scheduleArray) && scheduleArray.ValueKind == JsonValueKind.Array)
                        {
                            var sortedSchedule = scheduleArray.EnumerateArray()
                                .Select(item => new { Item = item, Date = DateTime.Parse(item.GetProperty("Date").GetString()) })
                                .OrderBy(x => x.Date).ToList();

                            foreach (var data in sortedSchedule)
                            {
                                var item = data.Item;
                                try
                                {
                                    int scheduleId = item.GetProperty("Id").GetInt32();
                                    DateTime slotDate = DateTime.Parse(item.GetProperty("Date").GetString());
                                    string timeSlot = item.GetProperty("TimeSlot").GetString();

                                    if (slotDate < startDate) continue;
                                    if (slotDate > endDate) continue;
                                    if (slotDate.Date == startDate)
                                    {
                                        string endTimeStr = timeSlot.Split('-')[1].Trim();
                                        if (TimeSpan.TryParse(endTimeStr, out TimeSpan endTime))
                                            if (currentDateTime.TimeOfDay > endTime) continue;
                                    }

                                    string dayName = item.TryGetProperty("DayOfWeek", out var dayElement) ? dayElement.GetString() : slotDate.DayOfWeek.ToString();
                                    string dateStr = slotDate.ToString("dd.MM.yyyy");
                                    int capacity = item.TryGetProperty("Capacity", out var capacityElement) ? capacityElement.GetInt32() : 50;
                                    int availableSeats = item.TryGetProperty("AvailableSeats", out var seatsElement) ? seatsElement.GetInt32() : capacity;
                                    string statusFromDB = item.TryGetProperty("Status", out var statusElement) ? statusElement.GetString() : "ДОСТУПНО";

                                    bool isBooked = !IsGuestMode && UserBookings.Any(b => b.ScheduleId == scheduleId);
                                    string statusDisplay;
                                    string actionDisplay = "ЗАБРОНИРОВАТЬ";

                                    if (isBooked) { statusDisplay = "ВАШЕ БРОНИРОВАНИЕ"; actionDisplay = "ОТМЕНИТЬ"; }
                                    else if (availableSeats <= 0 || statusFromDB != "ДОСТУПНО") { statusDisplay = "НЕТ МЕСТ"; actionDisplay = "-"; }
                                    else { statusDisplay = "ДОСТУПНО"; }

                                    if (!IsGuestMode)
                                        dgvSchedule.Rows.Add(dayName.ToUpper(), dateStr, timeSlot, capacity, availableSeats, statusDisplay, actionDisplay, scheduleId);
                                    else
                                        dgvSchedule.Rows.Add(dayName.ToUpper(), dateStr, timeSlot, capacity, availableSeats, statusDisplay, scheduleId);
                                }
                                catch { continue; }
                            }
                        }
                    }
                }
                catch { GenerateFallbackSchedule(currentDateTime); }
            }
            finally { lock (loadingLock) { isLoading = false; } dgvSchedule.ClearSelection(); }
        }

        private void GenerateFallbackSchedule(DateTime currentDateTime)
        {
            string[] daysOfWeek = { "ПОНЕДЕЛЬНИК", "ВТОРНИК", "СРЕДА", "ЧЕТВЕРГ", "ПЯТНИЦА", "СУББОТА", "ВОСКРЕСЕНЬЕ" };
            string[] timeSlots = { "10:00-10:45", "12:00-12:45", "14:00-14:45", "16:00-16:45", "18:00-18:45", "20:00-20:45" };
            Random rnd = new Random();

            for (int i = 0; i < 7; i++)
            {
                DateTime date = currentDateTime.Date.AddDays(i);
                string dayName = daysOfWeek[(int)date.DayOfWeek == 0 ? 6 : (int)date.DayOfWeek - 1];
                string dateStr = date.ToString("dd.MM.yyyy");

                foreach (string timeSlot in timeSlots)
                {
                    int capacity = 50;
                    int availableSeats = rnd.Next(0, 51);
                    string status = availableSeats > 0 ? "ДОСТУПНО" : "НЕТ МЕСТ";
                    string action = "ЗАБРОНИРОВАТЬ";

                    if (!IsGuestMode)
                        dgvSchedule.Rows.Add(dayName, dateStr, timeSlot, capacity, availableSeats, status, action, i * 1000);
                    else
                        dgvSchedule.Rows.Add(dayName, dateStr, timeSlot, capacity, availableSeats, status, i * 1000);
                }
            }
        }

        public async Task<List<Booking>> LoadUserBookingsFromServer()
        {
            try
            {
                var response = await SendServerRequest(new { Command = "get_user_bookings", UserId = currentUserId });
                if (response.TryGetProperty("Success", out var s) && s.GetBoolean() && response.TryGetProperty("Bookings", out var bookingsArray))
                {
                    var bookings = new List<Booking>();
                    foreach (var item in bookingsArray.EnumerateArray())
                    {
                        try
                        {
                            var booking = new Booking
                            {
                                Id = item.GetProperty("BookingId").GetInt32(),
                                Date = DateTime.Parse(item.GetProperty("Date").GetString()),
                                TimeSlot = item.GetProperty("TimeSlot").GetString(),
                                Status = item.TryGetProperty("Status", out var st) ? st.GetString() : "Booked",
                                ScheduleId = item.TryGetProperty("ScheduleId", out var sid) ? sid.GetInt32() : 0
                            };
                            bookings.Add(booking);
                        }
                        catch { }
                    }
                    return bookings;
                }
            }
            catch { }
            return new List<Booking>();
        }

        private async void DgvSchedule_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (IsGuestMode) { MessageBox.Show("Войдите в систему для бронирования.", "Гость", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (e.RowIndex < 0 || e.ColumnIndex != dgvSchedule.Columns["Action"].Index) return;

            string val = dgvSchedule.Rows[e.RowIndex].Cells["Action"].Value?.ToString();
            if (string.IsNullOrEmpty(val) || val == "-") return;

            lock (loadingLock) { if (isLoading) return; isLoading = true; }
            try
            {
                string day = dgvSchedule.Rows[e.RowIndex].Cells["Day"].Value?.ToString();
                string date = dgvSchedule.Rows[e.RowIndex].Cells["Date"].Value?.ToString();
                if (string.IsNullOrEmpty(date))
                {
                    for (int i = e.RowIndex; i >= 0; i--)
                    {
                        string t = dgvSchedule.Rows[i].Cells["Date"].Value?.ToString();
                        if (!string.IsNullOrEmpty(t)) { date = t; break; }
                    }
                }
                if (string.IsNullOrEmpty(day))
                {
                    for (int i = e.RowIndex; i >= 0; i--)
                    {
                        string t = dgvSchedule.Rows[i].Cells["Day"].Value?.ToString();
                        if (!string.IsNullOrEmpty(t)) { day = t; break; }
                    }
                }

                string time = dgvSchedule.Rows[e.RowIndex].Cells["TimeSlot"].Value?.ToString();
                string action = dgvSchedule.Rows[e.RowIndex].Cells["Action"].Value?.ToString();
                int scheduleId = Convert.ToInt32(dgvSchedule.Rows[e.RowIndex].Cells["ScheduleId"].Value);

                if (action == "ЗАБРОНИРОВАТЬ") await ShowBookingFormAsync(day, date, time, scheduleId);
                else if (action == "ОТМЕНИТЬ") await CancelBookingAsync(day, date, time, scheduleId);
            }
            finally { lock (loadingLock) { isLoading = false; } }
        }

        private async Task ShowBookingFormAsync(string day, string date, string time, int scheduleId)
        {
            if (IsGuestMode) return;
            DateTime slotDate;
            if (!DateTime.TryParse(date, out slotDate)) return;

            string endTimeStr = time.Split('-')[1].Trim();
            if (TimeSpan.TryParse(endTimeStr, out TimeSpan endTime))
            {
                if (slotDate.Date == DateTime.Now.Date && DateTime.Now.TimeOfDay > endTime)
                {
                    MessageBox.Show("Время сеанса вышло.", "Упс", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    await LoadScheduleFromServer();
                    return;
                }
            }

            int available = await GetAvailableSeatsForSchedule(scheduleId);
            if (available <= 0) { MessageBox.Show("Мест нет.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            var dbService = new DatabaseService(this);
            using (var bookingForm = new BookingForm(day, date, time, this, currentUserId, dbService, scheduleId, available))
            {
                if (bookingForm.ShowDialog() == DialogResult.OK)
                {
                    UserBookings = await LoadUserBookingsFromServer();
                    await LoadScheduleFromServer();
                }
            }
        }

        private async Task CancelBookingAsync(string day, string date, string time, int scheduleId)
        {
            var booking = UserBookings.FirstOrDefault(b => b.ScheduleId == scheduleId);
            if (booking == null) return;

            if (MessageBox.Show("Отменить бронирование?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var response = await SendServerRequest(new { Command = "cancel_booking", BookingId = booking.Id, ScheduleId = scheduleId, UserId = currentUserId });
                if (response.TryGetProperty("Success", out var s) && s.GetBoolean())
                {
                    UserBookings.RemoveAll(b => b.Id == booking.Id);
                    await LoadScheduleFromServer();
                    MessageBox.Show("Бронирование отменено.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private async Task<int> GetAvailableSeatsForSchedule(int scheduleId)
        {
            try
            {
                var response = await SendServerRequest(new { Command = "get_schedule" });
                if (response.ValueKind == JsonValueKind.Object && response.TryGetProperty("Schedule", out var arr))
                {
                    foreach (var item in arr.EnumerateArray())
                        if (item.GetProperty("Id").GetInt32() == scheduleId) return item.GetProperty("AvailableSeats").GetInt32();
                }
            }
            catch { }
            return 50;
        }

        private void ShowBookingForm() => MessageBox.Show("Для бронирования выберите доступный слот в таблице и нажмите кнопку 'Записаться'.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
        private void ShowCancelBookingDialog() => MessageBox.Show("Для отмены нажмите кнопку 'Отменить' напротив вашей записи в таблице.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);

        private void ShowProfile()
        {
            if (IsGuestMode) return;
            using (var profileForm = new ProfileForm(currentUser, currentUserId, UserReviews, this)) profileForm.ShowDialog();
        }

        public async Task<JsonElement> SendServerRequest(object request)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync("127.0.0.1", 8888);
                    using (var stream = client.GetStream())
                    {
                        byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
                        await stream.WriteAsync(data, 0, data.Length);
                        byte[] buffer = new byte[8192];
                        using (var ms = new MemoryStream())
                        {
                            int bytes;
                            do { bytes = await stream.ReadAsync(buffer, 0, buffer.Length); ms.Write(buffer, 0, bytes); } while (stream.DataAvailable);
                            string json = Encoding.UTF8.GetString(ms.ToArray()).Trim();
                            if (string.IsNullOrEmpty(json)) throw new Exception();
                            return JsonSerializer.Deserialize<JsonElement>(json);
                        }
                    }
                }
            }
            catch { return JsonSerializer.Deserialize<JsonElement>("{}"); }
        }
    }
}