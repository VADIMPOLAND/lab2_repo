using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO; // Добавлено для MemoryStream

namespace IceArena.Client
{
    public partial class AdminForm : Form
    {
        private TabControl tabControl;
        private UsersTab usersTab;
        private AnalyticsTab analyticsTab;
        private BookingsTab bookingsTab;
        private ScheduleTab scheduleTab;
        private SupportTab supportTab;
        private Panel panelHeader;
        private Panel panelFooter;
        private Button btnExit;
        private Panel mainContentPanel;

        public AdminForm()
        {
            InitializeForm();
            SetupUI();
        }

        private void InitializeForm()
        {
            this.Text = "🎯 ПАНЕЛЬ АДМИНИСТРАТОРА - ЛЕДОВАЯ АРЕНА";
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 248, 255);
            this.Font = new Font("Segoe UI", 10F);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(1000, 700);
            this.Padding = new Padding(0);
        }

        private void SetupUI()
        {
            CreateHeader();
            CreateFooter();
            CreateMainContentPanel();
            CreateTabControl();
            CreateExitButton();
            this.SizeChanged += (s, e) => AdjustLayout();
            this.Load += (s, e) => AdjustLayout();
        }

        private void CreateMainContentPanel()
        {
            mainContentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(10, 80, 10, 10)
            };
            this.Controls.Add(mainContentPanel);
        }

        private void AdjustLayout()
        {
            try
            {
                UpdateExitButtonPosition();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdjustLayout error: {ex.Message}");
            }
        }

        private void UpdateExitButtonPosition()
        {
            if (btnExit != null && panelFooter != null)
            {
                int margin = 20;
                btnExit.Location = new Point(
                    panelFooter.ClientSize.Width - btnExit.Width - margin,
                    (panelFooter.ClientSize.Height - btnExit.Height) / 2
                );
            }
        }

        private void CreateHeader()
        {
            panelHeader = new Panel
            {
                Height = 70,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent
            };
            // Градиентный фон
            panelHeader.Paint += (s, e) =>
            {
                var rect = panelHeader.ClientRectangle;
                using var brush = new LinearGradientBrush(
                    rect,
                    Color.FromArgb(70, 130, 180),
                    Color.FromArgb(100, 173, 216),
                    45F
                );
                e.Graphics.FillRectangle(brush, rect);
            };
            this.Controls.Add(panelHeader);

            var lblTitle = new Label
            {
                Text = "🎯 ПАНЕЛЬ АДМИНИСТРАТОРА - ЛЕДОВАЯ АРЕНА",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(0, 10, 0, 0)
            };
            panelHeader.Controls.Add(lblTitle);
        }

        private void CreateFooter()
        {
            panelFooter = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom,
                BackColor = Color.Transparent
            };
            this.Controls.Add(panelFooter);
        }

        private void CreateExitButton()
        {
            btnExit = new Button
            {
                Text = "🚪 Выход",
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.FlatAppearance.MouseOverBackColor = Color.FromArgb(192, 57, 43);
            btnExit.Click += (s, e) => this.Close();
            panelFooter.Controls.Add(btnExit);
            btnExit.BringToFront();
        }

        private void CreateTabControl()
        {
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ItemSize = new Size(220, 30),
                Padding = new Point(15, 5),
                Appearance = TabAppearance.Normal
            };
            // Создаем вкладки
            CreateUsersTab();
            CreateAnalyticsTab();
            CreateBookingsTab();
            CreateScheduleTab();
            CreateSupportTab();
            mainContentPanel.Controls.Add(tabControl);
            // Обработчик переключения вкладок
            tabControl.SelectedIndexChanged += (s, e) => OnTabChanged();
        }

        private void CreateUsersTab()
        {
            try
            {
                var tabPage = new TabPage("👥 УПРАВЛЕНИЕ ПОЛЬЗОВАТЕЛЯМИ");
                usersTab = new UsersTab();
                usersTab.Dock = DockStyle.Fill;
                tabPage.Controls.Add(usersTab);
                tabControl.Controls.Add(tabPage);
                // Сразу загружаем данные для первой вкладки
            }
            catch (Exception ex)
            {
                CreateErrorTab("👥 УПРАВЛЕНИЕ ПОЛЬЗОВАТЕЛЯМИ", ex.Message);
            }
        }

        private void CreateAnalyticsTab()
        {
            try
            {
                var tabPage = new TabPage("📊 АНАЛИТИКА АРЕНЫ");
                analyticsTab = new AnalyticsTab();
                analyticsTab.Dock = DockStyle.Fill;
                tabPage.Controls.Add(analyticsTab);
                tabControl.Controls.Add(tabPage);
            }
            catch (Exception ex)
            {
                CreateErrorTab("📊 АНАЛИТИКА АРЕНЫ", ex.Message);
            }
        }

        private void CreateBookingsTab()
        {
            try
            {
                var tabPage = new TabPage("🎫 ВСЕ БРОНИРОВАНИЯ");
                bookingsTab = new BookingsTab();
                bookingsTab.Dock = DockStyle.Fill;
                tabPage.Controls.Add(bookingsTab);
                tabControl.Controls.Add(tabPage);
            }
            catch (Exception ex)
            {
                CreateErrorTab("🎫 ВСЕ БРОНИРОВАНИЯ", ex.Message);
            }
        }

        private void CreateScheduleTab()
        {
            try
            {
                var tabPage = new TabPage("📅 РАСПИСАНИЕ");
                scheduleTab = new ScheduleTab();
                scheduleTab.Dock = DockStyle.Fill;
                tabPage.Controls.Add(scheduleTab);
                tabControl.Controls.Add(tabPage);
            }
            catch (Exception ex)
            {
                CreateErrorTab("📅 РАСПИСАНИЕ", ex.Message);
            }
        }

        private void CreateSupportTab()
        {
            try
            {
                var tabPage = new TabPage("🛠 ТЕХПОДДЕРЖКА");

                // ИСПРАВЛЕНИЕ: Создаем объект без параметров, а затем передаем родителя
                supportTab = new SupportTab();
                supportTab.SetParent(this); // Метод SetParent был добавлен в SupportTab в предыдущем шаге

                supportTab.Dock = DockStyle.Fill;
                tabPage.Controls.Add(supportTab);
                tabControl.Controls.Add(tabPage);
            }
            catch (Exception ex)
            {
                CreateErrorTab("🛠 ТЕХПОДДЕРЖКА", ex.Message);
            }
        }

        private void CreateErrorTab(string tabName, string errorMessage)
        {
            var tabPage = new TabPage(tabName + " ⚠️");
            var errorPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var label = new Label
            {
                Text = $"Ошибка загрузки вкладки:\n{errorMessage}",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.Red
            };
            errorPanel.Controls.Add(label);
            tabPage.Controls.Add(errorPanel);
            tabControl.Controls.Add(tabPage);
        }

        private void OnTabChanged()
        {
            try
            {
                // При переключении на вкладку пользователей обновляем данные
                if (tabControl.SelectedIndex == 0 && usersTab != null)
                {
                    // Можно добавить обновление данных при переключении
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnTabChanged error: {ex.Message}");
            }
        }

        // МЕТОД ДЛЯ ОТПРАВКИ ЗАПРОСОВ НА СЕРВЕР
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
                            do
                            {
                                bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                                ms.Write(buffer, 0, bytes);
                            } while (stream.DataAvailable);

                            string json = Encoding.UTF8.GetString(ms.ToArray()).Trim();
                            if (string.IsNullOrEmpty(json)) throw new Exception();
                            return JsonSerializer.Deserialize<JsonElement>(json);
                        }
                    }
                }
            }
            catch (Exception)
            {
                return JsonSerializer.Deserialize<JsonElement>("{}");
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            AdjustLayout();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            AdjustLayout();
        }
    }
}