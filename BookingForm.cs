using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IceArena.Client
{
    // Класс для передачи билета на сервер (строго соответствует ожиданиям сервера)
    public class TicketDto
    {
        public string Type { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    // Кастомная красивая кнопка
    public class ModernButton : Button
    {
        public int BorderRadius { get; set; } = 20;
        public Color HoverColor { get; set; }

        private Color originalColor;

        public ModernButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.Cursor = Cursors.Hand;
            this.Size = new Size(100, 40);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            this.MouseEnter += (s, e) => { originalColor = this.BackColor; this.BackColor = HoverColor; };
            this.MouseLeave += (s, e) => { this.BackColor = originalColor; };
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rectSurface = this.ClientRectangle;
            Rectangle rectBorder = Rectangle.Inflate(rectSurface, -1, -1);

            using (GraphicsPath pathSurface = GetFigurePath(rectSurface, BorderRadius))
            using (GraphicsPath pathBorder = GetFigurePath(rectBorder, BorderRadius))
            using (Pen penSurface = new Pen(this.Parent.BackColor, 2))
            using (Pen penBorder = new Pen(this.BackColor, 1))
            {
                this.Region = new Region(pathSurface);
                pevent.Graphics.DrawPath(penSurface, pathSurface);

                // Рисуем текст вручную для лучшего качества
                TextRenderer.DrawText(pevent.Graphics, this.Text, this.Font, rectSurface, this.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private GraphicsPath GetFigurePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float r = radius;
            path.StartFigure();
            path.AddArc(rect.X, rect.Y, r, r, 180, 90);
            path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
            path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
            path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public partial class BookingForm : Form
    {
        private const decimal ADULT_PRICE = 6.00m;
        private const decimal CHILD_PRICE = 4.00m;
        private const decimal SENIOR_PRICE = 4.00m;

        // Настройки подключения
        private const string SERVER_IP = "127.0.0.1";
        private const int SERVER_PORT = 8888;

        private ClientForm parentForm;
        private string date; // Формат yyyy-MM-dd
        private string timeSlot;
        private int userId;
        private int scheduleId;
        private int availableSeats;

        // Логические элементы (скрытые или используемые для расчетов)
        private NumericUpDown numAdult, numChild, numSenior;

        // UI элементы (новые)
        private Label lblTotalCount, lblTotalSum;
        private CheckBox chkSkates;
        private ComboBox cmbSkateSize, cmbSkateType;
        private ModernButton btnConfirm, btnCancel;

        public BookingForm(string day, string date, string time, ClientForm parent, int userId, object dbService, int scheduleId, int availableSeats)
        {
            this.date = date;
            this.timeSlot = time;
            this.parentForm = parent;
            this.userId = userId;
            this.scheduleId = scheduleId;
            this.availableSeats = availableSeats;

            // Инициализация "теневых" контролов для логики
            InitializeLogicControls();
            // Построение красивого интерфейса
            InitializeModernUI(day);
        }

        private void InitializeLogicControls()
        {
            // Мы используем стандартные NumericUpDown для логики, но прячем их от глаз,
            // управляя ими через красивые кнопки "+" и "-"
            numAdult = new NumericUpDown { Maximum = 100, Value = 0 };
            numChild = new NumericUpDown { Maximum = 100, Value = 0 };
            numSenior = new NumericUpDown { Maximum = 100, Value = 0 };

            numAdult.ValueChanged += UpdateTotals;
            numChild.ValueChanged += UpdateTotals;
            numSenior.ValueChanged += UpdateTotals;
        }

        private void InitializeModernUI(string dayOfWeek)
        {
            // 1. Настройки формы (УВЕЛИЧЕН РАЗМЕР для вместимости)
            this.Text = "Бронирование Ice Arena";
            // ИЗМЕНЕНИЕ: Ширина увеличена до 750, чтобы влезло длинное время
            this.Size = new Size(750, 850);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250); // Светло-серый современный фон

            int currentY = 20;

            // 2. Шапка (Header Panel)
            Panel headerPanel = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(this.Width - 60, 110),
                BackColor = Color.White,
            };
            // Эффект тени (простой рамкой)
            headerPanel.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, headerPanel.ClientRectangle, Color.FromArgb(230, 230, 230), ButtonBorderStyle.Solid);
            };

            Label lblTitle = new Label
            {
                Text = $"{dayOfWeek.ToUpper()}",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            Label lblDateVal = new Label
            {
                Text = $"{DateTime.Parse(date):dd MMMM yyyy}",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(20, 45)
            };

            // ИЗМЕНЕНИЕ: Сдвигаем точку начала времени левее (было -160, стало -240), 
            // чтобы длинный текст (10:00-10:45) не обрезался справа.
            Label lblTimeVal = new Label
            {
                Text = $"🕐 {timeSlot}",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 255), // Яркий синий
                AutoSize = true,
                Location = new Point(headerPanel.Width - 240, 25)
            };

            Label lblSeatsVal = new Label
            {
                Text = $"Мест: {availableSeats}",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(39, 174, 96), // Зеленый
                AutoSize = true,
                Location = new Point(headerPanel.Width - 240, 60)
            };

            headerPanel.Controls.AddRange(new Control[] { lblTitle, lblDateVal, lblTimeVal, lblSeatsVal });
            this.Controls.Add(headerPanel);
            currentY += 130;

            // 3. Секция выбора билетов (в белой панели)
            Panel ticketsPanel = new Panel
            {
                Location = new Point(20, currentY),
                Size = new Size(this.Width - 60, 260),
                BackColor = Color.White
            };
            ticketsPanel.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, ticketsPanel.ClientRectangle, Color.FromArgb(230, 230, 230), ButtonBorderStyle.Solid);

            Label lblSectionTickets = new Label
            {
                Text = "Количество билетов",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.Black,
                Location = new Point(20, 15),
                AutoSize = true
            };
            ticketsPanel.Controls.Add(lblSectionTickets);

            // Создаем ряды каунтеров (ширину ряда увеличиваем под ширину панели)
            int rowY = 50;
            int rowWidth = ticketsPanel.Width - 40;

            ticketsPanel.Controls.Add(CreateCounterRow("Взрослые (18-64)", $"{(int)ADULT_PRICE} BYN", numAdult, rowY, rowWidth));
            rowY += 65;
            ticketsPanel.Controls.Add(CreateCounterRow("Дети (до 17 лет)", $"{(int)CHILD_PRICE} BYN", numChild, rowY, rowWidth));
            rowY += 65;
            ticketsPanel.Controls.Add(CreateCounterRow("Пенсионеры (65+)", $"{(int)SENIOR_PRICE} BYN", numSenior, rowY, rowWidth));

            this.Controls.Add(ticketsPanel);
            currentY += 280;

            // 4. Секция проката (Skates)
            Panel skatesPanel = new Panel
            {
                Location = new Point(20, currentY),
                Size = new Size(this.Width - 60, 140),
                BackColor = Color.White
            };
            skatesPanel.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, skatesPanel.ClientRectangle, Color.FromArgb(230, 230, 230), ButtonBorderStyle.Solid);

            chkSkates = new CheckBox
            {
                Text = "⛸️ Добавить прокат коньков",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(20, 15),
                AutoSize = true,
                Cursor = Cursors.Hand
            };

            // Комбобоксы размеров и типов
            Label lblSize = new Label { Text = "Размер:", Location = new Point(20, 55), Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, ForeColor = Color.Gray };
            cmbSkateSize = new ComboBox { Location = new Point(20, 75), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false, Font = new Font("Segoe UI", 10), FlatStyle = FlatStyle.Flat, BackColor = Color.WhiteSmoke };
            for (int i = 30; i <= 46; i++) cmbSkateSize.Items.Add($"{i} размер");
            cmbSkateSize.SelectedIndex = 10; // 40

            Label lblType = new Label { Text = "Тип:", Location = new Point(230, 55), Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, ForeColor = Color.Gray };
            cmbSkateType = new ComboBox { Location = new Point(230, 75), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false, Font = new Font("Segoe UI", 10), FlatStyle = FlatStyle.Flat, BackColor = Color.WhiteSmoke };
            cmbSkateType.Items.AddRange(new string[] { "Фигурные", "Хоккейные" });
            cmbSkateType.SelectedIndex = 0;

            chkSkates.CheckedChanged += (s, e) =>
            {
                bool active = chkSkates.Checked;
                cmbSkateSize.Enabled = active;
                cmbSkateType.Enabled = active;
                cmbSkateSize.BackColor = active ? Color.White : Color.WhiteSmoke;
                cmbSkateType.BackColor = active ? Color.White : Color.WhiteSmoke;
            };

            skatesPanel.Controls.Add(chkSkates);
            skatesPanel.Controls.Add(lblSize);
            skatesPanel.Controls.Add(cmbSkateSize);
            skatesPanel.Controls.Add(lblType);
            skatesPanel.Controls.Add(cmbSkateType);

            this.Controls.Add(skatesPanel);
            currentY += 155;

            // 5. Итого и Кнопки
            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 130,
                BackColor = Color.White,
            };

            // Линия сверху
            bottomPanel.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.LightGray), 0, 0, bottomPanel.Width, 0);

            // Итого слева
            Label lblTotalText = new Label { Text = "ИТОГО:", Location = new Point(30, 20), Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Gray, AutoSize = true };
            lblTotalCount = new Label { Text = "0 билетов", Location = new Point(30, 40), Font = new Font("Segoe UI", 11), AutoSize = true };

            lblTotalSum = new Label
            {
                Text = "0.00 BYN",
                // ИЗМЕНЕНИЕ: Сдвигаем сумму левее, чтобы она не прилипала к правому краю
                Location = new Point(this.Width - 250, 25),
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 255),
                AutoSize = false,
                Size = new Size(220, 40),
                TextAlign = ContentAlignment.MiddleRight
            };

            // Кнопки
            btnCancel = new ModernButton
            {
                Text = "ОТМЕНА",
                BackColor = Color.FromArgb(231, 76, 60), // Красный
                HoverColor = Color.FromArgb(192, 57, 43),
                Location = new Point(30, 75),
                Size = new Size(150, 45),
                BorderRadius = 10
            };
            btnCancel.Click += (s, e) => this.Close();

            btnConfirm = new ModernButton
            {
                Text = "ОПЛАТИТЬ",
                BackColor = Color.FromArgb(46, 204, 113), // Зеленый
                HoverColor = Color.FromArgb(39, 174, 96),
                Location = new Point(200, 75),
                Size = new Size(this.Width - 240, 45),
                BorderRadius = 10
            };
            btnConfirm.Click += BtnConfirm_Click;

            bottomPanel.Controls.AddRange(new Control[] { lblTotalText, lblTotalCount, lblTotalSum, btnCancel, btnConfirm });
            this.Controls.Add(bottomPanel);
        }

        // Хелпер для создания строки каунтера (Текст, Цена, Связанный NuD, Y-координата)
        private Panel CreateCounterRow(string title, string price, NumericUpDown boundControl, int y, int width)
        {
            Panel p = new Panel { Size = new Size(width, 50), Location = new Point(20, y) };

            Label lblName = new Label { Text = title, Font = new Font("Segoe UI", 10), Location = new Point(0, 10), AutoSize = true };
            Label lblPrice = new Label { Text = price, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(0, 30), AutoSize = true };

            // Кнопки минус/плюс - прижимаем к правому краю
            int rightEdge = width - 10;

            Button btnPlus = new Button { Text = "+", Font = new Font("Segoe UI", 12, FontStyle.Bold), Size = new Size(35, 35), Location = new Point(rightEdge - 35, 5), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnPlus.FlatAppearance.BorderSize = 1;
            btnPlus.FlatAppearance.BorderColor = Color.LightGray;

            Label lblValue = new Label { Text = "0", Font = new Font("Segoe UI", 12, FontStyle.Bold), Size = new Size(40, 35), Location = new Point(rightEdge - 80, 5), TextAlign = ContentAlignment.MiddleCenter };

            Button btnMinus = new Button { Text = "−", Font = new Font("Segoe UI", 12, FontStyle.Bold), Size = new Size(35, 35), Location = new Point(rightEdge - 120, 5), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnMinus.FlatAppearance.BorderSize = 1;
            btnMinus.FlatAppearance.BorderColor = Color.LightGray;

            // Логика кнопок
            btnMinus.Click += (s, e) =>
            {
                if (boundControl.Value > boundControl.Minimum)
                {
                    boundControl.Value--;
                    lblValue.Text = boundControl.Value.ToString();
                }
            };

            btnPlus.Click += (s, e) =>
            {
                // Проверка общего лимита
                int currentTotal = (int)(numAdult.Value + numChild.Value + numSenior.Value);
                if (currentTotal < availableSeats && boundControl.Value < boundControl.Maximum)
                {
                    boundControl.Value++;
                    lblValue.Text = boundControl.Value.ToString();
                }
            };

            p.Controls.AddRange(new Control[] { lblName, lblPrice, btnMinus, lblValue, btnPlus });
            return p;
        }

        private void UpdateTotals(object sender, EventArgs e)
        {
            int count = (int)(numAdult.Value + numChild.Value + numSenior.Value);
            decimal sum = (numAdult.Value * ADULT_PRICE) +
                          (numChild.Value * CHILD_PRICE) +
                          (numSenior.Value * SENIOR_PRICE);

            lblTotalCount.Text = $"{count} билетов";
            lblTotalSum.Text = $"{sum:F2} BYN";
        }

        private async void BtnConfirm_Click(object sender, EventArgs e)
        {
            int totalTickets = (int)(numAdult.Value + numChild.Value + numSenior.Value);

            if (totalTickets == 0)
            {
                MessageBox.Show("Выберите хотя бы один билет!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnConfirm.Enabled = false;
            btnConfirm.Text = "Обработка...";

            try
            {
                // Формируем список билетов
                var ticketsList = new List<TicketDto>();

                // Добавляем инфо о коньках в тип билета, если выбрано
                string extraInfo = chkSkates.Checked ?
                    $" (+{cmbSkateType.SelectedItem}, {cmbSkateSize.SelectedItem})" : "";

                if (numAdult.Value > 0)
                    ticketsList.Add(new TicketDto { Type = "Adult" + extraInfo, Quantity = (int)numAdult.Value, Price = ADULT_PRICE });
                if (numChild.Value > 0)
                    ticketsList.Add(new TicketDto { Type = "Child" + extraInfo, Quantity = (int)numChild.Value, Price = CHILD_PRICE });
                if (numSenior.Value > 0)
                    ticketsList.Add(new TicketDto { Type = "Senior" + extraInfo, Quantity = (int)numSenior.Value, Price = SENIOR_PRICE });

                // Запрос к серверу
                var request = new
                {
                    Command = "create_booking",
                    UserId = this.userId,
                    ScheduleId = this.scheduleId,
                    Tickets = ticketsList
                };

                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(SERVER_IP, SERVER_PORT);
                    using (NetworkStream stream = client.GetStream())
                    {
                        string jsonRequest = JsonSerializer.Serialize(request);
                        byte[] data = Encoding.UTF8.GetBytes(jsonRequest);
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[4096];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string jsonResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                        {
                            if (doc.RootElement.TryGetProperty("Success", out var successElem) && successElem.GetBoolean())
                            {
                                MessageBox.Show("✅ Бронирование успешно создано!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                this.DialogResult = DialogResult.OK;
                                this.Close();
                            }
                            else
                            {
                                string error = doc.RootElement.TryGetProperty("Error", out var err) ?
                                    err.GetString() : "Неизвестная ошибка";
                                MessageBox.Show($"❌ Ошибка сервера:\n{error}", "Ошибка бронирования", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка соединения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnConfirm.Enabled = true;
                btnConfirm.Text = "ОПЛАТИТЬ";
            }
        }
    }
}