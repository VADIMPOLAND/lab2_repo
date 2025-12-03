using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace IceArena.Client
{
    public partial class AddEditUserForm : Form
    {
        private TextBox txtEmail, txtPassword;
        private ComboBox cmbRole;
        private Button btnSave, btnCancel, btnDelete;
        private ListBox lstActivity;
        private readonly bool isEdit;
        private readonly string currentEmail;
        private int currentUserId;
        public event Action UserSaved;
        private const string ConnectionString =
            "Server=DESKTOP-I80K0OH\\SQLEXPRESS;Database=Ice_Arena;Trusted_Connection=true;TrustServerCertificate=true;";
        // Цвета
        private readonly Color Primary = Color.FromArgb(41, 128, 185);
        private readonly Color Success = Color.FromArgb(46, 204, 113);
        private readonly Color Danger = Color.FromArgb(231, 76, 60);
        private readonly Color Warning = Color.FromArgb(241, 196, 15);
        private readonly Color Dark = Color.FromArgb(44, 62, 80);
        private readonly Color LightBg = Color.FromArgb(248, 249, 250);
        private readonly Color CardBg = Color.White;
        private readonly Color LightBlue = Color.FromArgb(240, 245, 255);
        private SplitContainer splitContainer; // для безопасной установки SplitterDistance
        public AddEditUserForm(string email = null)
        {
            isEdit = email != null;
            currentEmail = email;
            Text = isEdit ? "Редактирование пользователя" : "Добавление пользователя";
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = LightBg;
            Font = new Font("Segoe UI", 11F);
            FormBorderStyle = FormBorderStyle.Sizable;
            InitializeComponents();
            // SplitterDistance устанавливаем после отображения формы
            this.Shown += (s, e) => SetSplitterDistanceSafely();
            if (isEdit)
            {
                if (string.IsNullOrEmpty(currentEmail))
                {
                    MessageBox.Show("Email не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }
                LoadUserData(email);
                LoadUserActivity();
            }
        }
        private void InitializeComponents()
        {
            var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Controls.Add(mainLayout);
            // ───── Заголовок ─────
            var header = new Panel { Dock = DockStyle.Fill, BackColor = Primary, Padding = new Padding(30) };
            mainLayout.Controls.Add(header, 0, 0);
            header.Controls.Add(new Label
            {
                Text = isEdit ? "РЕДАКТИРОВАНИЕ ПОЛЬЗОВАТЕЛЯ" : "ДОБАВЛЕНИЕ ПОЛЬЗОВАТЕЛЯ",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 8)
            });
            header.Controls.Add(new Label
            {
                Text = isEdit
                    ? "Обновление данных существующего пользователя"
                    : "Создание новой учётной записи",
                ForeColor = Color.FromArgb(220, 240, 255),
                Font = new Font("Segoe UI", 11F),
                AutoSize = true,
                Location = new Point(0, 55)
            });
            // ───── SplitContainer ─────
            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel1MinSize = 0,
                Panel2MinSize = 0
            };
            mainLayout.Controls.Add(splitContainer, 0, 1);
            // Левая часть — форма
            splitContainer.Panel1.Controls.Add(CreateFormCard());
            // Правая часть — информация/активность
            var rightCard = CreateCard();
            splitContainer.Panel2.Controls.Add(rightCard);
            if (isEdit)
                SetupActivityPanel(rightCard);
            else
                SetupInfoPanel(rightCard);
        }
        private void SetSplitterDistanceSafely()
        {
            if (splitContainer == null || splitContainer.Width <= 0 || !splitContainer.IsHandleCreated) return;
            try
            {
                // Временно устанавливаем минимальные размеры в 0 для избежания ошибок
                splitContainer.Panel1MinSize = 0;
                splitContainer.Panel2MinSize = 0;
                int available = splitContainer.Width - splitContainer.SplitterWidth;
                if (available <= 0) return;
                int desiredMin = 400;
                int maxMin = available / 2;
                int panelMin = Math.Min(desiredMin, maxMin);
                int totalMin = panelMin * 2 + splitContainer.SplitterWidth;
                if (splitContainer.Width < totalMin) return;
                int target = available / 2;
                int safe = Math.Max(panelMin, Math.Min(target, available - panelMin));
                splitContainer.SplitterDistance = safe;
                // Теперь устанавливаем минимальные размеры
                splitContainer.Panel1MinSize = panelMin;
                splitContainer.Panel2MinSize = panelMin;
            }
            catch { /* Игнорируем */ }
        }
        private Panel CreateCard()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBg,
                Padding = new Padding(20),
                Margin = new Padding(10),
                BorderStyle = BorderStyle.FixedSingle
            };
        }
        private Panel CreateFormCard()
        {
            var card = CreateCard();
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 7,
                Padding = new Padding(10)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(new Label
            {
                Text = "ОСНОВНЫЕ ДАННЫЕ",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Dark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            layout.Controls.Add(CreateLabeledField("EMAIL:", out txtEmail,
                text: isEdit ? currentEmail : "", placeholder: "example@domain.com", readOnly: isEdit), 0, 1);
            var passPanel = CreateLabeledField("ПАРОЛЬ:", out txtPassword, placeholder: "", password: true);
            passPanel.Controls.Add(new Label
            {
                Text = "Минимум 6 символов",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                Dock = DockStyle.Bottom,
                Height = 25
            });
            layout.Controls.Add(passPanel, 0, 2);
            layout.Controls.Add(CreateLabeledCombo("РОЛЬ:", out cmbRole), 0, 3);
            var hintBox = new Panel { BackColor = LightBlue, Dock = DockStyle.Fill, Padding = new Padding(15) };
            hintBox.Controls.Add(new Label
            {
                Text = isEdit
                    ? "Оставьте поле пароля пустым, если не хотите менять пароль"
                    : "Пароль должен содержать минимум 6 символов",
                ForeColor = Color.FromArgb(70, 130, 180),
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F)
            });
            layout.Controls.Add(hintBox, 0, 4);
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0)
            };
            btnSave = CreateButton("СОХРАНИТЬ", Success, 150, 45);
            btnSave.Click += (s, e) => SaveUser();
            btnCancel = CreateButton("ОТМЕНА", Warning, 150, 45);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            btnPanel.Controls.Add(btnCancel);
            btnPanel.Controls.Add(btnSave);
            if (isEdit)
            {
                btnDelete = CreateButton("УДАЛИТЬ", Danger, 150, 45);
                btnDelete.Click += (s, e) => DeleteUser();
                btnPanel.Controls.Add(btnDelete);
                btnPanel.FlowDirection = FlowDirection.LeftToRight;
            }
            layout.Controls.Add(btnPanel, 0, 5);
            card.Controls.Add(layout);
            return card;
        }
        private Panel CreateLabeledField(string label, out TextBox tb,
            string text = "", string placeholder = "", bool password = false, bool readOnly = false)
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Dark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            }, 0, 0);
            tb = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F),
                Margin = new Padding(0, 5, 0, 0),
                Text = text,
                ReadOnly = readOnly,
                BackColor = readOnly ? Color.FromArgb(245, 245, 245) : Color.White
            };
            if (password) tb.PasswordChar = '•';
            if (!string.IsNullOrEmpty(placeholder)) tb.PlaceholderText = placeholder;
            panel.Controls.Add(tb, 0, 1);
            return panel;
        }
        private Panel CreateLabeledCombo(string label, out ComboBox cb)
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Dark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            }, 0, 0);
            cb = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F),
                Margin = new Padding(0, 5, 0, 0)
            };
            cb.Items.AddRange(new[] { "Администратор", "Клиент" });
            cb.SelectedIndex = 1;
            panel.Controls.Add(cb, 0, 1);
            return panel;
        }
        private Button CreateButton(string text, Color backColor, int width, int height)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(width, height),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.1f);
            return btn;
        }
        // ───── Правая панель ─────
        private void SetupActivityPanel(Panel container)
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), RowCount = 4 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            layout.Controls.Add(new Label
            {
                Text = "АКТИВНОСТЬ ПОЛЬЗОВАТЕЛЯ",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Dark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            layout.Controls.Add(new Label
            {
                Text = currentEmail,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Primary,
                Dock = DockStyle.Fill
            }, 0, 1);
            lstActivity = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5F),
                BorderStyle = BorderStyle.FixedSingle
            };
            layout.Controls.Add(lstActivity, 0, 2);
            var refreshBtn = CreateButton("ОБНОВИТЬ АКТИВНОСТЬ", Primary, 220, 45);
            refreshBtn.Dock = DockStyle.Fill;
            refreshBtn.Click += (s, e) => LoadUserActivity();
            layout.Controls.Add(refreshBtn, 0, 3);
            container.Controls.Add(layout);
        }
        private void SetupInfoPanel(Panel container)
        {
            var infoPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            container.Controls.Add(infoPanel);
            int y = 20;
            var titleLabel = new Label
            {
                Text = "ИНФОРМАЦИЯ О СИСТЕМЕ",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Dark,
                AutoSize = true,
                Location = new Point(20, y)
            };
            infoPanel.Controls.Add(titleLabel);
            y += titleLabel.Height + 30;
            y = AddInfoBlockToPanel(infoPanel, y, "АВТОМАТИЧЕСКИЕ НАСТРОЙКИ", new[]
            {
                "• Новый пользователь сразу появляется в списке",
                "• Дата регистрации заполняется автоматически",
                "• Роль «Клиент» по умолчанию",
                "• Статус «Активный»"
            });
            y += 20;
            y = AddInfoBlockToPanel(infoPanel, y, "ВОЗМОЖНОСТИ ПОЛЬЗОВАТЕЛЯ", new[]
            {
                "• Бронирование сеансов",
                "• Просмотр расписания",
                "• Уведомления",
                "• История посещений"
            });
            // Добавляем обработчик изменения размера для обновления ширины блоков
            infoPanel.SizeChanged += (s, e) => UpdateInfoBlocksWidth(infoPanel);
            UpdateInfoBlocksWidth(infoPanel);
        }
        private void UpdateInfoBlocksWidth(Panel parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c.Tag == "infoBlock")
                {
                    c.Width = parent.ClientSize.Width - 40;
                }
            }
        }
        private int AddInfoBlockToPanel(Panel parent, int y, string title, string[] lines)
        {
            var block = new Panel { Location = new Point(20, y), Width = parent.ClientSize.Width - 40, Tag = "infoBlock" };
            var titleLbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Dark,
                Dock = DockStyle.Top,
                Height = 35
            };
            var content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = LightBlue,
                Padding = new Padding(15),
                BorderStyle = BorderStyle.FixedSingle
            };
            content.Controls.Add(new Label
            {
                Text = string.Join(Environment.NewLine, lines),
                Dock = DockStyle.Fill,
                ForeColor = Dark,
                Font = new Font("Segoe UI", 11F),
                AutoSize = true
            });
            block.Controls.Add(content);
            block.Controls.Add(titleLbl);
            block.Height = titleLbl.Height + content.PreferredSize.Height + 20;
            parent.Controls.Add(block);
            return block.Location.Y + block.Height + 20;
        }
        private void LoadUserData(string email)
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();
                var sql = "SELECT Id, Role FROM Users WHERE Email = @Email";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Email", email);
                using var rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    currentUserId = rdr.GetInt32(0);
                    cmbRole.SelectedItem = rdr.GetString(1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void LoadUserActivity()
        {
            if (!isEdit) return;
            lstActivity.Items.Clear();
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();
                var sql = @"SELECT TOP 15 Id, BookingDate, Status
                            FROM Bookings
                            WHERE UserId = @UserId
                            ORDER BY BookingDate DESC";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserId", currentUserId);
                using var rdr = cmd.ExecuteReader();
                bool hasData = false;
                while (rdr.Read())
                {
                    hasData = true;
                    int id = rdr.GetInt32(0);
                    DateTime dt = rdr.GetDateTime(1);
                    string status = rdr.GetString(2);
                    string icon = status == "Booked" ? "Confirmed" : "Pending";
                    lstActivity.Items.Add($"{icon} Бронирование #{id} — {dt:dd.MM.yyyy HH:mm}");
                }
                if (!hasData)
                    lstActivity.Items.Add("Нет активных бронирований");
            }
            catch (Exception ex)
            {
                lstActivity.Items.Add("Ошибка загрузки активности");
                lstActivity.Items.Add(ex.Message);
            }
        }
        private void SaveUser()
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;
            string role = cmbRole.SelectedItem?.ToString() ?? "Клиент";
            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Введите email", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEmail.Focus(); return;
            }
            if (!IsValidEmail(email))
            {
                MessageBox.Show("Некорректный email", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEmail.Focus(); return;
            }
            if (!isEdit && string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите пароль", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus(); return;
            }
            if (!isEdit && password.Length < 6)
            {
                MessageBox.Show("Пароль должен быть не менее 6 символов", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus(); return;
            }
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();
                if (!isEdit)
                {
                    var check = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Email = @Email", conn);
                    check.Parameters.AddWithValue("@Email", email);
                    if ((int)check.ExecuteScalar() > 0)
                    {
                        MessageBox.Show("Пользователь с таким email уже существует", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                string sql = isEdit
                    ? "UPDATE Users SET Role = @Role" +
                      (!string.IsNullOrEmpty(password) ? ", PasswordHash = @PasswordHash" : "") +
                      " WHERE Email = @Email"
                    : "INSERT INTO Users (Email, PasswordHash, Role, RegDate) VALUES (@Email, @PasswordHash, @Role, GETDATE())";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Role", role);
                if (isEdit && !string.IsNullOrEmpty(password) || !isEdit)
                    cmd.Parameters.AddWithValue("@PasswordHash", ComputeSha256Hash(password));
                cmd.ExecuteNonQuery();
                MessageBox.Show(isEdit ? "Данные обновлены!" : "Пользователь добавлен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UserSaved?.Invoke();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void DeleteUser()
        {
            if (!isEdit) return;
            if (MessageBox.Show($"Удалить пользователя {currentEmail}?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();
                new SqlCommand("DELETE FROM Bookings WHERE UserId = @Id", conn)
                {
                    Parameters = { new SqlParameter("@Id", currentUserId) }
                }.ExecuteNonQuery();
                new SqlCommand("DELETE FROM Users WHERE Id = @Id", conn)
                {
                    Parameters = { new SqlParameter("@Id", currentUserId) }
                }.ExecuteNonQuery();
                MessageBox.Show("Пользователь удалён", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UserSaved?.Invoke();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private bool IsValidEmail(string email) =>
            Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        private string ComputeSha256Hash(string raw)
        {
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            var sb = new StringBuilder();
            foreach (byte b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}