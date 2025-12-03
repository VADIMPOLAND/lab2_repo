using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Text.Json;
using System.IO;

namespace IceArena.Client
{
    public partial class ScheduleTab : UserControl
    {
        // Цветовая палитра
        private readonly Color PrimaryColor = Color.FromArgb(52, 152, 219); // Голубой
        private readonly Color HeaderColor = Color.FromArgb(41, 128, 185);  // Темно-голубой
        private readonly Color BackgroundColor = Color.FromArgb(236, 240, 241); // Светло-серый фон
        private readonly Color SuccessColor = Color.FromArgb(46, 204, 113); // Зеленый
        private readonly Color DangerColor = Color.FromArgb(231, 76, 60);   // Красный

        private DataGridView dgvSchedule;
        private Button btnAdd, btnEdit, btnDelete, btnRefresh;
        private Label lblTitle;
        private Panel panelSchedule, panelButtons;
        private bool isLoading = false;
        private readonly object loadingLock = new object();

        public ScheduleTab()
        {
            InitializeComponent();
            SetupUI();
            this.Load += async (s, e) => await LoadScheduleFromServer();
        }

        private void SetupUI()
        {
            this.BackColor = BackgroundColor;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.Padding = new Padding(20); // Общий отступ от краев формы

            // 1. Заголовок
            lblTitle = new Label
            {
                Text = "📅 УПРАВЛЕНИЕ РАСПИСАНИЕМ (ТЕКУЩАЯ НЕДЕЛЯ)",
                Dock = DockStyle.Top,
                Height = 60,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = HeaderColor,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            this.Controls.Add(lblTitle);

            // 2. Панель кнопок (СНИЗУ)
            // ИЗМЕНЕНИЕ: Увеличил высоту панели до 100, чтобы кнопки дышали
            panelButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            // Отрисовка верхней границы панели кнопок
            panelButtons.Paint += (s, e) => {
                ControlPaint.DrawBorder(e.Graphics, panelButtons.ClientRectangle,
                    Color.LightGray, 0, ButtonBorderStyle.None,
                    Color.LightGray, 1, ButtonBorderStyle.Solid, // Линия сверху
                    Color.LightGray, 0, ButtonBorderStyle.None,
                    Color.LightGray, 0, ButtonBorderStyle.None);
            };
            this.Controls.Add(panelButtons);
            SetupButtons();

            // 3. Панель с таблицей (ПО ЦЕНТРУ)
            panelSchedule = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(15)
            };
            this.Controls.Add(panelSchedule);

            // Важно: BringToFront, чтобы центральная панель заняла всё свободное место между заголовком и кнопками
            panelSchedule.BringToFront();

            SetupScheduleGrid();
        }

        private void SetupButtons()
        {
            FlowLayoutPanel flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                // ИЗМЕНЕНИЕ: Отступ сверху 25px центрирует кнопку высотой 50px в панели высотой 100px
                Padding = new Padding(20, 25, 0, 0),
                AutoSize = false
            };

            btnAdd = CreateStyledButton("➕ ДОБАВИТЬ", SuccessColor);
            btnAdd.Click += async (s, e) => await AddScheduleAsync();

            btnEdit = CreateStyledButton("✏️ ИЗМЕНИТЬ", PrimaryColor);
            btnEdit.Click += async (s, e) => await EditScheduleAsync();

            btnDelete = CreateStyledButton("❌ УДАЛИТЬ", DangerColor);
            btnDelete.Click += async (s, e) => await DeleteScheduleAsync();

            btnRefresh = CreateStyledButton("🔄 ОБНОВИТЬ", Color.FromArgb(142, 68, 173));
            btnRefresh.Click += async (s, e) => await LoadScheduleFromServer();

            flowPanel.Controls.Add(btnAdd);
            flowPanel.Controls.Add(btnEdit);
            flowPanel.Controls.Add(btnDelete);
            flowPanel.Controls.Add(btnRefresh);

            panelButtons.Controls.Add(flowPanel);
        }

        private Button CreateStyledButton(string text, Color baseColor)
        {
            Button btn = new Button
            {
                Text = text,
                Size = new Size(200, 50), // Размер кнопки
                BackColor = baseColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 20, 0) // Расстояние между кнопками
            };

            btn.FlatAppearance.BorderSize = 0;

            // Hover эффект
            btn.MouseEnter += (s, e) => btn.BackColor = ControlPaint.Light(baseColor);
            btn.MouseLeave += (s, e) => btn.BackColor = baseColor;

