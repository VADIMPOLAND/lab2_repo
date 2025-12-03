using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Text.Json;
using System.Threading.Tasks;

namespace IceArena.Client
{
    public partial class ProfileForm : Form
    {
        private string currentUser;
        private int userId;
        private List<Booking> userBookings;
        private List<Review> userReviews;
        private ClientForm parentForm;

        private DataGridView dgvBookings;
        private Label lblTotalCost;
        private Label lblStats;
        private Panel mainContainer;
        private Panel sidebarPanel;
        private Panel reviewsPanel;
        private FlowLayoutPanel reviewsFlowPanel;

        // Цветовая палитра
        private Color primaryColor = Color.FromArgb(41, 128, 185);
        private Color secondaryColor = Color.FromArgb(52, 152, 219);
        private Color accentColor = Color.FromArgb(46, 204, 113);
        private Color dangerColor = Color.FromArgb(231, 76, 60);
        private Color warningColor = Color.FromArgb(241, 196, 15);
        private Color backgroundColor = Color.FromArgb(245, 245, 245);
        private Color textColor = Color.FromArgb(51, 51, 51);

        public ProfileForm(string username, int userId, List<Review> reviews, ClientForm parent)
        {
            currentUser = username;
            this.userId = userId;
            this.parentForm = parent;
            this.userReviews = reviews ?? new List<Review>();
            this.userBookings = new List<Booking>();

            InitializeComponents();
            SetupUI();
            // Загружаем данные
            _ = LoadUserDataAsync();
        }

        private void InitializeComponents()
        {
            this.SuspendLayout();
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1400, 900);
            this.MinimumSize = new Size(1200, 800);
            this.Name = "ProfileForm";
            this.Text = "👤 ЛИЧНЫЙ КАБИНЕТ - Ice Arena";
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = backgroundColor;
            this.Font = new Font("Segoe UI", 10);
            this.Padding = new Padding(10);
            this.ResumeLayout(false);
        }

        private async Task LoadUserDataAsync()
        {
            try
            {
                // ИСПРАВЛЕНИЕ: Используем команду get_user_bookings_with_tickets для получения билетов и цен
                var bookingsResponse = await parentForm.SendServerRequest(new
                {
                    Command = "get_user_bookings_with_tickets",
                    UserId = userId
                });

                userBookings = new List<Booking>();
                if (bookingsResponse.TryGetProperty("Success", out var success) && success.GetBoolean())
                {
                    if (bookingsResponse.TryGetProperty("Bookings", out var bookingsElem) && bookingsElem.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var bookingElem in bookingsElem.EnumerateArray())
                        {
                            var booking = new Booking
                            {
                                Id = bookingElem.TryGetProperty("BookingId", out var idElem) ? idElem.GetInt32() : 0,
                                Status = bookingElem.TryGetProperty("Status", out var statusElem) ? statusElem.GetString() : "",
                                BookingDate = bookingElem.TryGetProperty("BookingDate", out var bDateElem) ? DateTime.Parse(bDateElem.GetString()) : DateTime.Now,
                                Date = bookingElem.TryGetProperty("Date", out var dateElem) ? DateTime.Parse(dateElem.GetString()) : DateTime.Now,
                                TimeSlot = bookingElem.TryGetProperty("TimeSlot", out var timeElem) ? timeElem.GetString() : "",
                                Day = bookingElem.TryGetProperty("DayOfWeek", out var dayElem) ? dayElem.GetString() : ""
                            };

                            // Парсинг билетов
                            if (bookingElem.TryGetProperty("Tickets", out var ticketsElem) && ticketsElem.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var ticketElem in ticketsElem.EnumerateArray())
                                {
                                    booking.Tickets.Add(new Ticket
                                    {
                                        Id = ticketElem.TryGetProperty("Id", out var tIdElem) ? tIdElem.GetInt32() : 0,
                                        BookingId = booking.Id,
                                        Type = ticketElem.TryGetProperty("Type", out var typeElem) ? typeElem.GetString() : "",
                                        Quantity = ticketElem.TryGetProperty("Quantity", out var qtyElem) ? qtyElem.GetInt32() : 0,
                                        Price = ticketElem.TryGetProperty("Price", out var priceElem) ? priceElem.GetDecimal() : 0m
                                    });
                                }
                            }
                            userBookings.Add(booking);
                        }
                    }
                }

                await LoadUserReviewsAsync();

