using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Net.Mail;
using System.Security.Cryptography;
using System.IO;
using System.Threading;

namespace IceArena.Server
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
    }

    public static class ServerEncryptionHelper
    {
        private static readonly byte[] EncryptionKey = Encoding.UTF8.GetBytes("12345678901234567890123456789012");
        private static readonly byte[] EncryptionIV = Encoding.UTF8.GetBytes("1234567890123456");

        public static string Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = EncryptionKey;
                aes.IV = EncryptionIV;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            try
            {
                byte[] buffer = Convert.FromBase64String(cipherText);
                using (Aes aes = Aes.Create())
                {
                    aes.Key = EncryptionKey;
                    aes.IV = EncryptionIV;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    using (MemoryStream ms = new MemoryStream(buffer))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(cs))
                            {
                                return sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка расшифровки: {ex.Message}");
                return null;
            }
        }
    }

    class Program
    {
        private static TcpListener _listener;
        private const int Port = 8888;
        private const string Ip = "127.0.0.1";
        private static bool _isRunning = true;
        private static readonly List<TcpClient> _connectedClients = new List<TcpClient>();

        private const string ConnectionString =
            "Data Source=DESKTOP-I80K0OH\\SQLEXPRESS;Initial Catalog=Ice_Arena;Integrated Security=True;TrustServerCertificate=True;";

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Ice Arena JSON Server ===");
            Console.WriteLine("Сервер предназначен ТОЛЬКО для клиентского приложения");
            Console.WriteLine("Не открывайте в браузере - используйте IceArena.Client.exe");
            Console.WriteLine("==============================");

            // Обработка graceful shutdown
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => Shutdown();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Shutdown();
            };

            try
            {
                if (!await TestDatabaseConnection())
                {
                    Console.WriteLine("Ошибка подключения к БД. Проверьте SQL Server и базу данных Ice_Arena.");
                    Console.WriteLine("Нажмите любую клавишу для выхода...");
                    Console.ReadKey();
                    return;
                }

                _listener = new TcpListener(IPAddress.Parse(Ip), Port);
                _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listener.Start();

                Console.WriteLine($"✅ Сервер запущен на {Ip}:{Port}");
                Console.WriteLine($"✅ БД: Ice_Arena на DESKTOP-I80K0OH\\SQLEXPRESS");
                Console.WriteLine("✅ Режим: JSON API");
                Console.WriteLine("📋 Доступные команды: login, register, get_schedule, book_session, create_booking, etc.");
                Console.WriteLine("⏹️  Для остановки сервера нажмите Ctrl+C");
                Console.WriteLine("----------------------------------------");

                // Основной цикл обработки клиентов
                while (_isRunning)
                {
                    try
                    {
                        TcpClient client = await _listener.AcceptTcpClientAsync();
                        if (!_isRunning) break;

                        Console.WriteLine($"🔌 Клиент подключился: {client.Client.RemoteEndPoint}");
                        lock (_connectedClients)
                        {
                            _connectedClients.Add(client);
                        }
                        _ = Task.Run(() => HandleClient(client));
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (SocketException ex) when (!_isRunning)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (_isRunning)
                        {
                            Console.WriteLine($"❌ Ошибка при принятии подключения: {ex.Message}");
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"❌ Сетевая ошибка: {ex.Message}");
                Console.WriteLine($"🔧 Код ошибки: {ex.SocketErrorCode}");

                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    Console.WriteLine($"⚠️  Порт {Port} уже занят. Возможно, сервер уже запущен.");
                    Console.WriteLine("💡 Попробуйте:");
                    Console.WriteLine("1. Подождать 1-2 минуты и перезапустить");
                    Console.WriteLine("2. Использовать другой порт в настройках");
                    Console.WriteLine($"3. Найти и завершить процесс, использующий порт {Port}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Критическая ошибка сервера: {ex.Message}");
            }
            finally
            {
                Shutdown();
            }
        }

        private static void Shutdown()
        {
            if (!_isRunning) return;

            _isRunning = false;
            Console.WriteLine("🛑 Завершение работы сервера...");

            try
            {
                // Останавливаем прием новых подключений
                _listener?.Stop();
                Console.WriteLine("✅ Прослушиватель остановлен");

                // Закрываем все активные подключения
                lock (_connectedClients)
                {
                    foreach (var client in _connectedClients)
                    {
                        try
                        {
                            client?.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ Ошибка при закрытии клиентского соединения: {ex.Message}");
                        }
                    }
                    _connectedClients.Clear();
                }
                Console.WriteLine("✅ Все клиентские соединения закрыты");

                // Даем время для завершения операций
                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Ошибка при завершении работы: {ex.Message}");
            }

            Console.WriteLine("✅ Сервер остановлен корректно");
        }

        private static async Task<bool> TestDatabaseConnection()
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();
                Console.WriteLine("✅ Подключение к БД успешно.");

                string checkTablesSql = @"
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME IN ('Users', 'Schedule', 'Bookings', 'Reviews')";

                using var cmd = new SqlCommand(checkTablesSql, conn);
                int tableCount = (int)await cmd.ExecuteScalarAsync();
                Console.WriteLine($"📊 Найдено таблиц: {tableCount}/4");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка подключения к БД: {ex.Message}");
                return false;
            }
        }

        private static async void HandleClient(TcpClient client)
        {
            NetworkStream stream = null;
            try
            {
                stream = client.GetStream();
                byte[] buffer = new byte[4096];

                while (_isRunning)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Клиент отключился

                    string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // Фильтруем HTTP запросы от браузера - отправляем простой ответ вместо закрытия
                    if (IsHttpRequest(request))
                    {
                        Console.WriteLine($"🌐 Получен HTTP запрос - отправляем текстовый ответ");
                        await SendHttpResponse(stream, "Ice Arena JSON API Server is running\n\nUse the Windows Forms client application.");
                        continue;
                    }

                    Console.WriteLine($"📨 Получен JSON запрос: {request.Substring(0, Math.Min(request.Length, 200))}...");
                    await HandleJsonRequest(stream, request);
                }
            }
            catch (IOException ex) when (ex.InnerException is SocketException)
            {
                Console.WriteLine($"🔌 Соединение разорвано клиентом: {ex.Message}");
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    Console.WriteLine($"❌ Ошибка обработки клиента: {ex.Message}");
                }
            }
            finally
            {
                try
                {
                    stream?.Close();
                    lock (_connectedClients)
                    {
                        _connectedClients.Remove(client);
                    }
                    client.Close();
                    Console.WriteLine($"🔌 Клиент отключен");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Ошибка при закрытии соединения: {ex.Message}");
                }
            }
        }

        private static async Task SendHttpResponse(NetworkStream stream, string message)
        {
            try
            {
                string httpResponse = $"HTTP/1.1 200 OK\r\n" +
                                     $"Content-Type: text/plain; charset=utf-8\r\n" +
                                     $"Content-Length: {Encoding.UTF8.GetByteCount(message)}\r\n" +
                                     "Connection: close\r\n" +
                                     "\r\n" +
                                     message;

                byte[] responseBytes = Encoding.UTF8.GetBytes(httpResponse);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                Console.WriteLine("📤 Отправлен HTTP ответ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отправки HTTP ответа: {ex.Message}");
            }
        }

        private static bool IsHttpRequest(string request)
        {
            return request.StartsWith("GET") ||
                   request.StartsWith("POST") ||
                   request.StartsWith("PUT") ||
                   request.StartsWith("DELETE") ||
                   request.StartsWith("HTTP/") ||
                   request.Contains("User-Agent:") ||
                   request.Contains("Mozilla/") ||
                   request.Contains("Browser/");
        }

        private static async Task HandleJsonRequest(NetworkStream stream, string request)
        {
            try
            {
                string messageJson = ExtractJsonFromRequest(request);

                if (string.IsNullOrWhiteSpace(messageJson))
                {
                    await SendJsonResponse(stream, new { Success = false, Error = "Empty JSON request" });
                    return;
                }

                Console.WriteLine($"🔧 Обработка JSON: {messageJson}");

                using JsonDocument doc = JsonDocument.Parse(messageJson);
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("Command", out JsonElement cmd))
                {
                    await SendJsonResponse(stream, new { Success = false, Error = "Отсутствует команда" });
                    return;
                }

                string command = cmd.GetString();
                Console.WriteLine($"🎯 Выполнение команды: {command}");

                object response = command switch
                {
                    "login" => await HandleLogin(root),
                    "register" => await HandleRegister(root),
                    "get_schedule" => await HandleGetSchedule(root),
                    "book_session" => await HandleBookSession(root),
                    "create_booking" => await HandleCreateBooking(root),
                    "get_user_bookings" => await HandleGetUserBookings(root),
                    "get_reviews" => await HandleGetReviews(root),
                    "add_review" => await HandleAddReview(root),
                    "get_user_info" => await HandleGetUserInfo(root),
                    "cancel_booking" => await HandleCancelBooking(root),
                    "get_arena_metrics" => await HandleGetArenaMetrics(root),
                    "get_user_profile" => await HandleGetUserProfile(root),
                    "get_user_reviews" => await HandleGetUserReviews(root),
                    "test" => new { Success = true, Message = "Сервер работает", Timestamp = DateTime.Now },
                    _ => new { Success = false, Error = "Неизвестная команда" }
                };

                await SendJsonResponse(stream, response);
            }
            catch (JsonException ex)
            {
                await SendJsonResponse(stream, new { Success = false, Error = "Неверный JSON: " + ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при обработке JSON запроса: {ex.Message}");
                await SendJsonResponse(stream, new { Success = false, Error = "Ошибка сервера: " + ex.Message });
            }
        }

        private static string ExtractJsonFromRequest(string request)
        {
            int jsonStart = request.IndexOf("\r\n\r\n");
            if (jsonStart >= 0)
            {
                return request.Substring(jsonStart + 4).Trim();
            }

            if (request.TrimStart().StartsWith("{"))
            {
                return request.Trim();
            }

            return request;
        }

        private static async Task<object> HandleLogin(JsonElement root)
        {
            if (!root.TryGetProperty("Email", out JsonElement emailElem) ||
                !root.TryGetProperty("Password", out JsonElement passElem))
            {
                return new { Success = false, Error = "Отсутствует Email или Password" };
            }

            string email = emailElem.GetString()?.Trim();
            string encryptedPassword = passElem.GetString();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(encryptedPassword))
                return new { Success = false, Error = "Email и пароль не могут быть пустыми" };

            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                // РАСШИФРОВЫВАЕМ пароль от клиента
                string decryptedPassword = ServerEncryptionHelper.Decrypt(encryptedPassword);

                if (string.IsNullOrEmpty(decryptedPassword))
                {
                    return new { Success = false, Error = "Ошибка расшифровки пароля" };
                }

                // Хешируем расшифрованный пароль для сравнения с БД
                byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(decryptedPassword));
                string passwordHash = Convert.ToBase64String(hash);

                string sql = @"SELECT Id, Email, Role, PasswordHash 
              FROM Users 
              WHERE LOWER(Email) = LOWER(@Email) AND PasswordHash = @PasswordHash";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

                using var reader = await cmd.ExecuteReaderAsync();
                if (reader.Read())
                {
                    int userId = reader.GetInt32(0);
                    string userEmail = reader.GetString(1);
                    string role = reader.GetString(2);

                    Console.WriteLine($"✅ Успешный вход: {userEmail} ({role})");

                    return new
                    {
                        Success = true,
                        Role = role,
                        UserId = userId,
                        Email = userEmail
                    };
                }
                else
                {
                    reader.Close();
                    string checkEmailSql = "SELECT COUNT(*) FROM Users WHERE LOWER(Email) = LOWER(@Email)";
                    using var checkCmd = new SqlCommand(checkEmailSql, conn);
                    checkCmd.Parameters.AddWithValue("@Email", email);
                    int emailExists = (int)await checkCmd.ExecuteScalarAsync();

                    if (emailExists > 0)
                    {
                        Console.WriteLine($"❌ Неудачный вход: {email} (неверный пароль)");
                        return new { Success = false, Error = "Неверный пароль" };
                    }
                    else
                    {
                        Console.WriteLine($"❌ Неудачный вход: {email} (пользователь не найден)");
                        return new { Success = false, Error = "Пользователь с таким email не найден" };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка БД при входе: {ex.Message}");
                return new { Success = false, Error = "Ошибка сервера при входе" };
            }
        }

        private static async Task<object> HandleRegister(JsonElement root)
        {
            if (!root.TryGetProperty("Email", out JsonElement emailElem) ||
                !root.TryGetProperty("Password", out JsonElement passElem))
            {
                return new { Success = false, Error = "Отсутствуют поля: Email или Password" };
            }

            string email = emailElem.GetString()?.Trim();
            string encryptedPassword = passElem.GetString();
            string role = "Client";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(encryptedPassword))
                return new { Success = false, Error = "Email и пароль не могут быть пустыми" };

            if (!IsValidEmail(email))
                return new { Success = false, Error = "Некорректный формат email" };

            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                string checkSql = "SELECT COUNT(*) FROM Users WHERE LOWER(Email) = LOWER(@Email)";
                using (var checkCmd = new SqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@Email", email);
                    int count = (int)await checkCmd.ExecuteScalarAsync();
                    if (count > 0)
                        return new { Success = false, Error = "Пользователь с таким email уже существует" };
                }

                // РАСШИФРОВЫВАЕМ пароль и хешируем его для хранения в БД
                string decryptedPassword = ServerEncryptionHelper.Decrypt(encryptedPassword);
                if (string.IsNullOrEmpty(decryptedPassword))
                {
                    return new { Success = false, Error = "Ошибка расшифровки пароля" };
                }

                // Хешируем пароль для безопасного хранения
                byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(decryptedPassword));
                string passwordHash = Convert.ToBase64String(hash);

                string insertSql = @"INSERT INTO Users (Email, PasswordHash, Role, RegDate) 
                           VALUES (@Email, @PasswordHash, @Role, GETDATE()); 
                           SELECT SCOPE_IDENTITY();";

                using var insertCmd = new SqlCommand(insertSql, conn);
                insertCmd.Parameters.AddWithValue("@Email", email);
                insertCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                insertCmd.Parameters.AddWithValue("@Role", role);

                int newUserId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
                Console.WriteLine($"✅ Успешная регистрация: {email}, ID: {newUserId}");

                return new
                {
                    Success = true,
                    Message = "Регистрация успешна",
                    UserId = newUserId,
                    Role = role
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка БД при регистрации: {ex.Message}");
                return new { Success = false, Error = "Ошибка при регистрации. Попробуйте позже." };
            }
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<object> HandleGetUserProfile(JsonElement root)
        {
            if (!root.TryGetProperty("UserId", out JsonElement userIdElem))
            {
                return new { Success = false, Error = "Отсутствует UserId" };
            }

            int userId = userIdElem.GetInt32();

            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                string sql = @"SELECT Id, Email, Role, RegDate 
                              FROM Users 
                              WHERE Id = @UserId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (reader.Read())
                {
                    return new
                    {
                        Success = true,
                        User = new
                        {
                            Id = reader.GetInt32(0),
                            Email = reader.GetString(1),
                            Role = reader.GetString(2),
                            RegDate = reader.GetDateTime(3).ToString("yyyy-MM-dd")
                        }
                    };
                }
                else
                {
                    return new { Success = false, Error = "Пользователь не найден" };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка БД (get_user_profile): {ex.Message}");
                return new { Success = false, Error = "Ошибка при получении профиля" };
            }
        }

        private static async Task<object> HandleGetUserReviews(JsonElement root)
        {
            if (!root.TryGetProperty("UserId", out JsonElement userIdElem))
            {
                return new { Success = false, Error = "Отсутствует UserId" };
            }

            int userId = userIdElem.GetInt32();

            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                string sql = @"SELECT Id, UserId, Rating, Text, Date, IsApproved
                      FROM Reviews 
                      WHERE UserId = @UserId
                      ORDER BY Date DESC";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);
                using var reader = await cmd.ExecuteReaderAsync();

                var reviews = new List<object>();
                while (reader.Read())
                {
                    reviews.Add(new
                    {
                        Id = reader.GetInt32(0),
                        UserId = reader.GetInt32(1),
                        Rating = reader.GetInt32(2),
                        Text = reader.GetString(3),
                        Date = reader.GetDateTime(4).ToString("yyyy-MM-dd HH:mm"),
                        IsApproved = reader.GetBoolean(5)
                    });
                }

                return new { Success = true, Reviews = reviews };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка БД (get_user_reviews): {ex.Message}");
                return new { Success = false, Error = "Ошибка при получении отзывов пользователя" };
            }
        }

        private static async Task<object> HandleGetSchedule(JsonElement root)
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                string sql = @"SELECT Id, Date, TimeSlot, BreakSlot, DayOfWeek, Capacity, AvailableSeats, Status 
                      FROM Schedule 
                      WHERE Date >= CAST(GETDATE() AS DATE) 
                      ORDER BY Date, TimeSlot";

                using var cmd = new SqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var schedule = new List<object>();
                while (reader.Read())
                {
                    schedule.Add(new
                    {
                        Id = reader.GetInt32(0),
                        Date = reader.GetDateTime(1).ToString("yyyy-MM-dd"),
                        TimeSlot = reader.GetString(2),
                        BreakSlot = reader.GetString(3),
                        DayOfWeek = reader.GetString(4),
                        Capacity = reader.GetInt32(5),
                        AvailableSeats = reader.GetInt32(6),
                        Status = reader.GetString(7)
                    });
                }

                return new { Success = true, Schedule = schedule };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка БД (get_schedule): {ex.Message}");
                return new { Success = false, Error = "Ошибка при получении расписания" };
            }
        }

        private static async Task<object> HandleBookSession(JsonElement root)
        {
            if (!root.TryGetProperty("UserId", out JsonElement userIdElem) ||
                !root.TryGetProperty("ScheduleId", out JsonElement scheduleIdElem))
            {
                return new { Success = false, Error = "Отсутствуют UserId или ScheduleId" };
            }

            int userId = userIdElem.GetInt32();
            int scheduleId = scheduleIdElem.GetInt32();

            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                using var transaction = conn.BeginTransaction();
                try
                {
                    string checkSql = "SELECT AvailableSeats FROM Schedule WHERE Id = @ScheduleId";
                    using var checkCmd = new SqlCommand(checkSql, conn, transaction);
                    checkCmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                    int availableSeats = (int)await checkCmd.ExecuteScalarAsync();

                    if (availableSeats <= 0)
                    {
                        return new { Success = false, Error = "Нет свободных мест на этот сеанс" };
                    }

                    string insertSql = "INSERT INTO Bookings (UserId, ScheduleId, Status, BookingDate) VALUES (@UserId, @ScheduleId, 'Booked', GETDATE()); SELECT SCOPE_IDENTITY();";
                    using var insertCmd = new SqlCommand(insertSql, conn, transaction);
                    insertCmd.Parameters.AddWithValue("@UserId", userId);
                    insertCmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                    int newBookingId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

                    string updateSql = "UPDATE Schedule SET AvailableSeats = AvailableSeats - 1 WHERE Id = @ScheduleId";
                    using var updateCmd = new SqlCommand(updateSql, conn, transaction);
                    updateCmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                    await updateCmd.ExecuteNonQueryAsync();

                    transaction.Commit();
                    Console.WriteLine($"✅ Бронирование создано: UserId={userId}, ScheduleId={scheduleId}, BookingId={newBookingId}");
                    return new { Success = true, Message = "Бронирование успешно создано", BookingId = newBookingId };
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка БД (book_session): {ex.Message}");
                return new { Success = false, Error = "Ошибка при бронировании" };
            }
        }

        // НОВЫЙ МЕТОД ДЛЯ ОБРАБОТКИ create_booking
        private static async Task<object> HandleCreateBooking(JsonElement root)
        {
            if (!root.TryGetProperty("UserId", out JsonElement userIdElem) ||
                !root.TryGetProperty("ScheduleId", out JsonElement scheduleIdElem) ||
                !root.TryGetProperty("TicketsCount", out JsonElement ticketsCountElem))
            {
                return new { Success = false, Error = "Отсутствуют обязательные поля: UserId, ScheduleId или TicketsCount" };
            }

            int userId = userIdElem.GetInt32();
            int scheduleId = scheduleIdElem.GetInt32();
            int ticketsCount = ticketsCountElem.GetInt32();

            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                using var transaction = conn.BeginTransaction();
                try
                {
                    string checkSql = "SELECT AvailableSeats FROM Schedule WHERE Id = @ScheduleId";
                    using var checkCmd = new SqlCommand(checkSql, conn, transaction);
                    checkCmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                    int availableSeats = (int)await checkCmd.ExecuteScalarAsync();

                    if (availableSeats < ticketsCount)
                    {
                        return new { Success = false, Error = $"Недостаточно свободных мест. Доступно: {availableSeats}, запрошено: {ticketsCount}" };
                    }

                    string insertSql = "INSERT INTO Bookings (UserId, ScheduleId, Status, BookingDate) VALUES (@UserId, @ScheduleId, 'Booked', GETDATE()); SELECT SCOPE_IDENTITY();";
                    using var insertCmd = new SqlCommand(insertSql, conn, transaction);
                    insertCmd.Parameters.AddWithValue("@UserId", userId);
                    insertCmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                    int newBookingId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

                    string updateSql = "UPDATE Schedule SET AvailableSeats = AvailableSeats - @TicketsCount WHERE Id = @ScheduleId";
                    using var updateCmd = new SqlCommand(updateSql, conn, transaction);
                    updateCmd.Parameters.AddWithValue("@TicketsCount", ticketsCount);
                    updateCmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                    await updateCmd.ExecuteNonQueryAsync();

                    transaction.Commit();
                    Console.WriteLine($"✅ Бронирование создано: UserId={userId}, ScheduleId={scheduleId}, BookingId={newBookingId}, Билетов: {ticketsCount}");
                    return new { Success = true, Message = "Бронирование успешно создано", BookingId = newBookingId };
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка БД (create_booking): {ex.Message}");
                return new { Success = false, Error = "Ошибка при бронировании" };
            }
        }

        private static async Task<object> HandleGetUserBookings(JsonElement root)
        {
            if (!root.TryGetProperty("UserId", out JsonElement userIdElem))
            {
                return new { Success = false, Error = "Отсутствует UserId" };
            }

            int userId = userIdElem.GetInt32();

            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                string sql = @"SELECT b.Id, s.Date, s.TimeSlot, s.BreakSlot, s.DayOfWeek, b.Status, b.BookingDate
                      FROM Bookings b
                      JOIN Schedule s ON b.ScheduleId = s.Id
                      WHERE b.UserId = @UserId
                      ORDER BY s.Date DESC, s.TimeSlot";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);
                using var reader = await cmd.ExecuteReaderAsync();

                var bookings = new List<object>();
                while (reader.Read())
                {
                    bookings.Add(new
                    {
                        BookingId = reader.GetInt32(0),
                        Date = reader.GetDateTime(1).ToString("yyyy-MM-dd"),
                        TimeSlot = reader.GetString(2),
                        BreakSlot = reader.GetString(3),
                        DayOfWeek = reader.GetString(4),
                        Status = reader.GetString(5),
                        BookingDate = reader.GetDateTime(6).ToString("yyyy-MM-dd HH:mm")
                    });
                }

                return new { Success = true, Bookings = bookings };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка БД (get_user_bookings): {ex.Message}");
                return new { Success = false, Error = "Ошибка при получении бронирований" };
            }
        }

        private static async Task<object> HandleGetReviews(JsonElement root)
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                string sql = @"SELECT r.Id, u.Email, r.Rating, r.Text, r.Date, r.IsApproved
                      FROM Reviews r
                      JOIN Users u ON r.UserId = u.Id
                      WHERE r.IsApproved = 1
                      ORDER BY r.Date DESC";

                using var cmd = new SqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var reviews = new List<object>();
                while (reader.Read())
                {
                    reviews.Add(new
                    {
                        Id = reader.GetInt32(0),
                        UserEmail = reader.GetString(1),
                        Rating = reader.GetByte(2),
                        Text = reader.GetString(3),
                        Date = reader.GetDateTime(4).ToString("yyyy-MM-dd HH:mm"),
                        IsApproved = reader.GetBoolean(5)
                    });
                }

                return new { Success = true, Reviews = reviews };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка БД (get_reviews): {ex.Message}");
                return new { Success = false, Error = "Ошибка при получении отзывов" };
            }
        }

        private static async Task<object> HandleAddReview(JsonElement root)
        {
            if (!root.TryGetProperty("UserId", out JsonElement userIdElem) ||
                !root.TryGetProperty("Rating", out JsonElement ratingElem) ||
                !root.TryGetProperty("Text", out JsonElement textElem))
            {
                return new { Success = false, Error = "Отсутствуют обязательные поля: UserId, Rating, Text" };
            }

            int userId = userIdElem.GetInt32();
            int rating = ratingElem.GetInt32();
            string text = textElem.GetString();

            if (rating < 1 || rating > 5)
                return new { Success = false, Error = "Рейтинг должен быть от 1 до 5" };

            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                string sql = "INSERT INTO Reviews (UserId, Rating, Text, Date, IsApproved) VALUES (@UserId, @Rating, @Text, GETDATE(), 1); SELECT SCOPE_IDENTITY();";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Rating", rating);
                cmd.Parameters.AddWithValue("@Text", text);

                int reviewId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                Console.WriteLine($"✅ Добавлен отзыв: UserId={userId}, Rating={rating}, ReviewId={reviewId}");
                return new { Success = true, Message = "Отзыв успешно добавлен", ReviewId = reviewId };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка БД (add_review): {ex.Message}");
                return new { Success = false, Error = "Ошибка при добавлении отзыва" };
            }
        }

        private static async Task<object> HandleGetUserInfo(JsonElement root)
        {
            if (!root.TryGetProperty("UserId", out JsonElement userIdElem))
            {
                return new { Success = false, Error = "Отсутствует UserId" };
            }

            int userId = userIdElem.GetInt32();

            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                string sql = "SELECT Id, Email, Role, RegDate FROM Users WHERE Id = @UserId";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (reader.Read())
                {
                    return new
                    {
                        Success = true,
                        User = new
                        {
                            Id = reader.GetInt32(0),
                            Email = reader.GetString(1),
                            Role = reader.GetString(2),
                            RegDate = reader.GetDateTime(3).ToString("yyyy-MM-dd")
                        }
                    };
                }
                else
                {
                    return new { Success = false, Error = "Пользователь не найден" };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка БД (get_user_info): {ex.Message}");
                return new { Success = false, Error = "Ошибка при получении информации о пользователе" };
            }
        }

        private static async Task<object> HandleCancelBooking(JsonElement root)
        {
            if (!root.TryGetProperty("BookingId", out JsonElement bookingIdElem))
            {
                return new { Success = false, Error = "Отсутствует BookingId" };
            }

            int bookingId = bookingIdElem.GetInt32();

            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                using var transaction = conn.BeginTransaction();
                try
                {
                    string getScheduleSql = "SELECT ScheduleId FROM Bookings WHERE Id = @BookingId";
                    using var getCmd = new SqlCommand(getScheduleSql, conn, transaction);
                    getCmd.Parameters.AddWithValue("@BookingId", bookingId);
                    var scheduleIdObj = await getCmd.ExecuteScalarAsync();

                    if (scheduleIdObj == null)
                    {
                        return new { Success = false, Error = "Бронирование не найдено" };
                    }

                    int scheduleId = (int)scheduleIdObj;

                    string cancelSql = "UPDATE Bookings SET Status = 'Cancelled' WHERE Id = @BookingId";
                    using var cancelCmd = new SqlCommand(cancelSql, conn, transaction);
                    cancelCmd.Parameters.AddWithValue("@BookingId", bookingId);
                    await cancelCmd.ExecuteNonQueryAsync();

                    string returnSeatSql = "UPDATE Schedule SET AvailableSeats = AvailableSeats + 1 WHERE Id = @ScheduleId";
                    using var returnCmd = new SqlCommand(returnSeatSql, conn, transaction);
                    returnCmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                    await returnCmd.ExecuteNonQueryAsync();

                    transaction.Commit();
                    Console.WriteLine($"✅ Бронирование отменено: BookingId={bookingId}");
                    return new { Success = true, Message = "Бронирование успешно отменено" };
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка БД (cancel_booking): {ex.Message}");
                return new { Success = false, Error = "Ошибка при отмене бронирования" };
            }
        }

        private static async Task<object> HandleGetArenaMetrics(JsonElement root)
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                string sql = @"SELECT TOP 30 Date, Income, Attendance, Electricity, Notes 
                              FROM ArenaMetrics 
                              ORDER BY Date DESC";

                using var cmd = new SqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var metrics = new List<object>();
                while (reader.Read())
                {
                    metrics.Add(new
                    {
                        Date = reader.GetDateTime(0).ToString("yyyy-MM-dd"),
                        Income = reader.GetDecimal(1),
                        Attendance = reader.GetInt32(2),
                        Electricity = reader.GetDecimal(3),
                        Notes = reader.IsDBNull(4) ? "" : reader.GetString(4)
                    });
                }

                return new { Success = true, Metrics = metrics };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка БД (get_arena_metrics): {ex.Message}");
                return new { Success = false, Error = "Ошибка при получении метрик" };
            }
        }

        private static async Task SendJsonResponse(NetworkStream stream, object response)
        {
            try
            {
                string json = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                await stream.WriteAsync(bytes, 0, bytes.Length);
                Console.WriteLine($"📤 Отправлен JSON ответ: {json.Substring(0, Math.Min(json.Length, 100))}...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отправки ответа: {ex.Message}");
            }
        }
    }
}