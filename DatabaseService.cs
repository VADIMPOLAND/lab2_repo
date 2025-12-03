using System.Text.Json;
using IceArena.Client;

public class DatabaseService
{
    private ClientForm parentForm;

    public DatabaseService(ClientForm parent)
    {
        parentForm = parent;
    }

    public async Task<int> GetAvailableSeats(int scheduleId)
    {
        try
        {
            // Вместо отдельного запроса, загрузим все расписание и найдем нужный слот
            var response = await parentForm.SendServerRequest(new
            {
                Command = "get_schedule"
            });

            if (response.ValueKind == JsonValueKind.Object &&
                response.TryGetProperty("Success", out var successElement) &&
                successElement.GetBoolean())
            {
                if (response.TryGetProperty("Schedule", out var scheduleArray) &&
                    scheduleArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in scheduleArray.EnumerateArray())
                    {
                        if (item.TryGetProperty("Id", out var idElement) &&
                            idElement.GetInt32() == scheduleId)
                        {
                            if (item.TryGetProperty("AvailableSeats", out var seatsElement))
                            {
                                return seatsElement.GetInt32();
                            }
                            // Если AvailableSeats нет, используем Capacity
                            else if (item.TryGetProperty("Capacity", out var capacityElement))
                            {
                                return capacityElement.GetInt32();
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при получении количества мест: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // Если ничего не нашли, возвращаем дефолтное значение
        return 50;
    }

    public async Task<bool> DecreaseAvailableSeats(int scheduleId, int count)
    {
        try
        {
            var response = await parentForm.SendServerRequest(new
            {
                Command = "decrease_available_seats",
                ScheduleId = scheduleId,
                Count = count
            });

            return response.TryGetProperty("Success", out var successElement) && successElement.GetBoolean();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при уменьшении мест: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        return false;
    }

    public async Task IncreaseAvailableSeats(int scheduleId, int count)
    {
        try
        {
            await parentForm.SendServerRequest(new
            {
                Command = "increase_available_seats",
                ScheduleId = scheduleId,
                Count = count
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при увеличении мест: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public async Task<int> CreateBooking(int userId, int scheduleId, DateTime bookingDate, string status)
    {
        try
        {
            var response = await parentForm.SendServerRequest(new
            {
                Command = "create_booking",
                UserId = userId,
                ScheduleId = scheduleId,
                BookingDate = bookingDate.ToString("yyyy-MM-dd HH:mm:ss"),
                Status = status
            });

            if (response.TryGetProperty("Success", out var successElement) && successElement.GetBoolean())
            {
                if (response.TryGetProperty("BookingId", out var idElement))
                {
                    return idElement.GetInt32();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при создании бронирования: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        return 0;
    }

    public async Task<List<(int ticketId, int quantity)>> CreateTickets(int bookingId, List<Ticket> tickets)
    {
        var result = new List<(int, int)>();
        try
        {
            var response = await parentForm.SendServerRequest(new
            {
                Command = "create_tickets",
                BookingId = bookingId,
                Tickets = tickets.Select(t => new
                {
                    Type = t.Type,
                    Quantity = t.Quantity,
                    Price = t.Price
                }).ToList()
            });

            if (response.TryGetProperty("Success", out var successElement) && successElement.GetBoolean())
            {
                if (response.TryGetProperty("Tickets", out var ticketsArray) && ticketsArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var ticketItem in ticketsArray.EnumerateArray())
                    {
                        if (ticketItem.TryGetProperty("TicketId", out var ticketIdElement) &&
                            ticketItem.TryGetProperty("Quantity", out var quantityElement))
                        {
                            result.Add((ticketIdElement.GetInt32(), quantityElement.GetInt32()));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при создании билетов: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        return result;
    }

    public async Task CreateRental(int ticketId, string skateSize, string skateType)
    {
        try
        {
            await parentForm.SendServerRequest(new
            {
                Command = "create_rental",
                TicketId = ticketId,
                SkateSize = skateSize,
                SkateType = skateType
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при создании проката: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}