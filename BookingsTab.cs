using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;
// Используем современный клиент SQL
using Microsoft.Data.SqlClient;

namespace IceArena.Client
{
    public partial class BookingsTab : UserControl
    {
        // Добавлен '!' для подавления CS8618 (Nullable Reference Type warnings).
        private DataGridView dgvBookings = null!;
        private Button btnRefreshBookings = null!, btnExportExcel = null!, btnConfirm = null!, btnComplete = null!, btnCancel = null!, btnDelete = null!;
        private Label lblTitle = null!, lblSubtitle = null!;
        private Panel headerPanel = null!, statsPanel = null!, centerPanel = null!;
        private ComboBox cmbStatusFilter = null!;

        private const string ConnectionString = "Server=DESKTOP-I80K0OH\\SQLEXPRESS;Database=Ice_Arena;Trusted_Connection=true;TrustServerCertificate=true;";

        // Цветовая схема
        private readonly Color PrimaryColor = Color.FromArgb(41, 128, 185);
        private readonly Color SecondaryColor = Color.FromArgb(52, 152, 219);
        private readonly Color AccentColor = Color.FromArgb(46, 204, 113);
        private readonly Color WarningColor = Color.FromArgb(241, 196, 15);
        private readonly Color DeleteColor = Color.FromArgb(231, 76, 60);
        private readonly Color BackgroundColor = Color.FromArgb(245, 245, 245);
        private readonly Color CardColor = Color.White;

        public BookingsTab()
        {
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            this.SuspendLayout();
            this.Name = "BookingsTab";
            this.Dock = DockStyle.Fill;
            this.BackColor = BackgroundColor;
            this.DoubleBuffered = true;

            SetupLayout();

            this.ResumeLayout(false);

            this.Load += (s, e) =>
            {
                LoadBookings();
                UpdateBookingStats();
            };
        }

        private void SetupLayout()
        {
            // --- 1. Шапка (Верх) ---
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = PrimaryColor,
                Padding = new Padding(20, 10, 20, 10)
            };

            lblTitle = new Label
            {
                Text = "ПАНЕЛЬ АДМИНИСТРАТОРА - ЛЕДОВАЯ АРЕНА",
                Dock = DockStyle.Top,
                Height = 35,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblSubtitle = new Label
            {
                Text = "Управление бронированиями",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 220, 220),
                TextAlign = ContentAlignment.MiddleLeft
            };

            headerPanel.Controls.Add(lblSubtitle);
            headerPanel.Controls.Add(lblTitle);
            this.Controls.Add(headerPanel);

            // --- 2. Панель Статистики (Справа) ---
            statsPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 400,
                BackColor = Color.White,
                Padding = new Padding(10),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(statsPanel);

            // --- 3. Центральная часть (Таблица и кнопки) ---
            centerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = BackgroundColor
            };
            this.Controls.Add(centerPanel);

