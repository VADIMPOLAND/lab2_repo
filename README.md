using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;
using System.IO;
using System.Security.Cryptography;

namespace IceArena.Client
{
    public partial class Form1 : Form
    {
        // --- Логические поля ---
        private TcpClient _client;
        private NetworkStream _stream;

        // --- UI Элементы ---
        private TextBox txtLogin, txtPassword;
        private Button btnLogin, btnRegister, btnExit, btnGuest;
        private PictureBox picLogo, picPolessuLogo;
        private Panel panelMainCard;

        // Цветовая палитра
        private readonly Color clrBackground = Color.FromArgb(240, 242, 245);
        private readonly Color clrPrimary = Color.FromArgb(79, 70, 229); // Indigo
        private readonly Color clrPrimaryDark = Color.FromArgb(67, 56, 202);
        private readonly Color clrSecondary = Color.FromArgb(16, 185, 129); // Emerald
        private readonly Color clrTextDark = Color.FromArgb(31, 41, 55);
        private readonly Color clrTextLight = Color.FromArgb(107, 114, 128);

        public Form1()
        {
            InitializeComponent();
            SetupModernUI(); // Настройка интерфейса
        }

        // --- Настройка современного интерфейса ---
        private void SetupModernUI()
        {
            this.Text = "Ледовая Арена ПолесГУ - Вход";
            this.Size = new Size(1200, 850);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10F);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Градиентный фон формы
            this.Paint += (s, e) =>
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(this.ClientRectangle,
                    Color.FromArgb(224, 231, 255), Color.FromArgb(238, 242, 255), 45F))
                {
                    e.Graphics.FillRectangle(brush, this.ClientRectangle);
                }
            };

            // Основная центральная карточка (белая плашка)
            panelMainCard = new Panel
            {
                Size = new Size(500, 750), // Немного увеличил высоту для заголовка
                BackColor = Color.White,
                Location = new Point((this.ClientSize.Width - 500) / 2, (this.ClientSize.Height - 750) / 2),
                Padding = new Padding(40)
            };

            // Скругление углов карточки
            panelMainCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // Рисуем легкую тень (рамку)
                using (Pen p = new Pen(Color.FromArgb(20, 0, 0, 0), 1))
                {
                    e.Graphics.DrawRoundedRectangle(p, 0, 0, panelMainCard.Width - 1, panelMainCard.Height - 1, 20);
                }
            };
            SetRoundedRegion(panelMainCard, 20);
            this.Controls.Add(panelMainCard);

            // ====================================================================================
            // СОЗДАНИЕ ЭЛЕМЕНТОВ (Порядок создания не важен, важен порядок добавления в Controls)
            // ====================================================================================

            // 1. Футер (копирайт)
            Label lblFooter = new Label
            {
                Text = "© 2025 Polessu Ice Arena\nsupport@polessu.by",
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 50,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8)
            };

            // 2. Кнопки Регистрация и Выход
            TableLayoutPanel bottomButtons = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 45,
                ColumnCount = 2,
                RowCount = 1
            };
            bottomButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            bottomButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            btnRegister = new Button
            {
                Text = "Регистрация",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = clrPrimary,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.Click += (s, e) => { new RegisterForm().ShowDialog(); };

            btnExit = new Button
            {
                Text = "Выход",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.IndianRed,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.Click += (s, e) => Application.Exit();

            bottomButtons.Controls.Add(btnRegister, 0, 0);
            bottomButtons.Controls.Add(btnExit, 1, 0);

            // 3. Разделитель "или"
            Label lblOr = new Label
            {
                Text = "— или —",
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray
            };

            // 4. Кнопка Гость
            btnGuest = CreateModernButton("Продолжить как Гость", Color.White, Color.WhiteSmoke);
            btnGuest.ForeColor = clrTextDark;
            btnGuest.FlatStyle = FlatStyle.Flat;
            btnGuest.FlatAppearance.BorderColor = Color.LightGray;
            btnGuest.FlatAppearance.BorderSize = 1;
            btnGuest.Click += BtnGuest_Click;

            // 5. Кнопка Войти
            btnLogin = CreateModernButton("ВОЙТИ В АККАУНТ", clrPrimary, clrPrimaryDark);
            btnLogin.Click += BtnLogin_Click;

            // 6. Панель пароля
            var panelPassword = CreateModernInput(out txtPassword, "Пароль", true);
            panelPassword.Dock = DockStyle.Top;

            // 7. Панель логина
            var panelLogin = CreateModernInput(out txtLogin, "Email или Логин", false);
            panelLogin.Dock = DockStyle.Top;

            // 8. Подзаголовок
            Label lblSubtitle = new Label
            {
                Text = "Система бронирования Ледовой Арены",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = clrTextLight
            };

            // 9. Заголовок "Добро пожаловать"
            Label lblTitle = new Label
            {
                Text = "Добро пожаловать",
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = clrTextDark
            };

            // 10. Панель Логотипов и названия ВУЗа
            Panel logoPanel = new Panel { Dock = DockStyle.Top, Height = 140, BackColor = Color.Transparent };

            // Добавляем название "ПолесГУ" сверху
            Label lblUniName = new Label
            {
                Text = "ПолесГУ",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = clrPrimary
            };

            // Контейнер для картинок (чтобы они были под надписью "ПолесГУ")
            Panel imagesContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            // Логотип ПолесГУ (слева)
            picPolessuLogo = new PictureBox
            {
                Size = new Size(100, 80),
                Location = new Point(90, 10),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            // ВНИМАНИЕ: Здесь ваш обновленный путь
            TryLoadLogo(picPolessuLogo, @"C:\Users\vadim\source\repos\3 kurs\IceArena.Client\polessu\polessu.jpg", "polessu.jpg", "ПГУ");

            // Логотип Арены (справа)
            picLogo = new PictureBox
            {
                Size = new Size(80, 80),
                Location = new Point(230, 10),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            TryLoadLogo(picLogo, "gerb.png", "gerb.png", "🏒");

            imagesContainer.Controls.Add(picPolessuLogo);
            imagesContainer.Controls.Add(picLogo);

            logoPanel.Controls.Add(imagesContainer);
            logoPanel.Controls.Add(lblUniName); // Добавляем надпись первой в панель логотипов (будет сверху)

            // ====================================================================================
            // ИСПРАВЛЕНИЕ: ДОБАВЛЯЕМ ЭЛЕМЕНТЫ В ОБРАТНОМ ПОРЯДКЕ
            // (Потому что при Dock=Top последний добавленный элемент встает на самый верх)
            // ====================================================================================

            // Сначала нижний футер
            panelMainCard.Controls.Add(lblFooter);

            // Теперь идем СНИЗУ ВВЕРХ (визуально):

            // Самый низ контента (кнопки Рег/Выход) -> добавляем ПЕРВЫМИ в стек Dock.Top
            panelMainCard.Controls.Add(bottomButtons);
            panelMainCard.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 }); // Отступ

            panelMainCard.Controls.Add(lblOr);
            panelMainCard.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 25 }); // Отступ

            panelMainCard.Controls.Add(btnGuest);
            panelMainCard.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 }); // Отступ

            panelMainCard.Controls.Add(btnLogin);
            panelMainCard.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 30 }); // Отступ перед кнопками

            panelMainCard.Controls.Add(panelPassword);
            panelMainCard.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 15 }); // Отступ между полями

            panelMainCard.Controls.Add(panelLogin);
            panelMainCard.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 20 }); // Отступ после заголовка

            panelMainCard.Controls.Add(lblSubtitle);
            panelMainCard.Controls.Add(lblTitle);

            // И самый верхний элемент добавляем ПОСЛЕДНИМ
            panelMainCard.Controls.Add(logoPanel);

            // Настройка Enter
            this.AcceptButton = btnLogin;
            txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnLogin.PerformClick(); };
        }

        // --- Вспомогательные методы UI (Без изменений) ---

        private Panel CreateModernInput(out TextBox textBox, string placeholder, bool isPassword)
        {
            Panel container = new Panel
            {
                Height = 55,
                Padding = new Padding(0, 5, 0, 5),
                BackColor = Color.Transparent
            };
            Label lblTitle = new Label
            {
                Text = placeholder.ToUpper(),
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray
            };
            Panel inputBack = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.FromArgb(249, 250, 251),
                Padding = new Padding(10, 5, 10, 5)
            };
            Panel underline = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 2,
                BackColor = Color.LightGray
            };
            inputBack.Controls.Add(underline);

            textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = inputBack.BackColor,
                Font = new Font("Segoe UI", 11),
                ForeColor = clrTextDark
            };
            if (isPassword) textBox.PasswordChar = '•';

            textBox.Enter += (s, e) => { underline.BackColor = clrPrimary; inputBack.BackColor = Color.White; };
            textBox.Leave += (s, e) => { underline.BackColor = Color.LightGray; inputBack.BackColor = Color.FromArgb(249, 250, 251); };

            inputBack.Controls.Add(textBox);
            container.Controls.Add(inputBack);
            container.Controls.Add(lblTitle);

            return container;
        }

        private Button CreateModernButton(string text, Color bg, Color hover)
        {
            Button btn = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = hover;
            btn.MouseLeave += (s, e) => btn.BackColor = bg;
            btn.Paint += (s, e) => { SetRoundedRegion(btn, 10); };

            return btn;
        }

        private void SetRoundedRegion(Control c, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(c.Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(c.Width - radius, c.Height - radius, radius, radius, 0, 90);
            path.AddArc(0, c.Height - radius, radius, radius, 90, 90);
            path.CloseAllFigures();
            c.Region = new Region(path);
        }

        private void TryLoadLogo(PictureBox pb, string path1, string path2, string fallbackText)
        {
            try
            {
                // Проверяем абсолютный путь (если передан) или относительный
                if (File.Exists(path1)) pb.Image = Image.FromFile(path1);
                else
                {
                    // Пробуем второй путь относительно папки запуска
                    string fullPath2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path2);
                    if (File.Exists(fullPath2)) pb.Image = Image.FromFile(fullPath2);
                    else throw new FileNotFoundException();
                }
            }
            catch
            {
                // Если картинки нет, рисуем заглушку
                pb.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(224, 231, 255)), 0, 0, pb.Width - 1, pb.Height - 1);
                    using (Font f = new Font("Segoe UI", 14, FontStyle.Bold))
                    {
                        SizeF size = e.Graphics.MeasureString(fallbackText, f);
                        e.Graphics.DrawString(fallbackText, f, new SolidBrush(clrPrimary),
                            (pb.Width - size.Width) / 2, (pb.Height - size.Height) / 2);
                    }
                };
            }
        }

        // --- ЛОГИКА (Оставлена без изменений) ---

        private void BtnGuest_Click(object sender, EventArgs e)
        {
            try
            {
                ClientForm guestForm = new ClientForm("Гость", 0, true);
                this.Hide();
                guestForm.Show();
                guestForm.FormClosed += (s, args) =>
                {
                    this.Show();
                    this.BringToFront();
                    this.Focus();
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе как гость: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void ShowAuthForm()
        {
            this.Show();
            this.BringToFront();
            this.Focus();
            txtLogin.Text = "";
            txtPassword.Text = "";
            txtLogin.Focus();
        }

        private bool IsServerRunning()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync("127.0.0.1", 8888);
                    return connectTask.Wait(2000);
                }
            }
            catch { return false; }
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            if (!IsServerRunning())
            {
                MessageBox.Show("❌ Сервер не запущен!\nЗапустите IceArena.Server.exe перед использованием приложения.",
                    "Ошибка подключения", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string login = txtLogin.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (login == "admin" && password == "admin")
            {
                try
                {
                    AdminForm adminForm = new AdminForm();
                    this.Hide();
                    adminForm.FormClosed += (s, args) => this.ShowAuthForm();
                    adminForm.Show();
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии админ-панели: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Пожалуйста, введите логин и пароль!", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLogin.Focus();
                return;
            }

            try
            {
                btnLogin.Enabled = false;
                btnLogin.Text = "ВХОД...";

                string encryptedPassword = EncryptionHelper.Encrypt(password);
                if (string.IsNullOrEmpty(encryptedPassword))
                {
                    MessageBox.Show("Ошибка шифрования пароля", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync("127.0.0.1", 8888);
                    var timeoutTask = Task.Delay(5000);

                    if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
                    {
                        throw new TimeoutException("Таймаут подключения к серверу");
                    }

                    using (var stream = client.GetStream())
                    {
                        var request = new
                        {
                            Command = "login",
                            Email = login,
                            Password = encryptedPassword
                        };

                        string json = JsonSerializer.Serialize(request);
                        byte[] data = Encoding.UTF8.GetBytes(json);
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[4096];
                        var readTask = stream.ReadAsync(buffer, 0, buffer.Length);
                        var readTimeoutTask = Task.Delay(5000);
                        if (await Task.WhenAny(readTask, readTimeoutTask) == readTimeoutTask)
                        {
                            throw new TimeoutException("Таймаут получения ответа от сервера");
                        }

                        int bytesRead = await readTask;
                        string responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                        var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
                        if (response.TryGetProperty("Success", out JsonElement successElement) &&
                            successElement.GetBoolean())
                        {
                            string role = "Client";
                            int userId = 0;

                            if (response.TryGetProperty("Role", out JsonElement roleElement)) role = roleElement.GetString();
                            if (response.TryGetProperty("UserId", out JsonElement userIdElement)) userId = userIdElement.GetInt32();
                            MessageBox.Show($"Добро пожаловать, {login}!",
                                "Успешный вход", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.Hide();

                            if (role == "Admin")
                            {
                                var adminForm = new AdminForm();
                                adminForm.FormClosed += (s, args) => this.ShowAuthForm();
                                adminForm.Show();
                            }
                            else
                            {
                                var clientForm = new ClientForm(login, userId, false);
                                clientForm.FormClosed += (s, args) => this.ShowAuthForm();
                                clientForm.Show();
                            }
                        }
                        else
                        {
                            string error = "Ошибка авторизации";
                            if (response.TryGetProperty("Error", out JsonElement errorElement)) error = errorElement.GetString();
                            MessageBox.Show(error, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            txtPassword.Focus();
                            txtPassword.SelectAll();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "ВОЙТИ В АККАУНТ";
            }
        }
    }

    // Расширение для рисования скругленного прямоугольника
    public static class GraphicsExtension
    {
        public static void DrawRoundedRectangle(this Graphics g, Pen pen, int x, int y, int w, int h, int r)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(x, y, r, r, 180, 90);
            path.AddArc(x + w - r, y, r, r, 270, 90);
            path.AddArc(x + w - r, y + h - r, r, r, 0, 90);
            path.AddArc(x, y + h - r, r, r, 90, 90);
            path.CloseAllFigures();
            g.DrawPath(pen, path);
        }
    }
}
