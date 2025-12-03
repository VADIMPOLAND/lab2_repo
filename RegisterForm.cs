using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

namespace IceArena.Client
{
    public partial class RegisterForm : Form
    {
        // --- UI Элементы ---
        private TextBox txtEmail, txtPassword, txtConfirmPassword;
        private ComboBox cmbRole;
        private Button btnRegister, btnBack;
        private Label lblError;
        private Panel panelMainCard;
        private FlowLayoutPanel flowContent;

        // --- Цветовая палитра ---
        private readonly Color clrPrimary = Color.FromArgb(79, 70, 229);
        private readonly Color clrSuccess = Color.FromArgb(16, 185, 129);
        private readonly Color clrDanger = Color.FromArgb(239, 68, 68);
        private readonly Color clrTextDark = Color.FromArgb(31, 41, 55);
        private readonly Color clrTextLight = Color.FromArgb(107, 114, 128);
        private readonly Color clrBgLight = Color.FromArgb(249, 250, 251);

        public RegisterForm()
        {
            InitializeComponents();
            SetupModernUI();
        }

        private void InitializeComponents()
        {
            this.SuspendLayout();
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1200, 950); // Чуть увеличили высоту формы
            this.Name = "RegisterForm";
            this.Text = "Регистрация";
            this.ResumeLayout(false);
        }

