using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ZedGraph;
using WfLabel = System.Windows.Forms.Label;

namespace IceArena.Client
{
    public partial class AnalyticsTab : UserControl
    {
        private DataGridView dgvMetrics;
        private ZedGraphControl zedGraph;
        private ComboBox cmbReportType, cmbPeriod;
        private Button btnGenerate, btnExport, btnAddMetric, btnEditMetric, btnDeleteMetric, btnRefresh;
        private Panel quickStatsPanel;
        private SplitContainer contentSplit;

        private const string ConnectionString = "Server=DESKTOP-I80K0OH\\SQLEXPRESS;Database=Ice_Arena;Trusted_Connection=true;TrustServerCertificate=true;";

        // Цветовая схема
        private readonly Color Primary = Color.FromArgb(0, 120, 215);
        private readonly Color Secondary = Color.FromArgb(70, 170, 255);
        private readonly Color Success = Color.FromArgb(46, 204, 113);
        private readonly Color Warning = Color.FromArgb(241, 196, 15);
        private readonly Color Danger = Color.FromArgb(231, 76, 60);
        private readonly Color Info = Color.FromArgb(155, 89, 182);
        private readonly Color Dark = Color.FromArgb(44, 62, 80);
        private readonly Color LightBg = Color.FromArgb(248, 250, 252);
        private readonly Color CardBg = Color.White;
        private readonly Color Border = Color.FromArgb(225, 235, 245);

        private bool splitterAdjusted = false;

        // Ссылки на метки статистики
        private WfLabel lblIncomeToday, lblAvgAttendance, lblTotalEnergy, lblTotalRecords;

        public AnalyticsTab()
        {
            this.DoubleBuffered = true;
            this.BackColor = LightBg;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(20);

            InitializeComponents();
            SetupComponents();

            // Кастомная инициализация
            this.Load += (s, e) => { if (IsHandleCreated) BeginInvoke(new Action(SafeAdjustSplitter)); };
            this.SizeChanged += (s, e) => { if (IsHandleCreated) BeginInvoke(new Action(SafeAdjustSplitter)); };
            this.VisibleChanged += (s, e) => { if (this.Visible && IsHandleCreated) BeginInvoke(new Action(SafeAdjustSplitter)); };

            LoadMetrics();
            UpdateQuickStats();
            GenerateReport();
        }

        // Пустой метод, если он нужен для дизайнера
        private void InitializeComponents() { }

        private void SetupComponents()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                Padding = new Padding(0)
            };