            return btn;
        }

        private void SetupScheduleGrid()
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
                Font = new Font("Segoe UI", 11),
                RowTemplate = { Height = 50 },
                GridColor = Color.FromArgb(230, 230, 230)
            };

            dgvSchedule.EnableHeadersVisualStyles = false;
            dgvSchedule.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 11F);
            dgvSchedule.ColumnHeadersDefaultCellStyle.BackColor = HeaderColor;
            dgvSchedule.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvSchedule.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvSchedule.ColumnHeadersHeight = 55;
            dgvSchedule.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            dgvSchedule.DefaultCellStyle.SelectionBackColor = Color.FromArgb(214, 234, 248);
            dgvSchedule.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvSchedule.DefaultCellStyle.Padding = new Padding(5);
            dgvSchedule.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgvSchedule.Columns.Add("Id", "ID");
            dgvSchedule.Columns.Add("Day", "ДЕНЬ НЕДЕЛИ");
            dgvSchedule.Columns.Add("Date", "ДАТА");
            dgvSchedule.Columns.Add("TimeSlot", "ВРЕМЯ");
            dgvSchedule.Columns.Add("BreakSlot", "ПЕРЕРЫВ");
            dgvSchedule.Columns.Add("Capacity", "МЕСТ");
            dgvSchedule.Columns.Add("AvailableSeats", "СВОБОДНО");
            dgvSchedule.Columns.Add("Status", "СТАТУС");

            dgvSchedule.Columns["Id"].Visible = false;
            dgvSchedule.Columns["Day"].FillWeight = 15;
            dgvSchedule.Columns["Date"].FillWeight = 12;
            dgvSchedule.Columns["TimeSlot"].FillWeight = 15;
            dgvSchedule.Columns["BreakSlot"].FillWeight = 15;
            dgvSchedule.Columns["Capacity"].FillWeight = 10;
            dgvSchedule.Columns["AvailableSeats"].FillWeight = 10;
            dgvSchedule.Columns["Status"].FillWeight = 15;

            dgvSchedule.CellFormatting += DgvSchedule_CellFormatting;

            panelSchedule.Controls.Add(dgvSchedule);
        }

        private void DgvSchedule_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Статус (цветной фон)
            if (dgvSchedule.Columns[e.ColumnIndex].Name == "Status")
            {
                string status = e.Value?.ToString() ?? "";
                if (status == "ДОСТУПНО")
                {
                    e.CellStyle.BackColor = Color.FromArgb(220, 255, 220);
                    e.CellStyle.ForeColor = Color.DarkGreen;
                    e.CellStyle.SelectionForeColor = Color.DarkGreen;
                }
                else
                {
                    e.CellStyle.BackColor = Color.FromArgb(255, 225, 225);
                    e.CellStyle.ForeColor = Color.DarkRed;
                    e.CellStyle.SelectionForeColor = Color.DarkRed;
                }
                e.CellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            }

            // Объединение ячеек (скрытие текста повторяющейся даты)
            if (e.RowIndex > 0)
            {
                var colName = dgvSchedule.Columns[e.ColumnIndex].Name;
                if (colName == "Day" || colName == "Date")
                {
                    var currentValue = e.Value?.ToString();
                    var prevValue = dgvSchedule.Rows[e.RowIndex - 1].Cells[colName].Value?.ToString();

                    if (currentValue == prevValue)
                    {
                        e.Value = "";
                        e.FormattingApplied = true;
                    }
                }
            }
        }

        // --- ЛОГИКА (БЕЗ ИЗМЕНЕНИЙ, НО С ИСПРАВЛЕНИЕМ ОШИБКИ ДАТЫ) ---

        private async Task LoadScheduleFromServer()
        {
            lock (loadingLock) { if (isLoading) return; isLoading = true; }

            try
            {
                this.Cursor = Cursors.WaitCursor;
                dgvSchedule.Rows.Clear();

                DateTime currentDateTime = DateTime.Now;
                DateTime startDate = currentDateTime.Date.AddDays(-(int)currentDateTime.DayOfWeek + (int)DayOfWeek.Monday);
                DateTime endDate = startDate.AddDays(6);

                var scheduleData = await SendServerRequest(new { Command = "get_schedule" });

                if (scheduleData.ValueKind == JsonValueKind.Object &&
                    scheduleData.TryGetProperty("Success", out var successElement) &&
                    successElement.GetBoolean())
                {
                    if (scheduleData.TryGetProperty("Schedule", out var scheduleArray) &&
                        scheduleArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in scheduleArray.EnumerateArray().OrderBy(i => DateTime.Parse(i.GetProperty("Date").GetString())))
                        {
                            try
                            {
                                int id = item.GetProperty("Id").GetInt32();
                                DateTime slotDate = DateTime.Parse(item.GetProperty("Date").GetString());
                                string dayName = item.TryGetProperty("DayOfWeek", out var dayElement) ? dayElement.GetString() : slotDate.DayOfWeek.ToString();
                                string dateStr = slotDate.ToString("dd.MM.yyyy");
                                string timeSlot = item.GetProperty("TimeSlot").GetString();
                                string breakSlot = item.TryGetProperty("BreakSlot", out var breakElement) ? breakElement.GetString() : "45 мин";
                                int capacity = item.TryGetProperty("Capacity", out var capacityElement) ? capacityElement.GetInt32() : 50;
                                int availableSeats = item.TryGetProperty("AvailableSeats", out var seatsElement) ? seatsElement.GetInt32() : capacity;
                                string status = item.TryGetProperty("Status", out var statusElement) ? statusElement.GetString() : "ДОСТУПНО";

                                if (slotDate < startDate || slotDate > endDate) continue;

                                dgvSchedule.Rows.Add(id, dayName.ToUpper(), dateStr, timeSlot, breakSlot, capacity, availableSeats, status);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Ошибка: {ex.Message}");
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Ошибка загрузки данных.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                lock (loadingLock) { isLoading = false; }
                this.Cursor = Cursors.Default;
                dgvSchedule.ClearSelection();
            }
        }

        private async Task AddScheduleAsync()
        {
            using (Form addForm = CreateStyledEditForm("Добавить расписание"))
            {
                if (addForm.ShowDialog() == DialogResult.OK)
                {
                    var dtpDate = (DateTimePicker)addForm.Controls.Find("dtpDate", true)[0];
                    var txtTimeSlot = (TextBox)addForm.Controls.Find("txtTimeSlot", true)[0];
                    var txtBreakSlot = (TextBox)addForm.Controls.Find("txtBreakSlot", true)[0];
                    var txtCapacity = (TextBox)addForm.Controls.Find("txtCapacity", true)[0];
                    var cmbStatus = (ComboBox)addForm.Controls.Find("cmbStatus", true)[0];

                    if (!int.TryParse(txtCapacity.Text, out int capacity))
                    {
                        MessageBox.Show("Неверный формат емкости.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var request = new
                    {
                        Command = "add_schedule",
                        Date = dtpDate.Value.ToString("yyyy-MM-dd"),
                        TimeSlot = txtTimeSlot.Text,
                        BreakSlot = txtBreakSlot.Text,
                        Capacity = capacity,
                        Status = cmbStatus.SelectedItem.ToString()
                    };

                    var response = await SendServerRequest(request);
                    if (response.TryGetProperty("Success", out var success) && success.GetBoolean())
                    {
                        await LoadScheduleFromServer();
                        MessageBox.Show("Успешно добавлено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Ошибка при добавлении.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async Task EditScheduleAsync()
        {
            if (dgvSchedule.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите запись для редактирования.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedRow = dgvSchedule.SelectedRows[0];
            int id = Convert.ToInt32(selectedRow.Cells["Id"].Value);
            string dateString = selectedRow.Cells["Date"].Value.ToString();

            if (!DateTime.TryParse(dateString, out DateTime date))
            {
                MessageBox.Show("Ошибка чтения даты.", "Ошибка");
                return;
            }

            string timeSlot = selectedRow.Cells["TimeSlot"].Value.ToString();
            string breakSlot = selectedRow.Cells["BreakSlot"].Value.ToString();
            int capacity = Convert.ToInt32(selectedRow.Cells["Capacity"].Value);
            string status = selectedRow.Cells["Status"].Value.ToString();

            using (Form editForm = CreateStyledEditForm("Редактировать", date, timeSlot, breakSlot, capacity, status))
            {
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    var dtpDate = (DateTimePicker)editForm.Controls.Find("dtpDate", true)[0];
                    var txtTimeSlot = (TextBox)editForm.Controls.Find("txtTimeSlot", true)[0];
                    var txtBreakSlot = (TextBox)editForm.Controls.Find("txtBreakSlot", true)[0];
                    var txtCapacity = (TextBox)editForm.Controls.Find("txtCapacity", true)[0];
                    var cmbStatus = (ComboBox)editForm.Controls.Find("cmbStatus", true)[0];

                    if (!int.TryParse(txtCapacity.Text, out int newCapacity)) return;

                    var request = new
                    {
                        Command = "update_schedule",
                        Id = id,
                        Date = dtpDate.Value.ToString("yyyy-MM-dd"),
                        TimeSlot = txtTimeSlot.Text,
                        BreakSlot = txtBreakSlot.Text,
                        Capacity = newCapacity,
                        Status = cmbStatus.SelectedItem.ToString()
                    };

                    var response = await SendServerRequest(request);
                    if (response.TryGetProperty("Success", out var success) && success.GetBoolean())
                    {
                        await LoadScheduleFromServer();
                        MessageBox.Show("Успешно обновлено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private Form CreateStyledEditForm(string title, DateTime? initialDate = null, string initialTimeSlot = "", string initialBreakSlot = "", int initialCapacity = 50, string initialStatus = "ДОСТУПНО")
        {
            Form form = new Form
            {
                Text = title,
                Size = new Size(450, 420),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F)
            };

            Label lblHeader = new Label
            {
                Text = title.ToUpper(),
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = HeaderColor,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            };
            form.Controls.Add(lblHeader);

            int startY = 70;
            int gap = 50;
            int lblX = 30;
            int ctrlX = 150;

            void AddField(string labelText, Control ctrl, int y)
            {
                Label lbl = new Label { Text = labelText, Location = new Point(lblX, y + 3), AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
                ctrl.Location = new Point(ctrlX, y);
                ctrl.Width = 250;
                form.Controls.Add(lbl);
                form.Controls.Add(ctrl);
            }

            DateTimePicker dtpDate = new DateTimePicker { Name = "dtpDate", Value = initialDate ?? DateTime.Now, Format = DateTimePickerFormat.Short };
            AddField("Дата:", dtpDate, startY);

            TextBox txtTimeSlot = new TextBox { Name = "txtTimeSlot", Text = initialTimeSlot };
            AddField("Время:", txtTimeSlot, startY + gap);

            TextBox txtBreakSlot = new TextBox { Name = "txtBreakSlot", Text = initialBreakSlot };
            AddField("Перерыв:", txtBreakSlot, startY + gap * 2);

            TextBox txtCapacity = new TextBox { Name = "txtCapacity", Text = initialCapacity.ToString() };
            AddField("Емкость:", txtCapacity, startY + gap * 3);

            ComboBox cmbStatus = new ComboBox { Name = "cmbStatus", DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new string[] { "ДОСТУПНО", "НЕДОСТУПНО" });
            cmbStatus.SelectedItem = initialStatus;
            AddField("Статус:", cmbStatus, startY + gap * 4);

            Button btnOk = new Button { Text = "СОХРАНИТЬ", DialogResult = DialogResult.OK, Location = new Point(50, 330), Size = new Size(160, 40), BackColor = SuccessColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            Button btnCancel = new Button { Text = "ОТМЕНА", DialogResult = DialogResult.Cancel, Location = new Point(230, 330), Size = new Size(160, 40), BackColor = Color.LightGray, ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11, FontStyle.Bold) };

            form.Controls.Add(btnOk);
            form.Controls.Add(btnCancel);
            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            return form;
        }

        private async Task DeleteScheduleAsync()
        {
            if (dgvSchedule.SelectedRows.Count == 0) return;
            if (MessageBox.Show("Удалить запись?", "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                int id = Convert.ToInt32(dgvSchedule.SelectedRows[0].Cells["Id"].Value);
                var response = await SendServerRequest(new { Command = "delete_schedule", Id = id });
                if (response.TryGetProperty("Success", out var success) && success.GetBoolean()) await LoadScheduleFromServer();
            }
        }

        public async Task<JsonElement> SendServerRequest(object request)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync("127.0.0.1", 8888);
                    if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask) throw new TimeoutException();

                    using (var stream = client.GetStream())
                    {
                        byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[8192];
                        using (var ms = new MemoryStream())
                        {
                            int bytesRead;
                            do
                            {
                                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                                ms.Write(buffer, 0, bytesRead);
                            } while (stream.DataAvailable);

                            string json = Encoding.UTF8.GetString(ms.ToArray()).Trim();
                            if (string.IsNullOrEmpty(json)) throw new Exception();
                            return JsonSerializer.Deserialize<JsonElement>(json);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка сервера: " + ex.Message);
            }
        }
    }
}