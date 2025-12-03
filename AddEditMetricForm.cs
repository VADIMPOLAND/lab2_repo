using System;
using System.Data;
using System.Data.SqlClient; // Или Microsoft.Data.SqlClient, в зависимости от версии .NET
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace IceArena.Client
{
    public partial class AddEditMetricForm : Form
    {
        // Контролы
        private DateTimePicker dtpDate;
        private NumericUpDown numIncome, numAttendance, numElectricity;
        private TextBox txtNotes;
        private Button btnSave, btnCancel, btnInfo, btnClear;
        private Label lblTitle;
        private Panel headerPanel, contentPanel, buttonPanel;

        // Переменные состояния
        private bool isEdit;
        private DateTime currentDate;

        // Строка подключения (ОБЯЗАТЕЛЬНО ПРОВЕРЬТЕ ЕЕ ПЕРЕД ЗАПУСКОМ)
        private const string ConnectionString = "Server=DESKTOP-I80K0OH\\SQLEXPRESS;Database=Ice_Arena;Trusted_Connection=true;TrustServerCertificate=true;";

        // Цветовая схема (Modern UI Palette)
        private readonly Color PrimaryColor = Color.FromArgb(41, 128, 185);    // Синий
        private readonly Color SecondaryColor = Color.FromArgb(52, 152, 219);  // Светло-синий
        private readonly Color SuccessColor = Color.FromArgb(39, 174, 96);     // Зеленый
        private readonly Color DangerColor = Color.FromArgb(192, 57, 43);      // Красный
        private readonly Color InfoColor = Color.FromArgb(142, 68, 173);       // Фиолетовый
        private readonly Color BackgroundColor = Color.FromArgb(236, 240, 241); // Светло-серый фон
        private readonly Color CardColor = Color.White;
        private readonly Color TextColor = Color.FromArgb(44, 62, 80);
        private readonly Color HintColor = Color.FromArgb(127, 140, 141);
        private readonly Color BorderColor = Color.FromArgb(189, 195, 199);

        public AddEditMetricForm(DateTime? date = null)
        {
            // Включаем двойную буферизацию для плавности
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            isEdit = date.HasValue;
            currentDate = date ?? DateTime.Now.Date;

            InitializeForm();
            SetupUI();

            if (isEdit)
            {
                this.Text = "✏️ Редактирование метрики";
                LoadMetricData(currentDate);
                // Блокируем дату при редактировании, чтобы не создать дубликат случайно
                dtpDate.Enabled = false;
            }
            else
            {
                this.Text = "➕ Новая метрика";
            }
        }

        private void InitializeForm()
        {
            this.Size = new Size(850, 900);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = BackgroundColor;
            this.Font = new Font("Segoe UI", 10F);
        }

        private void SetupUI()
        {
            // 1. Создаем шапку
            CreateHeader();

            // 2. Создаем основную панель с полями (используем TableLayoutPanel для ровности)
            CreateContentPanel();

            // 3. Создаем кнопки управления
            CreateButtons();

            // 4. Добавляем всплывающие подсказки
            AddFieldHints();
        }

        private void CreateHeader()
        {
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = PrimaryColor,
                Padding = new Padding(20)
            };

            lblTitle = new Label
            {
                Text = isEdit ? "✏️ РЕДАКТИРОВАНИЕ МЕТРИКИ" : "➕ ДОБАВЛЕНИЕ НОВОЙ МЕТРИКИ",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            headerPanel.Controls.Add(lblTitle);
            this.Controls.Add(headerPanel);
        }

        private void CreateContentPanel()
        {
            // Основная карточка (белый фон)
            contentPanel = new Panel
            {
                Location = new Point(25, 120),
                Size = new Size(785, 650), // Фиксированный размер
                BackColor = CardColor,
                Padding = new Padding(30)
            };

            // Скругление углов для карточки
            contentPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectangle(contentPanel.ClientRectangle, 20))
                using (var pen = new Pen(BorderColor, 1))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            // Используем TableLayoutPanel для идеального выравнивания
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(10),
            };

            // Настройка колонок: Левая (названия) 40%, Правая (инпуты) 60%
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

            // Настройка строк: Автоматическая высота
            for (int i = 0; i < 5; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // --- ДОБАВЛЕНИЕ ЭЛЕМЕНТОВ ---

            // 1. Дата
            AddFormRow(layout, 0, "📅 Дата записи:", "Выберите дату метрики", out dtpDate);
            dtpDate.Value = currentDate;
            dtpDate.MaxDate = DateTime.Today; // Нельзя вводить будущее

            // 2. Доход
            AddFormRow(layout, 1, "💰 Доход (BYN):", "Общая выручка за день", out numIncome, 2, 1000000);

            // 3. Посещаемость
            AddFormRow(layout, 2, "👥 Посещаемость:", "Количество человек", out numAttendance, 0, 10000);

            // 4. Электричество
            AddFormRow(layout, 3, "⚡ Электричество (кВт):", "Расход энергии", out numElectricity, 1, 100000);

            // 5. Примечания (занимает 2 колонки внизу)
            var lblNotes = CreateLabel("📝 Примечания:", 14);
            layout.Controls.Add(lblNotes, 0, 4);
            layout.SetColumnSpan(lblNotes, 2);
            lblNotes.Margin = new Padding(0, 20, 0, 10);

            txtNotes = new TextBox
            {
                Multiline = true,
                Height = 150,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(250, 250, 250) // Чуть сероватый фон для текстбокса
            };

            // Вставляем TextBox в панель для отступов, если нужно, или напрямую
            var notePanel = new Panel { Dock = DockStyle.Top, Height = 160, Padding = new Padding(0, 5, 0, 0) };
            notePanel.Controls.Add(txtNotes);
            layout.Controls.Add(notePanel, 0, 5); // Это технически 6-я строка в добавление, но RowCount мы растянем
            layout.SetColumnSpan(notePanel, 2);


            contentPanel.Controls.Add(layout);
            this.Controls.Add(contentPanel);
        }

        // Вспомогательный метод для добавления строки с Label + Control
        private void AddFormRow(TableLayoutPanel panel, int row, string title, string hint, out DateTimePicker picker)
        {
            // Метка
            var labelPanel = CreateLabelWithHint(title, hint);
            panel.Controls.Add(labelPanel, 0, row);

            // Контрол
            picker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 12),
                Dock = DockStyle.Fill,
                Height = 35,
                Margin = new Padding(0, 15, 0, 15) // Отступы сверху и снизу
            };
            panel.Controls.Add(picker, 1, row);
        }

        private void AddFormRow(TableLayoutPanel panel, int row, string title, string hint, out NumericUpDown num, int decimals, int max)
        {
            // Метка
            var labelPanel = CreateLabelWithHint(title, hint);
            panel.Controls.Add(labelPanel, 0, row);

            // Контрол
            num = new NumericUpDown
            {
                Font = new Font("Segoe UI", 12),
                Dock = DockStyle.Fill,
                Height = 35,
                DecimalPlaces = decimals,
                Maximum = max,
                ThousandsSeparator = true,
                TextAlign = HorizontalAlignment.Right,
                Margin = new Padding(0, 15, 0, 15)
            };
            panel.Controls.Add(num, 1, row);
        }

        private Panel CreateLabelWithHint(string title, string hint)
        {
            var p = new Panel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 10, 10, 10) };
            var lTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = TextColor,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            var lHint = new Label
            {
                Text = hint,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = HintColor,
                AutoSize = true,
                Location = new Point(0, 25)
            };
            p.Controls.Add(lTitle);
            p.Controls.Add(lHint);
            return p;
        }

        private Label CreateLabel(string text, int size)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", size, FontStyle.Bold),
                ForeColor = TextColor,
                AutoSize = true
            };
        }

        private void CreateButtons()
        {
            buttonPanel = new Panel
            {
                Size = new Size(785, 80),
                Location = new Point(25, 780), // Под контент панелью
                BackColor = Color.Transparent
            };

            // Создаем кнопки
            btnInfo = CreateStyledButton("ℹ️ Справка", InfoColor, 0);
            btnClear = CreateStyledButton("🧹 Очистить", Color.Gray, 160);

            // Кнопки справа
            btnSave = CreateStyledButton("💾 СОХРАНИТЬ", SuccessColor, 0); // Позицию зададим позже
            btnCancel = CreateStyledButton("❌ Отмена", DangerColor, 0);

            // Расставляем Save/Cancel справа
            btnSave.Location = new Point(buttonPanel.Width - btnSave.Width, 0);
            btnCancel.Location = new Point(buttonPanel.Width - btnSave.Width - btnCancel.Width - 20, 0);

            // Привязка событий
            btnInfo.Click += (s, e) => ShowInformation();
            btnClear.Click += (s, e) => ClearForm();
            btnSave.Click += (s, e) => SaveMetric();
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            buttonPanel.Controls.AddRange(new Control[] { btnInfo, btnClear, btnSave, btnCancel });
            this.Controls.Add(buttonPanel);
        }

        private Button CreateStyledButton(string text, Color color, int x)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, 0),
                Size = new Size(150, 50),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;

            // Рисуем скругленные кнопки
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectangle(btn.ClientRectangle, 10))
                using (var brush = new SolidBrush(color))
                {
                    e.Graphics.FillPath(brush, path);
                    TextRenderer.DrawText(e.Graphics, text, btn.Font, btn.ClientRectangle, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            };
            return btn;
        }

        // --- ЛОГИКА ДАННЫХ И БД ---

        private void LoadMetricData(DateTime date)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    string sql = "SELECT Income, Attendance, Electricity, Notes FROM ArenaMetrics WHERE CONVERT(date, Date) = @Date"; // Поле в БД может называться Date или MetricDate, проверьте! В коде AnalyticsTab было Date.

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Date", date.Date);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                numIncome.Value = reader.IsDBNull(0) ? 0 : reader.GetDecimal(0);
                                numAttendance.Value = reader.IsDBNull(1) ? 0 : Convert.ToDecimal(reader["Attendance"]); // Безопасное приведение
                                numElectricity.Value = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2);
                                txtNotes.Text = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveMetric()
        {
            // 1. Валидация
            if (dtpDate.Value.Date > DateTime.Today)
            {
                MessageBox.Show("Нельзя вносить данные за будущее число!", "Ошибка даты", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. SQL
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    // Используем MERGE для вставки или обновления
                    // ВНИМАНИЕ: Проверьте название столбца с датой в БД. Обычно это 'Date' или 'MetricDate'.
                    // Ниже я использую 'Date', как было в AnalyticsTab.
                    string sql = @"
                        MERGE ArenaMetrics AS target
                        USING (SELECT @Date AS MDate) AS source
                        ON (CONVERT(date, target.Date) = source.MDate)
                        WHEN MATCHED THEN
                            UPDATE SET 
                                Income = @Income, 
                                Attendance = @Attendance, 
                                Electricity = @Electricity, 
                                Notes = @Notes
                        WHEN NOT MATCHED THEN
                            INSERT (Date, Income, Attendance, Electricity, Notes)
                            VALUES (@Date, @Income, @Attendance, @Electricity, @Notes);";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Date", dtpDate.Value.Date);
                        cmd.Parameters.AddWithValue("@Income", numIncome.Value);
                        cmd.Parameters.AddWithValue("@Attendance", (int)numAttendance.Value);
                        cmd.Parameters.AddWithValue("@Electricity", numElectricity.Value);
                        cmd.Parameters.AddWithValue("@Notes", txtNotes.Text.Trim());

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearForm()
        {
            if (MessageBox.Show("Очистить все поля?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                numIncome.Value = 0;
                numAttendance.Value = 0;
                numElectricity.Value = 0;
                txtNotes.Clear();
            }
        }

        private void ShowInformation()
        {
            MessageBox.Show("Инструкция:\n1. Выберите дату (прошедшую или текущую).\n2. Введите доход и посещаемость.\n3. Нажмите Сохранить.\n\nДанные перезапишутся, если запись за эту дату уже существует.", "Справка", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AddFieldHints()
        {
            var tt = new ToolTip();
            tt.SetToolTip(numIncome, "Общая выручка в BYN");
            tt.SetToolTip(numAttendance, "Количество человек (билеты + абонементы)");
            tt.SetToolTip(numElectricity, "Показания счетчиков (разница)");
        }

        // Утилита для рисования скругленных прямоугольников
        private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.X + rect.Width - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.X + rect.Width - d, rect.Y + rect.Height - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}