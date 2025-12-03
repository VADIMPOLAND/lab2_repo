using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace IceArena.Client
{
    public partial class UsersTab : UserControl
    {
        private DataGridView dgvUsers;
        private TextBox txtSearch;
        private Button btnAdd, btnEdit, btnDelete, btnRefresh;
        private Panel statsPanel;
        private FlowLayoutPanel statsContainer;
        private Label lblTotalUsers, lblActiveToday, lblNewThisWeek;
        private Panel weeklyStatsPanel;
        private const string ConnectionString = "Server=DESKTOP-I80K0OH\\SQLEXPRESS;Database=Ice_Arena;Trusted_Connection=true;TrustServerCertificate=true;";
        // Цветовая схема (современная, приятная глазу)
        private readonly Color Primary = Color.FromArgb(46, 134, 193);
        private readonly Color Success = Color.FromArgb(46, 204, 113);
        private readonly Color Danger = Color.FromArgb(231, 76, 60);
        private readonly Color Warning = Color.FromArgb(241, 196, 18);
        private readonly Color Dark = Color.FromArgb(44, 62, 80);
        private readonly Color LightBg = Color.FromArgb(248, 252, 255);
        private readonly Color CardBg = Color.White;
        private readonly Color Border = Color.FromArgb(230, 230, 230);
        private readonly Color Purple = Color.FromArgb(155, 89, 182);
        private readonly Color Orange = Color.FromArgb(243, 156, 18);
        public UsersTab()
        {
            InitializeComponents();
            this.DoubleBuffered = true;
            this.BackColor = LightBg;
            this.Padding = new Padding(20);
        }
        private void InitializeComponents()
        {
            // === Основной layout ===
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            this.Controls.Add(mainLayout);
            // === Левая часть — таблица + поиск ===
            var leftPanel = CreateCard();
            mainLayout.Controls.Add(leftPanel, 0, 0);
            var leftLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(25, 20, 25, 25) };
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            leftPanel.Controls.Add(leftLayout);
            // Заголовок
            leftLayout.Controls.Add(new Label
            {
                Text = "УПРАВЛЕНИЕ ПОЛЬЗОВАТЕЛЯМИ",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Dark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            // Поиск
            var searchPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(15, 10, 15, 10) };
            searchPanel.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(Border, 1), 0, 0, searchPanel.Width - 1, searchPanel.Height - 1);
            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.Gray,
                Text = "Поиск по email...",
                BorderStyle = BorderStyle.None
            };
            txtSearch.GotFocus += (s, e) => { if (txtSearch.Text == "Поиск по email...") { txtSearch.Text = ""; txtSearch.ForeColor = Color.Black; } };
            txtSearch.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) { txtSearch.Text = "Поиск по email..."; txtSearch.ForeColor = Color.Gray; } };
            txtSearch.TextChanged += (s, e) => FilterUsers();
            searchPanel.Controls.Add(txtSearch);
            leftLayout.Controls.Add(searchPanel, 0, 1);
            // Таблица
            dgvUsers = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 10F),
                GridColor = Color.FromArgb(240, 240, 240),
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 45 },
                ReadOnly = true
            };
            dgvUsers.Columns.AddRange(new DataGridViewTextBoxColumn[]
            {
                new() { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", Width = 60 },
                new() { Name = "Email", HeaderText = "EMAIL", DataPropertyName = "Email", FillWeight = 40 },
                new() { Name = "Role", HeaderText = "РОЛЬ", DataPropertyName = "Role", FillWeight = 20 },
                new() { Name = "RegDate", HeaderText = "РЕГИСТРАЦИЯ", DataPropertyName = "RegDate", FillWeight = 25 }
            });
            dgvUsers.CellDoubleClick += (s, e) => EditSelectedUser();
            dgvUsers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvUsers.ColumnHeadersHeight = 45;
            dgvUsers.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            dgvUsers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvUsers.EnableHeadersVisualStyles = false;
            leftLayout.Controls.Add(dgvUsers, 0, 2);
            // Кнопки - полностью переработанная панель кнопок
            var btnPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(0, 10, 0, 0)
            };
            btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            leftLayout.Controls.Add(btnPanel, 0, 3);
            // Создаем кнопки с увеличенной шириной для "РЕДАКТИРОВАТЬ"
            btnAdd = CreateFlatButton("ДОБАВИТЬ", Success, 0, 48);
            btnEdit = CreateFlatButton("РЕДАКТИРОВАТЬ", Primary, 0, 48);
            btnDelete = CreateFlatButton("УДАЛИТЬ", Danger, 0, 48);
            btnRefresh = CreateFlatButton("ОБНОВИТЬ", Color.FromArgb(149, 165, 166), 0, 48);
            btnAdd.Click += (s, e) => AddUser();
            btnEdit.Click += (s, e) => EditSelectedUser();
            btnDelete.Click += (s, e) => DeleteSelectedUser();
            btnRefresh.Click += (s, e) => LoadUsers();
            // Устанавливаем Dock для кнопок, чтобы они заполняли свои ячейки
            btnAdd.Dock = DockStyle.Fill;
            btnEdit.Dock = DockStyle.Fill;
            btnDelete.Dock = DockStyle.Fill;
            btnRefresh.Dock = DockStyle.Fill;
            // Добавляем кнопки в TableLayoutPanel
            btnPanel.Controls.Add(btnAdd, 0, 0);
            btnPanel.Controls.Add(btnEdit, 1, 0);
            btnPanel.Controls.Add(btnDelete, 2, 0);
            btnPanel.Controls.Add(btnRefresh, 3, 0);
            // === Правая часть — статистика ===
            statsPanel = CreateCard();
            mainLayout.Controls.Add(statsPanel, 1, 0);
            var statsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(25, 20, 25, 25), RowCount = 5 };
            statsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            statsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            statsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            statsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            statsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            statsPanel.Controls.Add(statsLayout);
            statsLayout.Controls.Add(new Label
            {
                Text = "СТАТИСТИКА",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Dark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            lblTotalUsers = CreateStatCard("Всего пользователей", "0", Primary);
            lblActiveToday = CreateStatCard("Зарегистрировано сегодня", "0", Success);
            lblNewThisWeek = CreateStatCard("Новых за неделю", "0", Purple);
            statsLayout.Controls.Add(lblTotalUsers, 0, 1);
            statsLayout.Controls.Add(lblActiveToday, 0, 2);
            statsLayout.Controls.Add(lblNewThisWeek, 0, 3);
            // Панель для недельной статистики
            weeklyStatsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBg,
                Margin = new Padding(0, 20, 0, 0),
                Padding = new Padding(15)
            };
            weeklyStatsPanel.Paint += (s, e) =>
                e.Graphics.DrawRectangle(new Pen(Border, 1), 0, 0, weeklyStatsPanel.Width - 1, weeklyStatsPanel.Height - 1);
            statsLayout.Controls.Add(weeklyStatsPanel, 0, 4);
            LoadUsers();
        }
        private Panel CreateCard()
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBg,
                Padding = new Padding(5),
                Margin = new Padding(10, 0, 0, 0)
            };
            card.Paint += (s, e) =>
                e.Graphics.DrawRectangle(new Pen(Border, 1), 0, 0, card.Width - 1, card.Height - 1);
            return card;
        }
        private Label CreateStatCard(string title, string value, Color color)
        {
            var card = new Label
            {
                BackColor = color,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 15, 20, 15),
                Font = new Font("Segoe UI", 11F),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 15),
                Height = 90
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.Clear(color);
                // Тень для текста для лучшей читаемости
                using (var shadowBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                {
                    g.DrawString(title, new Font("Segoe UI", 10F, FontStyle.Regular),
                                Brushes.White, 12, 17);
                    g.DrawString(value, new Font("Segoe UI", 24F, FontStyle.Bold),
                                Brushes.White, 12, 35);
                }
            };
            return card;
        }
        private Button CreateFlatButton(string text, Color color, int width, int height)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(width, height),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 0, 2, 0) // Минимальные отступы
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(color, 0.1f);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(color, 0.2f);
            // Уменьшаем размер шрифта для длинного текста
            if (text.Length > 10)
            {
                btn.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }
            return btn;
        }
        private void LoadUsers()
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();
                var dt = new DataTable();
                new SqlDataAdapter("SELECT Id, Email, Role, RegDate FROM Users ORDER BY RegDate DESC", conn).Fill(dt);
                dgvUsers.DataSource = dt;
                // Форматирование даты
                if (dgvUsers.Columns["RegDate"] is DataGridViewTextBoxColumn col)
                    col.DefaultCellStyle.Format = "dd.MM.yyyy HH:mm";
                UpdateStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки пользователей: " + ex.Message, "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void FilterUsers()
        {
            if (dgvUsers.DataSource is DataTable dt)
            {
                if (string.IsNullOrWhiteSpace(txtSearch.Text) || txtSearch.Text == "Поиск по email...")
                    dt.DefaultView.RowFilter = "";
                else
                    dt.DefaultView.RowFilter = $"Email LIKE '%{txtSearch.Text.Replace("'", "''")}%'";
            }
        }
        private void UpdateStats()
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();
                // Основная статистика
                int total = (int)new SqlCommand("SELECT COUNT(*) FROM Users", conn).ExecuteScalar();
                int today = (int)new SqlCommand("SELECT COUNT(*) FROM Users WHERE CAST(RegDate AS DATE) = CAST(GETDATE() AS DATE)", conn).ExecuteScalar();
                // Исправленный запрос для недельной статистики - считаем последние 7 дней включая сегодня
                int week = (int)new SqlCommand(@"
                    SELECT COUNT(*) FROM Users
                    WHERE RegDate >= DATEADD(DAY, -6, CAST(GETDATE() AS DATE))
                    AND RegDate < DATEADD(DAY, 1, CAST(GETDATE() AS DATE))", conn).ExecuteScalar();
                // Обновляем основные карточки
                UpdateStatCard(lblTotalUsers, "Всего пользователей", total.ToString());
                UpdateStatCard(lblActiveToday, "Зарегистрировано сегодня", today.ToString());
                UpdateStatCard(lblNewThisWeek, "Новых за неделю", week.ToString());
                // Статистика по дням за последние 7 дней
                UpdateWeeklyStats(conn);
                // Обновляем подпись под таблицей
                this.FindForm()?.Text = $"Панель администратора • Пользователей: {total}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка обновления статистики: " + ex.Message, "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void UpdateStatCard(Label card, string title, string value)
        {
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.Clear(card.BackColor);
                // Тень для текста для лучшей читаемости
                using (var shadowBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                {
                    g.DrawString(title, new Font("Segoe UI", 10F, FontStyle.Regular),
                                Brushes.White, 12, 17);
                    g.DrawString(value, new Font("Segoe UI", 24F, FontStyle.Bold),
                                Brushes.White, 12, 35);
                }
            };
            card.Invalidate();
        }
        private void UpdateWeeklyStats(SqlConnection conn)
        {
            // Очищаем панель недельной статистики
            weeklyStatsPanel.Controls.Clear();
            // Заголовок
            var headerLabel = new Label
            {
                Text = "Регистрации по дням:",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Dark,
                Dock = DockStyle.Top,
                Height = 30,
                Margin = new Padding(0, 0, 0, 10)
            };
            weeklyStatsPanel.Controls.Add(headerLabel);
            // Получаем данные за последние 7 дней (включая сегодня)
            var query = @"
                SELECT
                    CAST(RegDate AS DATE) as RegDate,
                    COUNT(*) as UserCount
                FROM Users
                WHERE RegDate >= DATEADD(DAY, -6, CAST(GETDATE() AS DATE))
                AND RegDate < DATEADD(DAY, 1, CAST(GETDATE() AS DATE))
                GROUP BY CAST(RegDate AS DATE)
                ORDER BY RegDate DESC";
            var dt = new DataTable();
            new SqlDataAdapter(query, conn).Fill(dt);
            // Создаем таблицу для отображения статистики
            var tablePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 7,
                ColumnCount = 3, // Добавили третью колонку для количества
                Padding = new Padding(0, 5, 0, 0)
            };
            // Настройка стилей строк и колонок
            for (int i = 0; i < 7; i++)
            {
                tablePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            }
            tablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            // Заполняем данными
            var days = new[] { "Сегодня", "Вчера", "2 дня назад", "3 дня назад", "4 дня назад", "5 дней назад", "6 дней назад" };
            var dates = new List<DateTime>();
            // Генерируем даты за последние 7 дней (включая сегодня)
            for (int i = 0; i < 7; i++)
            {
                dates.Add(DateTime.Today.AddDays(-i));
            }
            int totalWeekCount = 0;
            for (int i = 0; i < 7; i++)
            {
                var date = dates[i];
                var dateStr = date.ToString("dd.MM.yyyy");
                // Ищем данные для этой даты
                var row = dt.AsEnumerable()
                    .FirstOrDefault(r => ((DateTime)r["RegDate"]).Date == date);
                int count = row != null ? Convert.ToInt32(row["UserCount"]) : 0;
                totalWeekCount += count;
                // День недели и дата
                var dayLabel = new Label
                {
                    Text = days[i],
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Dark,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(5, 0, 0, 0)
                };
                // Дата
                var dateLabel = new Label
                {
                    Text = $"({dateStr})",
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = Color.Gray,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(5, 0, 0, 0)
                };
                // Количество
                var countLabel = new Label
                {
                    Text = count.ToString(),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = count > 0 ? Success : Danger,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleRight,
                    Padding = new Padding(0, 0, 5, 0)
                };
                tablePanel.Controls.Add(dayLabel, 0, i);
                tablePanel.Controls.Add(dateLabel, 1, i);
                tablePanel.Controls.Add(countLabel, 2, i);
            }
            weeklyStatsPanel.Controls.Add(tablePanel);
            // Добавляем итоговую строку
            var totalPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(5)
            };
            var totalLabel = new Label
            {
                Text = $"Всего за 7 дней: {totalWeekCount}",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Dark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };
            totalPanel.Controls.Add(totalLabel);
            weeklyStatsPanel.Controls.Add(totalPanel);
        }
        private void AddUser()
        {
            var form = new AddEditUserForm();
            form.UserSaved += () => { LoadUsers(); };
            form.ShowDialog();
        }
        private void EditSelectedUser()
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите пользователя для редактирования", "Внимание",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var row = dgvUsers.SelectedRows[0];
            string email = row.Cells["Email"].Value?.ToString();
            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Email не найден для выбранной строки", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var form = new AddEditUserForm(email);
            form.UserSaved += () => { LoadUsers(); };
            form.ShowDialog();
        }
        private void DeleteSelectedUser()
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите пользователя для удаления", "Внимание",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var row = dgvUsers.SelectedRows[0];
            if (row.Cells["Id"].Value == null)
            {
                MessageBox.Show("ID не найден для выбранной строки", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            int id = Convert.ToInt32(row.Cells["Id"].Value);
            string email = row.Cells["Email"].Value?.ToString() ?? "";
            if (MessageBox.Show($"Удалить пользователя?\n\n{email}", "Подтверждение",
                              MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    using var conn = new SqlConnection(ConnectionString);
                    conn.Open();
                    new SqlCommand("DELETE FROM Users WHERE Id = @Id", conn)
                    { Parameters = { new SqlParameter("@Id", id) } }.ExecuteNonQuery();
                    LoadUsers();
                    MessageBox.Show("Пользователь удалён", "Успех",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка удаления: " + ex.Message, "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}