        private void SetupModernUI()
        {
            this.Text = "Ледовая Арена ПолесГУ - Регистрация";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Font = new Font("Segoe UI", 10F);

            // 1. Градиентный фон
            this.Paint += (s, e) =>
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(this.ClientRectangle,
                    Color.FromArgb(224, 231, 255), Color.FromArgb(238, 242, 255), 45F))
                {
                    e.Graphics.FillRectangle(brush, this.ClientRectangle);
                }
            };

            // 2. Центральная карточка
            panelMainCard = new Panel
            {
                Size = new Size(550, 850), // Увеличили высоту карточки, так как поля стали больше
                BackColor = Color.White,
                Padding = new Padding(0)
            };

            panelMainCard.Location = new Point(
                (this.ClientSize.Width - panelMainCard.Width) / 2,
                (this.ClientSize.Height - panelMainCard.Height) / 2
            );

            panelMainCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(Color.FromArgb(20, 0, 0, 0), 1))
                {
                    GraphicsExtensions.DrawRoundedRectangle(e.Graphics, p, 0, 0, panelMainCard.Width - 1, panelMainCard.Height - 1, 20);
                }
            };

            GraphicsPath path = new GraphicsPath();
            int r = 20;
            path.AddArc(0, 0, r, r, 180, 90);
            path.AddArc(panelMainCard.Width - r, 0, r, r, 270, 90);
            path.AddArc(panelMainCard.Width - r, panelMainCard.Height - r, r, r, 0, 90);
            path.AddArc(0, panelMainCard.Height - r, r, r, 90, 90);
            path.CloseAllFigures();
            panelMainCard.Region = new Region(path);

            this.Controls.Add(panelMainCard);

            // 3. FlowLayoutPanel
            flowContent = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(40, 30, 40, 30),
                AutoScroll = false
            };
            panelMainCard.Controls.Add(flowContent);

            // --- Элементы ---

            Label lblTitle = new Label
            {
                Text = "Создание аккаунта",
                Width = 470,
                Height = 50,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = clrTextDark,
                Margin = new Padding(0, 0, 0, 10)
            };
            flowContent.Controls.Add(lblTitle);

            Label lblSub = new Label
            {
                Text = "Заполните форму ниже для регистрации",
                Width = 470,
                Height = 30,
                TextAlign = ContentAlignment.TopCenter,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = clrTextLight,
                Margin = new Padding(0, 0, 0, 20)
            };
            flowContent.Controls.Add(lblSub);

            // Поля ввода
            var pnlRole = CreateModernCombo(out cmbRole, "Тип учетной записи");
            cmbRole.Items.Add("Клиент");
            cmbRole.Items.Add("Тренер");
            cmbRole.SelectedIndex = 0;
            flowContent.Controls.Add(pnlRole);

            var pnlEmail = CreateModernInput(out txtEmail, "Электронная почта", false, "example@gmail.com");
            flowContent.Controls.Add(pnlEmail);

            var pnlPass = CreateModernInput(out txtPassword, "Пароль", true);
            flowContent.Controls.Add(pnlPass);

            var pnlConfirm = CreateModernInput(out txtConfirmPassword, "Подтверждение пароля", true);
            flowContent.Controls.Add(pnlConfirm);

            lblError = new Label
            {
                Width = 470,
                Height = 30,
                ForeColor = clrDanger,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false,
                Text = "Ошибка",
                Margin = new Padding(0, 5, 0, 5)
            };
            flowContent.Controls.Add(lblError);

            flowContent.Controls.Add(new Panel { Height = 10, Width = 470, BackColor = Color.Transparent });

            btnRegister = CreateModernButton("ЗАРЕГИСТРИРОВАТЬСЯ", clrSuccess, Color.FromArgb(16, 160, 110));
            btnRegister.Click += BtnRegister_Click;
            flowContent.Controls.Add(btnRegister);

            flowContent.Controls.Add(new Panel { Height = 10, Width = 470, BackColor = Color.Transparent });

            btnBack = CreateModernButton("ОТМЕНА", Color.FromArgb(243, 244, 246), Color.FromArgb(229, 231, 235));
            btnBack.ForeColor = clrTextDark;
            btnBack.Click += (s, e) => this.Close();
            flowContent.Controls.Add(btnBack);

            // Enter navigation
            txtEmail.KeyDown += (s, ev) => { if (ev.KeyCode == Keys.Enter) txtPassword.Focus(); };
            txtPassword.KeyDown += (s, ev) => { if (ev.KeyCode == Keys.Enter) txtConfirmPassword.Focus(); };
            txtConfirmPassword.KeyDown += (s, ev) => { if (ev.KeyCode == Keys.Enter) btnRegister.PerformClick(); };
        }

        // --- ИСПРАВЛЕННЫЕ ХЕЛПЕРЫ (ДЛЯ ЛУЧШЕЙ ВИДИМОСТИ) ---

        private Panel CreateModernInput(out TextBox textBox, string labelText, bool isPassword, string placeholder = "")
        {
            Panel container = new Panel
            {
                Width = 470,
                Height = 80, // <-- УВЕЛИЧЕНО с 70 до 80
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 15)
            };

            Label lbl = new Label
            {
                Text = labelText.ToUpper(),
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = clrTextLight
            };
            container.Controls.Add(lbl);

            Panel inputBack = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = clrBgLight,
                // <-- ВАЖНОЕ ИЗМЕНЕНИЕ: Отступ сверху 18, чтобы текст был по центру и не резался
                Padding = new Padding(15, 18, 15, 10)
            };

            Panel underline = new Panel { Dock = DockStyle.Bottom, Height = 2, BackColor = Color.LightGray };
            inputBack.Controls.Add(underline);

            textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = clrBgLight,
                Font = new Font("Segoe UI", 14F), // <-- УВЕЛИЧЕН ШРИФТ с 12 до 14
                ForeColor = clrTextDark,
                PlaceholderText = placeholder
            };
            if (isPassword) textBox.PasswordChar = '•';

            textBox.Enter += (s, e) => { underline.BackColor = clrPrimary; inputBack.BackColor = Color.White; };
            textBox.Leave += (s, e) => { underline.BackColor = Color.LightGray; inputBack.BackColor = clrBgLight; };

            inputBack.Controls.Add(textBox);
            container.Controls.Add(inputBack);
            return container;
        }

        private Panel CreateModernCombo(out ComboBox comboBox, string labelText)
        {
            Panel container = new Panel
            {
                Width = 470,
                Height = 80, // <-- УВЕЛИЧЕНО
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 15)
            };

            Label lbl = new Label
            {
                Text = labelText.ToUpper(),
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = clrTextLight
            };
            container.Controls.Add(lbl);

            Panel inputBack = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = clrBgLight,
                Padding = new Padding(15, 18, 15, 10) // <-- Отступы для центрирования
            };

            comboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14F), // <-- УВЕЛИЧЕН ШРИФТ
                BackColor = clrBgLight,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            inputBack.Controls.Add(comboBox);
            container.Controls.Add(inputBack);
            return container;
        }

        private Button CreateModernButton(string text, Color bg, Color hoverBg)
        {
            Button btn = new Button
            {
                Text = text,
                Width = 470,
                Height = 55,
                BackColor = bg,
                ForeColor = (bg.R > 200 && bg.G > 200 && bg.B > 200) ? clrTextDark : Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => { btn.BackColor = hoverBg; };
            btn.MouseLeave += (s, e) => { btn.BackColor = bg; };

            btn.Paint += (s, e) =>
            {
                GraphicsPath p = new GraphicsPath();
                int r = 10;
                p.AddArc(0, 0, r, r, 180, 90);
                p.AddArc(btn.Width - r, 0, r, r, 270, 90);
                p.AddArc(btn.Width - r, btn.Height - r, r, r, 0, 90);
                p.AddArc(0, btn.Height - r, r, r, 90, 90);
                p.CloseAllFigures();
                btn.Region = new Region(p);
            };
            return btn;
        }

        private void ShowError(string message)
        {
            lblError.Text = $"⚠️ {message}";
            lblError.Visible = true;
        }

        private void HideError()
        {
            lblError.Visible = false;
        }

        // --- ЛОГИКА ---
        private async void BtnRegister_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;
            string role = "Client";
            if (cmbRole.SelectedItem != null) role = cmbRole.SelectedItem.ToString();

            HideError();

            if (string.IsNullOrEmpty(email)) { ShowError("Введите email!"); txtEmail.Focus(); return; }
            if (!IsValidEmail(email)) { ShowError("Некорректный формат email!"); txtEmail.Focus(); return; }
            if (string.IsNullOrEmpty(password)) { ShowError("Введите пароль!"); txtPassword.Focus(); return; }
            if (password.Length < 6) { ShowError("Пароль должен быть не менее 6 символов!"); txtPassword.Focus(); return; }
            if (password != confirmPassword) { ShowError("Пароли не совпадают!"); txtConfirmPassword.Focus(); return; }

            try
            {
                btnRegister.Enabled = false;
                btnRegister.Text = "ОБРАБОТКА...";
                btnRegister.BackColor = Color.Gray;

                string encryptedPassword = EncryptionHelper.Encrypt(password);
                if (string.IsNullOrEmpty(encryptedPassword)) { ShowError("Ошибка шифрования."); return; }

                bool success = await RegisterUserOnServer(email, encryptedPassword, role);
                if (success)
                {
                    MessageBox.Show("🎉 Регистрация прошла успешно!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
            }
            catch (Exception ex) { ShowError($"Ошибка: {ex.Message}"); }
            finally
            {
                btnRegister.Enabled = true;
                btnRegister.Text = "ЗАРЕГИСТРИРОВАТЬСЯ";
                btnRegister.BackColor = clrSuccess;
            }
        }

        private async Task<bool> RegisterUserOnServer(string email, string encryptedPassword, string role)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync("127.0.0.1", 8888);
                    if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                    {
                        ShowError("Сервер недоступен (timeout)."); return false;
                    }
                    await connectTask;

                    using (var stream = client.GetStream())
                    {
                        var request = new { Command = "register", Email = email, Password = encryptedPassword, Role = role };
                        byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[4096];
                        StringBuilder responseBuilder = new StringBuilder();
                        do
                        {
                            int bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                            if (bytes == 0) break;
                            responseBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytes));
                        } while (stream.DataAvailable);

                        string responseJson = responseBuilder.ToString().Trim();
                        if (string.IsNullOrEmpty(responseJson)) { ShowError("Пустой ответ"); return false; }

                        try
                        {
                            var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
                            if (response.TryGetProperty("Success", out var s) && s.GetBoolean()) return true;

                            string err = "Ошибка";
                            if (response.TryGetProperty("Error", out var e)) err = e.GetString();
                            else if (response.TryGetProperty("Message", out var m)) err = m.GetString();
                            ShowError(err);
                            return false;
                        }
                        catch { ShowError("Ошибка данных."); return false; }
                    }
                }
            }
            catch (Exception ex) { ShowError($"Сеть: {ex.Message}"); return false; }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try { return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250)); }
            catch { return false; }
        }
    }

    public static class GraphicsExtensions
    {
        public static void DrawRoundedRectangle(Graphics g, Pen pen, int x, int y, int w, int h, int r)
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