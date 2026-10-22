using SQLite;

namespace RailwayErp.MvvmDemo.Models;

/// <summary>
/// Маршрут поїзда. Поле <see cref="DriverDocumentNumber"/> у БД зберігається
/// у зашифрованому вигляді (див. AesHmacFieldEncryptor); у ViewModel завжди відкритий текст.
/// </summary>
[Table("TrainRoute")]
public class TrainRoute
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Глобальний ідентифікатор для синхронізації із сервером.</summary>
    public Guid Uid { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Origin { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Destination { get; set; } = string.Empty;

    public DateTime DepartureTime { get; set; }

    [MaxLength(100)]
    public string? DriverName { get; set; }

    /// <summary>Номер службового посвідчення машиніста (чутливе поле, шифрується).</summary>
    public string? DriverDocumentNumber { get; set; }

    /// <summary>Час останньої зміни (UTC) — використовується для стратегії last write wins.</summary>
    public DateTime UpdatedAt { get; set; }

    public TrainRoute Clone() => (TrainRoute)MemberwiseClone();
}