            // Настройка строк макета
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));

            this.Controls.Add(mainLayout);

            // 1. Панель управления (Верх)
            mainLayout.Controls.Add(CreateControlsPanel(), 0, 0);

            // 2. SplitContainer: таблица + график
            contentSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 10,
                BackColor = LightBg
            };
            mainLayout.Controls.Add(contentSplit, 0, 1);

            // Левая часть — Таблица
            var tableCard = CreateCard();
            contentSplit.Panel1.Controls.Add(tableCard);

            var tableLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(15), RowCount = 2 };
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableCard.Controls.Add(tableLayout);

            tableLayout.Controls.Add(new WfLabel
            {
                Text = "МЕТРИКИ",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Dark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            dgvMetrics = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9.5F),
                GridColor = Border,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 35 }
            };

            dgvMetrics.Columns.AddRange(new DataGridViewTextBoxColumn[]
            {
                new() { HeaderText = "ID", DataPropertyName = "Id", FillWeight = 8 },
                new() { HeaderText = "ДАТА", DataPropertyName = "Date", FillWeight = 18 },
                new() { HeaderText = "ДОХОД", DataPropertyName = "Income", FillWeight = 18 },
                new() { HeaderText = "ПОСЕЩАЕМОСТЬ", DataPropertyName = "Attendance", FillWeight = 20 },
                new() { HeaderText = "ЭЛЕКТРИЧЕСТВО", DataPropertyName = "Electricity", FillWeight = 20 },
                new() { HeaderText = "ПРИМЕЧАНИЯ", DataPropertyName = "Notes", FillWeight = 40 }
            });

            dgvMetrics.ColumnHeadersHeight = 40;
            dgvMetrics.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            dgvMetrics.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgvMetrics.EnableHeadersVisualStyles = false;
            dgvMetrics.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditSelectedMetric(); };

            tableLayout.Controls.Add(dgvMetrics, 0, 1);

            // Правая часть — График
            var graphCard = CreateCard();
            contentSplit.Panel2.Controls.Add(graphCard);

            var graphLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(15), RowCount = 2 };
            graphLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            graphLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            graphCard.Controls.Add(graphLayout);

            graphLayout.Controls.Add(new WfLabel
            {
                Text = "ГРАФИК",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Dark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            zedGraph = new ZedGraphControl { Dock = DockStyle.Fill };
            zedGraph.GraphPane.Fill = new Fill(Color.White);
            zedGraph.GraphPane.Chart.Fill = new Fill(Color.White);
            zedGraph.GraphPane.Border.IsVisible = false;
            zedGraph.GraphPane.Title.FontSpec.Size = 12;
            graphLayout.Controls.Add(zedGraph, 0, 1);

            // 3. Панель быстрой статистики (Низ)
            quickStatsPanel = CreateStatsPanel();
            mainLayout.Controls.Add(quickStatsPanel, 0, 2);
        }

        private Panel CreateControlsPanel()
        {
            var panel = CreateCard();
            var layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
            };

            void AddControl(Control c, int marginTop = 5)
            {
                c.Margin = new Padding(0, marginTop, 10, 5);
                layout.Controls.Add(c);
            }

            var lblType = new WfLabel { Text = "ТИП:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Dark, AutoSize = true };
            AddControl(lblType, 10);

            cmbReportType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), Width = 180 };
            cmbReportType.Items.AddRange(new[] { "Посещаемость", "Доход", "Энергопотребление" });
            cmbReportType.SelectedIndex = 0;
            AddControl(cmbReportType);

            var lblPeriod = new WfLabel { Text = "ПЕРИОД:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Dark, AutoSize = true };
            AddControl(lblPeriod, 10);

            cmbPeriod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), Width = 140 };
            cmbPeriod.Items.AddRange(new[] { "1 день", "3 дня", "Неделя", "Месяц", "Все данные" });
            cmbPeriod.SelectedIndex = 2;
            AddControl(cmbPeriod);

            btnGenerate = CreateFlatButton("ГЕНЕРИРОВАТЬ", Primary, 140, 35);
            btnGenerate.Click += (s, e) => GenerateReport();
            AddControl(btnGenerate, 2);

            btnExport = CreateFlatButton("ЭКСПОРТ", Secondary, 100, 35);
            btnExport.Click += (s, e) => ExportReport();
            AddControl(btnExport, 2);

            btnRefresh = CreateFlatButton("ОБНОВИТЬ", Color.FromArgb(149, 165, 166), 100, 35);
            btnRefresh.Click += (s, e) => { LoadMetrics(); UpdateQuickStats(); GenerateReport(); };
            AddControl(btnRefresh, 2);

            btnAddMetric = CreateFlatButton("ДОБАВИТЬ", Success, 100, 35);
            btnAddMetric.Click += (s, e) => AddMetric();
            AddControl(btnAddMetric, 2);

            btnEditMetric = CreateFlatButton("РЕДАКТИРОВАТЬ", Warning, 140, 35);
            btnEditMetric.Click += (s, e) => EditSelectedMetric();
            AddControl(btnEditMetric, 2);

            btnDeleteMetric = CreateFlatButton("УДАЛИТЬ", Danger, 100, 35);
            btnDeleteMetric.Click += (s, e) => DeleteSelectedMetric();
            AddControl(btnDeleteMetric, 2);

            panel.Controls.Add(layout);
            return panel;
        }

        private Panel CreateStatsPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = LightBg, Padding = new Padding(0, 10, 0, 0) };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Padding = new Padding(0) };

            for (int i = 0; i < 4; i++)
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            layout.Controls.Add(CreateStatCard("СЕГОДНЯШНИЙ ДОХОД", "0 BYN", Primary, out lblIncomeToday), 0, 0);
            layout.Controls.Add(CreateStatCard("СРЕДНЯЯ ПОСЕЩАЕМОСТЬ", "0 ЧЕЛ.", Success, out lblAvgAttendance), 1, 0);
            layout.Controls.Add(CreateStatCard("ЭНЕРГОПОТРЕБЛЕНИЕ", "0 КВТ", Info, out lblTotalEnergy), 2, 0);
            layout.Controls.Add(CreateStatCard("ВСЕГО ЗАПИСЕЙ", "0", Warning, out lblTotalRecords), 3, 0);

            panel.Controls.Add(layout);
            return panel;
        }

        private Panel CreateStatCard(string title, string value, Color color, out WfLabel valueLabel)
        {
            var outerPanel = CreateCard();
            outerPanel.Padding = new Padding(0);

            var innerPanel = new Panel { Dock = DockStyle.Fill, BackColor = color };

            var titleLbl = new WfLabel
            {
                Text = title,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };

            valueLabel = new WfLabel
            {
                Text = value,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            innerPanel.Controls.Add(valueLabel);
            innerPanel.Controls.Add(titleLbl);
            titleLbl.BringToFront();

            outerPanel.Controls.Add(innerPanel);
            return outerPanel;
        }

        private Panel CreateCard() => new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg,
            Margin = new Padding(5),
            Padding = new Padding(5)
        };

        private Button CreateFlatButton(string text, Color color, int width, int height)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(width, height),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(5)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(color, 0.2f);
            return btn;
        }

        private void SafeAdjustSplitter()
        {
            if (contentSplit == null || contentSplit.Width <= 0 || !contentSplit.IsHandleCreated || !this.IsHandleCreated) return;
            try
            {
                if (!splitterAdjusted)
                {
                    contentSplit.SplitterDistance = contentSplit.Width / 2;
                    splitterAdjusted = true;
                }
            }
            catch { }
        }

        private void LoadMetrics() => ExecuteSafe(() =>
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            var dt = new DataTable();
            new SqlDataAdapter(
                "SELECT Id, CONVERT(varchar, Date, 104) AS Date, Income, Attendance, Electricity, Notes FROM ArenaMetrics ORDER BY Date ASC",
                conn).Fill(dt);
            dgvMetrics.DataSource = dt;
        });

        private void UpdateQuickStats() => ExecuteSafe(() =>
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT ISNULL(SUM(Income), 0) FROM ArenaMetrics WHERE CONVERT(date, Date) = CONVERT(date, GETDATE())";
            lblIncomeToday.Text = $"{cmd.ExecuteScalar():0} BYN";

            cmd.CommandText = "SELECT ISNULL(AVG(Attendance), 0) FROM ArenaMetrics";
            lblAvgAttendance.Text = $"{cmd.ExecuteScalar():0} ЧЕЛ.";

            cmd.CommandText = "SELECT ISNULL(SUM(Electricity), 0) FROM ArenaMetrics";
            lblTotalEnergy.Text = $"{cmd.ExecuteScalar():0} КВТ";

            cmd.CommandText = "SELECT COUNT(*) FROM ArenaMetrics";
            lblTotalRecords.Text = cmd.ExecuteScalar().ToString();
        });

        private void GenerateReport()
        {
            string reportType = cmbReportType.SelectedItem?.ToString() ?? "Посещаемость";
            string period = cmbPeriod.SelectedItem?.ToString() ?? "Неделя";
            var data = GetMetricsData(period, reportType);
            dgvMetrics.DataSource = data;
            DrawGraph(reportType, data, period);
        }

        private DataTable GetMetricsData(string period, string reportType)
        {
            var dt = new DataTable();
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            string dateFilter = GetDateFilter(period);
            string sql = $@"
                SELECT Id, CONVERT(varchar, Date, 104) AS Date, Income, Attendance, Electricity, Notes
                FROM ArenaMetrics
                WHERE {dateFilter}
                ORDER BY Date ASC";
            using var adapter = new SqlDataAdapter(sql, conn);
            adapter.Fill(dt);
            return dt;
        }

        private string GetDateFilter(string period)
        {
            return period switch
            {
                "1 день" => "CONVERT(date, Date) = CONVERT(date, GETDATE())",
                "3 дня" => "Date >= DATEADD(day, -2, CAST(GETDATE() AS date)) AND Date <= CAST(GETDATE() AS date)",
                "Неделя" => "Date >= DATEADD(day, -6, CAST(GETDATE() AS date)) AND Date <= CAST(GETDATE() AS date)",
                "Месяц" => "Date >= DATEADD(day, -30, CAST(GETDATE() AS date)) AND Date <= CAST(GETDATE() AS date)",
                "Все данные" => "1=1",
                _ => "Date >= DATEADD(day, -6, CAST(GETDATE() AS date)) AND Date <= CAST(GETDATE() AS date)"
            };
        }

        private void DrawGraph(string reportType, DataTable data, string period)
        {
            var pane = zedGraph.GraphPane;
            pane.CurveList.Clear();
            pane.Title.Text = $"{reportType} — {period}";
            pane.XAxis.Title.Text = "Дата";
            pane.YAxis.Title.Text = GetYAxisTitle(reportType);
            pane.Margin.All = 10;
            pane.Title.FontSpec.Size = 14;

            if (data.Rows.Count == 0)
            {
                pane.Title.Text = "НЕТ ДАННЫХ ЗА ПЕРИОД";
                zedGraph.AxisChange();
                zedGraph.Refresh();
                return;
            }

            var list = new PointPairList();
            string[] labels = new string[data.Rows.Count];
            string column = reportType switch
            {
                "Посещаемость" => "Attendance",
                "Доход" => "Income",
                "Энергопотребление" => "Electricity",
                _ => "Attendance"
            };

            for (int i = 0; i < data.Rows.Count; i++)
            {
                double y = 0;
                if (data.Rows[i][column] != DBNull.Value)
                    double.TryParse(data.Rows[i][column].ToString(), out y);

                list.Add(i, y);
                labels[i] = data.Rows[i]["Date"].ToString();
            }

            Color lineColor = reportType switch
            {
                "Посещаемость" => Primary,
                "Доход" => Success,
                "Энергопотребление" => Info,
                _ => Primary
            };

            var curve = pane.AddCurve("", list, lineColor, SymbolType.Circle);
            curve.Line.IsVisible = true;
            curve.Line.Width = 3f;
            curve.Line.IsAntiAlias = true;
            curve.Symbol.Size = 8;
            curve.Symbol.Fill = new Fill(Color.White);
            curve.Symbol.Border.Width = 2;

            pane.XAxis.Scale.TextLabels = labels;
            pane.XAxis.Type = AxisType.Text;

            if (data.Rows.Count <= 10) pane.XAxis.Scale.MajorStep = 1;
            pane.XAxis.Scale.FontSpec.Angle = data.Rows.Count > 8 ? 45 : 0;
            pane.XAxis.Scale.FontSpec.Size = 10;
            pane.XAxis.MajorGrid.IsVisible = true;
            pane.YAxis.MajorGrid.IsVisible = true;
            pane.XAxis.MajorGrid.Color = pane.YAxis.MajorGrid.Color = Border;

            zedGraph.AxisChange();
            zedGraph.Refresh();
        }

        private string GetYAxisTitle(string reportType)
        {
            return reportType switch
            {
                "Посещаемость" => "Человек",
                "Доход" => "BYN",
                "Энергопотребление" => "кВт·ч",
                _ => "Значение"
            };
        }

        private void ExportReport()
        {
            if (dgvMetrics.Rows.Count == 0 || dgvMetrics.DataSource == null)
            {
                MessageBox.Show("Нет данных для экспорта.");
                return;
            }

            string reportType = cmbReportType.SelectedItem?.ToString() ?? "Отчет";
            string period = cmbPeriod.SelectedItem?.ToString() ?? "Период";

            var sfd = new SaveFileDialog
            {
                Filter = "CSV файл (*.csv)|*.csv|Текстовый файл (*.txt)|*.txt",
                FileName = $"Отчет_{reportType}_{DateTime.Now:yyyy-MM-dd}.csv"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Id;Дата;Доход;Посещаемость;Электричество;Примечания");

                foreach (DataGridViewRow row in dgvMetrics.Rows)
                {
                    if (!row.IsNewRow)
                    {
                        string id = row.Cells[0].Value?.ToString() ?? "";
                        string date = row.Cells[1].Value?.ToString() ?? "";
                        string income = row.Cells[2].Value?.ToString() ?? "0";
                        string attendance = row.Cells[3].Value?.ToString() ?? "0";
                        string electricity = row.Cells[4].Value?.ToString() ?? "0";
                        string notes = row.Cells[5].Value?.ToString() ?? "";
                        notes = notes.Replace(";", ",");
                        sb.AppendLine($"{id};{date};{income};{attendance};{electricity};{notes}");
                    }
                }

                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("Отчёт успешно экспортирован!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при экспорте: " + ex.Message);
            }
        }

        // --------------------------------------------------------------------------------------
        // ИЗМЕНЕНИЯ ЗДЕСЬ: Подключение формы AddEditMetricForm
        // --------------------------------------------------------------------------------------

        private void AddMetric()
        {
            // Открываем форму БЕЗ даты (режим добавления)
            using (var form = new AddEditMetricForm())
            {
                // Если пользователь нажал "Сохранить" и все прошло успешно
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // Обновляем данные на экране
                    LoadMetrics();
                    UpdateQuickStats();
                    GenerateReport();
                }
            }
        }

        private void EditSelectedMetric()
        {
            if (dgvMetrics.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите строку для редактирования.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Получаем дату из выбранной строки (столбец индекс 1 - "Date")
            var dateString = dgvMetrics.SelectedRows[0].Cells[1].Value?.ToString();

            if (DateTime.TryParse(dateString, out DateTime selectedDate))
            {
                // Открываем форму С датой (режим редактирования)
                // Ошибка CS7014 была исправлена здесь удалением лишних меток цитирования
                using (var form = new AddEditMetricForm(selectedDate))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        LoadMetrics();
                        UpdateQuickStats();
                        GenerateReport();
                    }
                }
            }
            else
            {
                MessageBox.Show("Не удалось прочитать дату выбранной записи.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteSelectedMetric()
        {
            if (dgvMetrics.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите строку для удаления.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Вы действительно хотите удалить выбранную запись?\nЭто действие нельзя отменить.", "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    var id = dgvMetrics.SelectedRows[0].Cells[0].Value;
                    using var conn = new SqlConnection(ConnectionString);
                    conn.Open();
                    var cmd = new SqlCommand("DELETE FROM ArenaMetrics WHERE Id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();

                    LoadMetrics();
                    UpdateQuickStats();
                    GenerateReport();

                    MessageBox.Show("Запись успешно удалена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка удаления: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExecuteSafe(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка выполнения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}