                // Обновляем UI в основном потоке
                this.Invoke((MethodInvoker)delegate {
                    UpdateBookingsGrid();
                    UpdateTotalCost();
                    UpdateStatistics();
                    UpdateReviewsList();
                    UpdateDeleteButtonState();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadUserReviewsAsync()
        {
            try
            {
                var response = await parentForm.SendServerRequest(new
                {
                    Command = "get_user_reviews",
                    UserId = userId
                });

                if (response.TryGetProperty("Success", out var successElement) && successElement.GetBoolean())
                {
                    if (response.TryGetProperty("Reviews", out var reviewsElement) &&
                        reviewsElement.ValueKind == JsonValueKind.Array)
                    {
                        var newReviews = new List<Review>();
                        foreach (var reviewElement in reviewsElement.EnumerateArray())
                        {
                            try
                            {
                                var review = new Review
                                {
                                    Id = reviewElement.TryGetProperty("Id", out var idElement) ? idElement.GetInt32() : 0,
                                    UserId = reviewElement.TryGetProperty("UserId", out var userIdElement) ? userIdElement.GetInt32() : userId,
                                    Text = reviewElement.TryGetProperty("Text", out var textElement) ? textElement.GetString() ?? "" : "",
                                    Rating = reviewElement.TryGetProperty("Rating", out var ratingElement) ? ratingElement.GetInt32() : 5,
                                    Date = reviewElement.TryGetProperty("Date", out var dateElement) ? DateTime.Parse(dateElement.GetString()) : DateTime.Now
                                };

                                if (!string.IsNullOrEmpty(review.Text) && review.UserId == userId)
                                {
                                    newReviews.Add(review);
                                }
                            }
                            catch { }
                        }
                        userReviews = newReviews;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки отзывов: {ex.Message}");
            }
        }

        private void SetupUI()
        {
            mainContainer = new Panel
            {
                Location = new Point(0, 0),
                Size = this.Size,
                BackColor = backgroundColor,
                AutoScroll = true
            };
            this.Controls.Add(mainContainer);

            CreateSidebar();
            CreateHeader();
            CreateBookingsSection();
            CreateReviewsSection();
        }

        private void CreateSidebar()
        {
            sidebarPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(280, mainContainer.Height),
                BackColor = primaryColor,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };
            mainContainer.Controls.Add(sidebarPanel);

            // Аватар
            PictureBox profileAvatar = new PictureBox
            {
                Location = new Point(65, 40),
                Size = new Size(150, 150),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            DrawProfileAvatar(profileAvatar);
            sidebarPanel.Controls.Add(profileAvatar);

            // Имя
            Label lblUserName = new Label
            {
                Text = currentUser.ToUpper(),
                Location = new Point(10, 210),
                Size = new Size(260, 30),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };
            sidebarPanel.Controls.Add(lblUserName);

            Label lblUserId = new Label
            {
                Text = $"ID: {userId}",
                Location = new Point(10, 245),
                Size = new Size(260, 25),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };
            sidebarPanel.Controls.Add(lblUserId);

            // Статистика
            Panel statsPanel = new Panel
            {
                Location = new Point(15, 290),
                Size = new Size(250, 120),
                BackColor = Color.FromArgb(60, 255, 255, 255),
                BorderStyle = BorderStyle.None
            };
            Label lblStatsTitle = new Label
            {
                Text = "📊 СТАТИСТИКА",
                Location = new Point(0, 10),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };
            statsPanel.Controls.Add(lblStatsTitle);

            lblStats = new Label
            {
                Text = "Загрузка...",
                Location = new Point(0, 40),
                Size = new Size(250, 70),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.TopCenter
            };
            statsPanel.Controls.Add(lblStats);
            sidebarPanel.Controls.Add(statsPanel);

            // КНОПКА ТЕХ. ПОДДЕРЖКИ
            Button btnSupport = new Button
            {
                Text = "🛠 ТЕХ. ПОДДЕРЖКА",
                Location = new Point(15, 430),
                Size = new Size(250, 50),
                BackColor = accentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSupport.FlatAppearance.BorderSize = 0;
            btnSupport.Click += (s, e) => OpenSupportForm();
            sidebarPanel.Controls.Add(btnSupport);
        }

        private void OpenSupportForm()
        {
            var supportForm = new SupportForm(userId, currentUser, parentForm);
            supportForm.ShowDialog();
        }

        private void CreateHeader()
        {
            Panel headerPanel = new Panel
            {
                Location = new Point(290, 10),
                Size = new Size(mainContainer.Width - 310, 80),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            mainContainer.Controls.Add(headerPanel);

            lblTotalCost = new Label
            {
                Text = "💰 ОБЩАЯ СУММА БРОНИРОВАНИЙ: 0.00 BYN",
                Location = new Point(20, 20),
                Size = new Size(600, 40),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = primaryColor,
                TextAlign = ContentAlignment.MiddleLeft
            };
            headerPanel.Controls.Add(lblTotalCost);

            Button btnRefresh = new Button
            {
                Text = "🔄 ОБНОВИТЬ",
                Location = new Point(headerPanel.Width - 130, 20),
                Size = new Size(110, 40),
                BackColor = secondaryColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += async (s, e) => await LoadUserDataAsync();
            headerPanel.Controls.Add(btnRefresh);
        }

        private void CreateBookingsSection()
        {
            Panel bookingsPanel = new Panel
            {
                Location = new Point(290, 100),
                Size = new Size(mainContainer.Width - 310, 400),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            mainContainer.Controls.Add(bookingsPanel);

            Label lblBookingsTitle = new Label
            {
                Text = "🎫 ВСЕ БРОНИРОВАНИЯ",
                Location = new Point(20, 15),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = primaryColor,
                TextAlign = ContentAlignment.MiddleLeft
            };
            bookingsPanel.Controls.Add(lblBookingsTitle);

            dgvBookings = new DataGridView
            {
                Location = new Point(20, 50),
                Size = new Size(bookingsPanel.Width - 40, 280),
                Font = new Font("Segoe UI", 9),
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BorderStyle = BorderStyle.Fixed3D,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 35 },
                ScrollBars = ScrollBars.Both,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            dgvBookings.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = primaryColor,
                ForeColor = Color.White,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            dgvBookings.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = textColor,
                SelectionBackColor = Color.FromArgb(200, primaryColor),
                SelectionForeColor = Color.White,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Padding = new Padding(5)
            };
            dgvBookings.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(250, 250, 250)
            };

            dgvBookings.Columns.Clear();
            dgvBookings.Columns.Add(new DataGridViewTextBoxColumn { Name = "BookingId", HeaderText = "ID", FillWeight = 30 });
            dgvBookings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "ДАТА", FillWeight = 50 });
            dgvBookings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "ВРЕМЯ", FillWeight = 50 });
            dgvBookings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "ТИП БИЛЕТА", FillWeight = 60 });
            dgvBookings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "КОЛ-ВО", FillWeight = 30 });
            dgvBookings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Price", HeaderText = "ЦЕНА (ЗА ШТ.)", FillWeight = 50 });
            dgvBookings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "СТАТУС", FillWeight = 50 });

            bookingsPanel.Controls.Add(dgvBookings);

            Panel controlsPanel = new Panel
            {
                Location = new Point(20, 340),
                Size = new Size(bookingsPanel.Width - 40, 50),
                BackColor = Color.White,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            Button btnRemoveBooking = new Button
            {
                Text = "🗑️ УДАЛИТЬ ВЫБРАННОЕ",
                Location = new Point(0, 0),
                Size = new Size(350, 40),
                BackColor = dangerColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Enabled = false,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Name = "btnRemoveBooking"
            };
            btnRemoveBooking.FlatAppearance.BorderSize = 0;
            btnRemoveBooking.Click += BtnRemoveBooking_Click;
            controlsPanel.Controls.Add(btnRemoveBooking);

            bookingsPanel.Controls.Add(controlsPanel);
            dgvBookings.SelectionChanged += (s, e) => UpdateDeleteButtonState();
        }

        private void CreateReviewsSection()
        {
            reviewsPanel = new Panel
            {
                Location = new Point(290, 520),
                Size = new Size(mainContainer.Width - 310, 350),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoScroll = true
            };
            mainContainer.Controls.Add(reviewsPanel);

            Label lblReviewsTitle = new Label
            {
                Text = "💬 МОИ ОТЗЫВЫ",
                Location = new Point(20, 15),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = primaryColor,
                TextAlign = ContentAlignment.MiddleLeft
            };
            reviewsPanel.Controls.Add(lblReviewsTitle);

            Panel newReviewPanel = new Panel
            {
                Location = new Point(20, 50),
                Size = new Size(reviewsPanel.Width - 40, 80),
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            TextBox txtNewReview = new TextBox
            {
                Location = new Point(10, 10),
                Size = new Size(newReviewPanel.Width - 220, 60),
                Multiline = true,
                PlaceholderText = "Напишите ваш отзыв...",
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Name = "txtNewReview",
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            newReviewPanel.Controls.Add(txtNewReview);

            ComboBox cmbRating = new ComboBox
            {
                Location = new Point(newReviewPanel.Width - 200, 10),
                Size = new Size(120, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.White,
                Name = "cmbRating",
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            cmbRating.Items.AddRange(new object[] { "⭐", "⭐⭐", "⭐⭐⭐", "⭐⭐⭐⭐", "⭐⭐⭐⭐⭐" });
            cmbRating.SelectedIndex = 4;
            newReviewPanel.Controls.Add(cmbRating);

            Button btnAddReview = new Button
            {
                Text = "📝 ДОБАВИТЬ",
                Location = new Point(newReviewPanel.Width - 70, 45),
                Size = new Size(60, 25),
                BackColor = accentColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Name = "btnAddReview",
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnAddReview.FlatAppearance.BorderSize = 0;
            btnAddReview.Click += BtnAddReview_Click;
            newReviewPanel.Controls.Add(btnAddReview);

            reviewsPanel.Controls.Add(newReviewPanel);

            reviewsFlowPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 140),
                Size = new Size(reviewsPanel.Width - 40, 190),
                AutoScroll = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            reviewsPanel.Controls.Add(reviewsFlowPanel);
        }

        private void DrawProfileAvatar(PictureBox pictureBox)
        {
            Bitmap bmp = new Bitmap(pictureBox.Width, pictureBox.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                LinearGradientBrush brush = new LinearGradientBrush(
                    new Rectangle(0, 0, bmp.Width, bmp.Height),
                    Color.FromArgb(86, 204, 242),
                    Color.FromArgb(47, 128, 237),
                    45F
                );
                g.FillEllipse(brush, 0, 0, bmp.Width, bmp.Height);

                using (Font iconFont = new Font("Segoe UI", 50, FontStyle.Bold))
                using (SolidBrush textBrush = new SolidBrush(Color.White))
                {
                    g.DrawString("👤", iconFont, textBrush, 25, 20);
                }

                using (Pen pen = new Pen(Color.White, 3))
                {
                    g.DrawEllipse(pen, 1, 1, bmp.Width - 3, bmp.Height - 3);
                }
            }
            pictureBox.Image = bmp;
        }

        private void UpdateBookingsGrid()
        {
            dgvBookings.Rows.Clear();
            foreach (var booking in userBookings.OrderByDescending(b => b.Date))
            {
                // ИСПРАВЛЕНИЕ: Гарантируем корректное отображение списка билетов
                string ticketTypes = booking.Tickets.Count > 0
                    ? string.Join(", ", booking.Tickets.Select(t => $"{GetTicketTypeDisplayName(t.Type)} x{t.Quantity}"))
                    : "Нет данных";

                // ИСПРАВЛЕНИЕ: Считаем сумму вручную, если в классе Booking это не реализовано
                decimal bookingTotalCost = booking.Tickets.Sum(t => t.Price * t.Quantity);
                int totalQty = booking.Tickets.Sum(t => t.Quantity);

                string totalPrice = $"{bookingTotalCost:F2} BYN";
                string statusDisplay = IsBookingPassed(booking) ? "○ ПРОШЕЛ" : "✓ АКТИВНО";

                int rowIndex = dgvBookings.Rows.Add(
                    booking.Id,
                    booking.Date.ToString("dd.MM.yyyy"),
                    booking.TimeSlot,
                    ticketTypes,
                    totalQty,
                    totalPrice,
                    statusDisplay
                );

                if (IsBookingPassed(booking))
                {
                    dgvBookings.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230);
                }
            }
        }

        private string FormatTimeSlot(string timeSlot)
        {
            if (string.IsNullOrEmpty(timeSlot)) return "Не указано";
            try
            {
                if (timeSlot.Contains("-"))
                {
                    var parts = timeSlot.Split('-');
                    if (parts.Length == 2)
                    {
                        string start = FormatTime(parts[0].Trim());
                        string end = FormatTime(parts[1].Trim());
                        return $"{start}-{end}";
                    }
                }
                return FormatTime(timeSlot);
            }
            catch
            {
                return timeSlot;
            }
        }

        private string FormatTime(string time)
        {
            if (string.IsNullOrEmpty(time)) return "Не указано";
            time = time.Replace(" ", "");
            if (time.Length == 4 && int.TryParse(time, out _))
            {
                return $"{time.Substring(0, 2)}:{time.Substring(2, 2)}";
            }
            return time;
        }

        private void UpdateTotalCost()
        {
            // ИСПРАВЛЕНИЕ: Явный подсчет полной суммы на основе билетов
            decimal total = userBookings.Sum(b => b.Tickets.Sum(t => t.Price * t.Quantity));
            lblTotalCost.Text = $"💰 ОБЩАЯ СУММА БРОНИРОВАНИЙ: {total:F2} BYN";
        }

        private void UpdateStatistics()
        {
            if (userBookings != null)
            {
                int activeBookings = userBookings.Count(b => !IsBookingTimePassed(b.Date, b.TimeSlot));
                int totalBookings = userBookings.Count;
                int totalReviews = userReviews?.Count(r => r.UserId == userId) ?? 0;

                string statsText = $@"🎫 Активных бронирований: {activeBookings} из {totalBookings}
⭐ Всего отзывов: {totalReviews}
📅 Последнее бронирование: {(userBookings.Any() ? userBookings.Max(b => b.Date).ToString("dd.MM.yyyy") : "нет")}";
                lblStats.Text = statsText;
            }
            else
            {
                lblStats.Text = "Загрузка статистики...";
            }
        }

        private void UpdateDeleteButtonState()
        {
            if (dgvBookings == null) return;
            var btnRemove = mainContainer.Controls.OfType<Panel>()
                .First(p => p.Controls.OfType<DataGridView>().Any())
                .Controls.OfType<Panel>()
                .Last()
                .Controls.OfType<Button>()
                .FirstOrDefault(b => b.Name == "btnRemoveBooking");

            if (btnRemove == null) return;

            bool hasSelection = dgvBookings.SelectedRows.Count > 0;
            if (hasSelection)
            {
                var selectedRow = dgvBookings.SelectedRows[0];
                bool isExpired = selectedRow.Cells["Status"].Value?.ToString() == "○ ПРОШЕЛ";
                btnRemove.Enabled = !isExpired;
                btnRemove.BackColor = isExpired ? Color.Gray : dangerColor;
            }
            else
            {
                btnRemove.Enabled = false;
                btnRemove.BackColor = Color.Gray;
            }
        }

        private bool IsBookingTimePassed(DateTime sessionDate, string timeSlot)
        {
            try
            {
                if (sessionDate.Date < DateTime.Today) return true;
                if (sessionDate.Date == DateTime.Today && !string.IsNullOrEmpty(timeSlot))
                {
                    if (timeSlot.Contains("-"))
                    {
                        string[] timeParts = timeSlot.Split('-');
                        if (timeParts.Length == 2)
                        {
                            string endTimeStr = timeParts[1].Trim();
                            if (TimeSpan.TryParse(endTimeStr, out TimeSpan endTime))
                            {
                                return DateTime.Now.TimeOfDay > endTime;
                            }
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private string GetTicketTypeDisplayName(string type)
        {
            if (string.IsNullOrEmpty(type)) return "Стандарт";
            switch (type.ToLower().Trim())
            {
                case "adult": return "✅ Взрослый";
                case "child": return "🧒 Детский";
                case "senior": return "👴 Пенсионер";
                case "vip": return "💎 VIP";
                default: return $"✅ {type}";
            }
        }

        private bool IsBookingPassed(Booking booking)
        {
            try
            {
                // Добавлена проверка на формат времени, чтобы избежать краша
                string[] parts = booking.TimeSlot.Split('-');
                if (parts.Length > 1)
                {
                    DateTime sessionDateTime = booking.Date.Add(TimeSpan.Parse(parts[1]));
                    return sessionDateTime < DateTime.Now;
                }
                return booking.Date < DateTime.Now;
            }
            catch { return false; }
        }

        private void UpdateReviewsList()
        {
            if (reviewsFlowPanel == null) return;
            reviewsFlowPanel.Controls.Clear();

            var userSpecificReviews = userReviews?
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Date)
                .ToList() ?? new List<Review>();

            if (userSpecificReviews.Count > 0)
            {
                foreach (var review in userSpecificReviews)
                {
                    reviewsFlowPanel.Controls.Add(CreateReviewPanel(review));
                }
            }
            else
            {
                Label lblNoReviews = new Label
                {
                    Text = "У вас пока нет отзывов\nДобавьте первый отзыв!",
                    Size = new Size(reviewsFlowPanel.Width - 20, 60),
                    Font = new Font("Segoe UI", 11, FontStyle.Italic),
                    ForeColor = Color.Gray,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin = new Padding(10)
                };
                reviewsFlowPanel.Controls.Add(lblNoReviews);
            }

            reviewsFlowPanel.Refresh();
        }

        private Panel CreateReviewPanel(Review review)
        {
            Panel reviewPanel = new Panel
            {
                Size = new Size(reviewsFlowPanel.Width - 25, 80),
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(5)
            };

            string stars = new string('⭐', Math.Max(1, Math.Min(5, review.Rating)));
            Label lblRating = new Label
            {
                Text = stars,
                Location = new Point(10, 10),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = warningColor
            };

            Label lblDate = new Label
            {
                Text = review.Date.ToString("dd.MM.yyyy HH:mm"),
                Location = new Point(reviewPanel.Width - 120, 10),
                Size = new Size(110, 20),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleRight
            };

            Label lblText = new Label
            {
                Text = review.Text,
                Location = new Point(10, 35),
                Size = new Size(reviewPanel.Width - 20, 40),
                Font = new Font("Segoe UI", 9),
                ForeColor = textColor
            };

            reviewPanel.Controls.AddRange(new Control[] { lblRating, lblDate, lblText });
            return reviewPanel;
        }

        private async void BtnRemoveBooking_Click(object sender, EventArgs e)
        {
            if (dgvBookings.SelectedRows.Count == 0) return;
            var selectedRow = dgvBookings.SelectedRows[0];

            if (selectedRow.Cells["BookingId"].Value == null ||
                !int.TryParse(selectedRow.Cells["BookingId"].Value.ToString(), out int bookingId)) return;

            var bookingToRemove = userBookings.FirstOrDefault(b => b.Id == bookingId);
            if (bookingToRemove == null) return;

            var result = MessageBox.Show($"Удалить бронирование #{bookingId}?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var response = await parentForm.SendServerRequest(new { Command = "cancel_booking", BookingId = bookingId, UserId = userId }); // Добавлен UserId для надежности
                    if (response.TryGetProperty("Success", out var success) && success.GetBoolean())
                    {
                        userBookings.RemoveAll(b => b.Id == bookingId);
                        // Если в parentForm есть метод для удаления, раскомментируйте:
                        // await parentForm.RemoveBookingAsync(bookingToRemove);

                        UpdateBookingsGrid();
                        UpdateTotalCost();
                        UpdateStatistics();

                        MessageBox.Show("Бронирование удалено.");
                    }
                    else
                    {
                        string error = response.TryGetProperty("Error", out var err) ? err.GetString() : "Неизвестная ошибка";
                        MessageBox.Show($"Ошибка удаления: {error}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сети: {ex.Message}");
                }
            }
        }

        private async void BtnAddReview_Click(object sender, EventArgs e)
        {
            if (reviewsPanel == null) return;
            var txtNewReview = reviewsPanel.Controls.OfType<Panel>().First().Controls.OfType<TextBox>()
                .FirstOrDefault(t => t.Name == "txtNewReview");
            var cmbRating = reviewsPanel.Controls.OfType<Panel>().First().Controls.OfType<ComboBox>()
                .FirstOrDefault(c => c.Name == "cmbRating");

            if (txtNewReview == null || cmbRating == null) return;

            string reviewText = txtNewReview.Text.Trim();
            if (reviewText.Length < 3)
            {
                MessageBox.Show("Введите отзыв (минимум 3 символа).");
                return;
            }

            try
            {
                var response = await parentForm.SendServerRequest(new
                {
                    Command = "add_review",
                    UserId = userId,
                    Rating = cmbRating.SelectedIndex + 1,
                    Text = reviewText
                });

                if (response.TryGetProperty("Success", out var s) && s.GetBoolean())
                {
                    txtNewReview.Clear();
                    MessageBox.Show("Отзыв добавлен!");
                    await LoadUserReviewsAsync();
                    UpdateReviewsList();
                    UpdateStatistics();
                }
                else
                {
                    MessageBox.Show("Ошибка при добавлении отзыва.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}