            var mainGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
            };
            mainGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            mainGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));

            centerPanel.Controls.Add(mainGrid);

            // 3.1 Панель инструментов (Фильтры + Действия)
            var toolbarPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = false,
                WrapContents = false,
                Padding = new Padding(0, 5, 0, 0)
            };

            // Фильтр
            var lblFilter = new Label
            {
                Text = "🔍 Статус:",
                AutoSize = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 8, 5, 0)
            };

            cmbStatusFilter = new ComboBox
            {
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(0, 5, 20, 0)
            };
            cmbStatusFilter.Items.AddRange(new object[] { "Все статусы", "Booked", "Confirmed", "Cancelled", "Completed" });
            cmbStatusFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectedIndexChanged += (s, e) => LoadBookings();

            // Кнопки действий
            btnConfirm = CreateActionButton("✅ ПОДТВЕРДИТЬ", AccentColor);
            btnConfirm.Click += (s, e) => ChangeBookingStatus("Confirmed");

            btnComplete = CreateActionButton("🏁 ЗАВЕРШИТЬ", Color.FromArgb(155, 89, 182));
            btnComplete.Click += (s, e) => ChangeBookingStatus("Completed");

            btnCancel = CreateActionButton("🚫 ОТМЕНИТЬ", WarningColor);
            btnCancel.Click += (s, e) => ChangeBookingStatus("Cancelled");

            btnDelete = CreateActionButton("❌ УДАЛИТЬ", DeleteColor);
            btnDelete.Click += (s, e) => DeleteBooking();

            toolbarPanel.Controls.AddRange(new Control[] { lblFilter, cmbStatusFilter, btnConfirm, btnComplete, btnCancel, btnDelete });
            mainGrid.Controls.Add(toolbarPanel, 0, 0);

            // 3.2 Таблица (DataGridView)
            dgvBookings = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(240, 240, 240),
                Font = new Font("Segoe UI", 10),
                RowTemplate = { Height = 40 }
            };

            // Настройка колонок
            dgvBookings.Columns.Add("Id", "ID");
            dgvBookings.Columns.Add("UserEmail", "ПОЛЬЗОВАТЕЛЬ");
            dgvBookings.Columns.Add("Date", "ДАТА");
            dgvBookings.Columns.Add("TimeSlot", "ВРЕМЯ");
            dgvBookings.Columns.Add("Tickets", "БИЛЕТЫ");
            dgvBookings.Columns.Add("TotalCost", "СТОИМОСТЬ");
            dgvBookings.Columns.Add("Status", "СТАТУС");
            dgvBookings.Columns.Add("Created", "СОЗДАНО");

            dgvBookings.Columns["Id"].FillWeight = 5;
            dgvBookings.Columns["UserEmail"].FillWeight = 25;
            dgvBookings.Columns["Date"].FillWeight = 10;
            dgvBookings.Columns["TimeSlot"].FillWeight = 12;
            dgvBookings.Columns["Tickets"].FillWeight = 8;
            dgvBookings.Columns["TotalCost"].FillWeight = 12;
            dgvBookings.Columns["Status"].FillWeight = 13;
            dgvBookings.Columns["Created"].FillWeight = 15;

            dgvBookings.Columns["Id"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvBookings.Columns["Tickets"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvBookings.Columns["TotalCost"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvBookings.Columns["Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvBookings.Columns["Date"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvBookings.Columns["TimeSlot"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            StyleDataGridView(dgvBookings);

            var tableContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 10) };
            tableContainer.Controls.Add(dgvBookings);
            mainGrid.Controls.Add(tableContainer, 0, 1);

            // 3.3 Нижние кнопки (Обновить/Экспорт)
            var bottomPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 10, 0, 0)
            };

            btnRefreshBookings = CreateActionButton("🔄 ОБНОВИТЬ", SecondaryColor);
            btnRefreshBookings.Width = 160;
            btnRefreshBookings.Click += (s, e) => { LoadBookings(); UpdateBookingStats(); };

            btnExportExcel = CreateActionButton("📊 ЭКСПОРТ (CSV)", AccentColor);
            btnExportExcel.Width = 160;
            btnExportExcel.Click += (s, e) => ExportToExcel();

            bottomPanel.Controls.Add(btnRefreshBookings);
            bottomPanel.Controls.Add(btnExportExcel);
            mainGrid.Controls.Add(bottomPanel, 0, 2);
        }

        private Button CreateActionButton(string text, Color bg)
        {
            return new Button
            {
                Text = text,
                Size = new Size(130, 38),
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
        }

        private void StyleDataGridView(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = PrimaryColor;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.ColumnHeadersHeight = 45;

            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            dgv.RowsDefaultCellStyle.BackColor = Color.White;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 235, 252);
            dgv.DefaultCellStyle.SelectionForeColor = Color.Black;
        }

        // --- ЛОГИКА РАБОТЫ С ДАННЫМИ ---

        private void LoadBookings()
        {
            try
            {
                dgvBookings.Rows.Clear();
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();

                string sql = @"
                    SELECT 
                        b.Id, 
                        u.Email, 
                        FORMAT(s.Date, 'dd.MM.yyyy') as FormattedDate,
                        s.TimeSlot, 
                        COUNT(t.Id) as TicketCount, 
                        ISNULL(SUM(t.Price * t.Quantity), 0) as TotalCost,
                        b.Status, 
                        FORMAT(b.BookingDate, 'dd.MM.yyyy HH:mm') as FormattedCreated
                    FROM Bookings b
                    JOIN Users u ON b.UserId = u.Id
                    JOIN Schedule s ON b.ScheduleId = s.Id
                    LEFT JOIN Tickets t ON b.Id = t.BookingId";

                string? selectedStatus = cmbStatusFilter.SelectedItem?.ToString();
                if (selectedStatus != null && selectedStatus != "Все статусы")
                {
                    sql += " WHERE b.Status = @Status";
                }

                sql += @" GROUP BY b.Id, u.Email, s.Date, s.TimeSlot, b.Status, b.BookingDate
                         ORDER BY b.BookingDate DESC";

                using var cmd = new SqlCommand(sql, conn);

                if (selectedStatus != null && selectedStatus != "Все статусы")
                {
                    cmd.Parameters.AddWithValue("@Status", selectedStatus);
                }

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string userEmail = reader.GetString(1);
                    string formattedDate = reader.GetString(2);
                    string timeSlot = reader.GetString(3);
                    int ticketCount = reader.GetInt32(4);
                    decimal totalCost = reader.GetDecimal(5);
                    string status = reader.GetString(6);
                    string formattedCreated = reader.GetString(7);

                    string formattedCost = $"{totalCost:F2} BYN";
                    dgvBookings.Rows.Add(id, userEmail, formattedDate, timeSlot, ticketCount, formattedCost, status, formattedCreated);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки бронирований: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateBookingStats()
        {
            try
            {
                if (statsPanel == null) return;

                statsPanel.SuspendLayout();
                statsPanel.Controls.Clear();

                using var conn = new SqlConnection(ConnectionString);
                conn.Open();

                int yPos = 10;

                statsPanel.Controls.Add(CreateHeaderPanel("📊 ОБЩАЯ СТАТИСТИКА", 0));

                string totalBookingsSql = "SELECT COUNT(*) FROM Bookings";
                using var cmdTotal = new SqlCommand(totalBookingsSql, conn);
                int totalBookings = (int)cmdTotal.ExecuteScalar();
                statsPanel.Controls.Add(CreateStatsCard($"Всего бронирований: {totalBookings}", yPos += 50, Color.FromArgb(240, 248, 255), 14, true));

                string statusSql = "SELECT Status, COUNT(*) as Count FROM Bookings GROUP BY Status";
                using var cmdStatus = new SqlCommand(statusSql, conn);
                using var reader = cmdStatus.ExecuteReader();
                var statusData = new Dictionary<string, int>();
                while (reader.Read()) statusData[reader.GetString(0)] = reader.GetInt32(1);
                reader.Close();

                yPos += 50;
                foreach (var status in statusData)
                {
                    Color statusColor = status.Key.ToLower() switch
                    {
                        "booked" => Color.FromArgb(255, 245, 230),
                        "confirmed" => Color.FromArgb(230, 255, 230),
                        "cancelled" => Color.FromArgb(255, 230, 230),
                        "completed" => Color.FromArgb(230, 240, 255),
                        _ => Color.FromArgb(245, 245, 245)
                    };
                    statsPanel.Controls.Add(CreateStatsCard($"{status.Key}: {status.Value}", yPos, statusColor, 12, false));
                    yPos += 45;
                }

                yPos += 20;
                statsPanel.Controls.Add(CreateHeaderPanel("💰 ДОХОД ЗА СЕГОДНЯ", yPos));
                yPos += 50;

                decimal todayRevenue = 0;
                try
                {
                    string revenueSql = "SELECT ISNULL(SUM(Income), 0) FROM ArenaMetrics WHERE CAST(Date AS DATE) = CAST(GETDATE() AS DATE)";
                    using var cmdRevenue = new SqlCommand(revenueSql, conn);
                    // Явное приведение для nullable decimal
                    todayRevenue = (decimal?)cmdRevenue.ExecuteScalar() ?? 0;
                }
                catch { }

                statsPanel.Controls.Add(CreateStatsCard($"{todayRevenue:F2} BYN", yPos, Color.FromArgb(230, 255, 230), 16, true));
                yPos += 60;

                statsPanel.Controls.Add(CreateHeaderPanel("📈 ПОСЛЕДНИЕ 7 ДНЕЙ", yPos));
                yPos += 50;

                string dailyStatsSql = @"
                    SELECT CAST(BookingDate AS DATE) as BookingDate, COUNT(*) as BookingCount
                    FROM Bookings WHERE BookingDate >= DATEADD(DAY, -6, CAST(GETDATE() AS DATE))
                    GROUP BY CAST(BookingDate AS DATE) ORDER BY BookingDate DESC";
                using var cmdDaily = new SqlCommand(dailyStatsSql, conn);
                using var readerDaily = cmdDaily.ExecuteReader();
                var dailyData = new Dictionary<DateTime, int>();
                while (readerDaily.Read()) dailyData[readerDaily.GetDateTime(0).Date] = readerDaily.GetInt32(1);
                readerDaily.Close();

                for (int i = 0; i < 7; i++)
                {
                    DateTime day = DateTime.Today.AddDays(-i);
                    int bookingCount = dailyData.ContainsKey(day) ? dailyData[day] : 0;
                    string dayName = i == 0 ? "🎯 СЕГОДНЯ" : i == 1 ? "📅 ВЧЕРА" : $"📅 {day:dd.MM.yyyy}";
                    Color dayColor = i == 0 ? Color.FromArgb(255, 255, 200) : i == 1 ? Color.FromArgb(230, 240, 255) : Color.White;
                    statsPanel.Controls.Add(CreateStatsCard($"{dayName}: {bookingCount}", yPos, dayColor, 11, i <= 1));
                    yPos += 45;
                }

                var spacer = new Panel { Location = new Point(0, yPos), Size = new Size(10, 50) };
                statsPanel.Controls.Add(spacer);

                statsPanel.ResumeLayout();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка статистики: {ex.Message}");
            }
        }

        private Panel CreateHeaderPanel(string text, int yPos)
        {
            var p = new Panel
            {
                Location = new Point(10, yPos),
                Size = new Size(360, 40),
                BackColor = PrimaryColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            var l = new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };
            p.Controls.Add(l);
            return p;
        }

        private Panel CreateStatsCard(string text, int yPos, Color bg, int fontSize, bool isBold)
        {
            var p = new Panel
            {
                Location = new Point(10, yPos),
                Size = new Size(360, 38),
                BackColor = bg,
                BorderStyle = BorderStyle.FixedSingle
            };
            var l = new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", fontSize, isBold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 60, 60),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            p.Controls.Add(l);
            return p;
        }

        private void ChangeBookingStatus(string newStatus)
        {
            if (dgvBookings.SelectedRows.Count == 0)
            {
                MessageBox.Show("Пожалуйста, выберите бронирование.", "Инфо", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedRow = dgvBookings.SelectedRows[0];
            // Использование явного приведения типов для безопасности
            if (selectedRow.Cells["Id"].Value == null) return;
            int bookingId = Convert.ToInt32(selectedRow.Cells["Id"].Value);
            string? userEmail = selectedRow.Cells["UserEmail"].Value?.ToString();

            if (MessageBox.Show($"Изменить статус брони #{bookingId} ({userEmail}) на '{newStatus}'?",
                "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    using var conn = new SqlConnection(ConnectionString);
                    conn.Open();
                    string sql = "UPDATE Bookings SET Status = @Status WHERE Id = @Id";
                    using var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@Status", newStatus);
                    cmd.Parameters.AddWithValue("@Id", bookingId);

                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        LoadBookings();
                        UpdateBookingStats();
                        MessageBox.Show("Статус обновлен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DeleteBooking()
        {
            if (dgvBookings.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите бронирование для удаления.", "Инфо", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Использование явного приведения типов для безопасности
            if (dgvBookings.SelectedRows[0].Cells["Id"].Value == null) return;
            int bookingId = Convert.ToInt32(dgvBookings.SelectedRows[0].Cells["Id"].Value);

            if (MessageBox.Show($"Удалить бронирование #{bookingId}? Это действие необратимо.",
                "Удаление", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    using var conn = new SqlConnection(ConnectionString);
                    conn.Open();

                    // Сначала удаляем билеты
                    using (var cmdTickets = new SqlCommand("DELETE FROM Tickets WHERE BookingId = @Id", conn))
                    {
                        cmdTickets.Parameters.AddWithValue("@Id", bookingId);
                        cmdTickets.ExecuteNonQuery();
                    }

                    // Затем бронирование
                    using (var cmdBookings = new SqlCommand("DELETE FROM Bookings WHERE Id = @Id", conn))
                    {
                        cmdBookings.Parameters.AddWithValue("@Id", bookingId);
                        cmdBookings.ExecuteNonQuery();
                    }

                    LoadBookings();
                    UpdateBookingStats();
                    MessageBox.Show("Удалено успешно.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportToExcel()
        {
            if (dgvBookings.Rows.Count == 0) return;

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "CSV файлы (*.csv)|*.csv",
                FileName = $"BookingReport_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (var sw = new StreamWriter(sfd.FileName, false, Encoding.UTF8))
                    {
                        sw.WriteLine("ID;USER;DATE;TIME;TICKETS;COST;STATUS;CREATED");
                        foreach (DataGridViewRow row in dgvBookings.Rows)
                        {
                            // Безопасная обработка нулевых значений
                            string id = row.Cells["Id"].Value?.ToString() ?? "";
                            string userEmail = row.Cells["UserEmail"].Value?.ToString() ?? "";
                            string date = row.Cells["Date"].Value?.ToString() ?? "";
                            string timeSlot = row.Cells["TimeSlot"].Value?.ToString() ?? "";
                            string tickets = row.Cells["Tickets"].Value?.ToString() ?? "";
                            string totalCost = row.Cells["TotalCost"].Value?.ToString()?.Replace(" BYN", "").Trim() ?? "";
                            string status = row.Cells["Status"].Value?.ToString() ?? "";
                            string created = row.Cells["Created"].Value?.ToString() ?? "";

                            // Используем точку с запятой в качестве разделителя для CSV (для русскоязычного Excel)
                            sw.WriteLine($"{id};{userEmail};{date};{timeSlot};{tickets};{totalCost};{status};{created}");
                        }
                    }
                    MessageBox.Show("Экспорт завершен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка экспорта: {ex.Message}");
                }
            }
        }
    }
}