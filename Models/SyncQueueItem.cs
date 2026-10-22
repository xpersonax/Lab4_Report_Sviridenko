using SQLite;

namespace RailwayErp.MvvmDemo.Models;

/// <summary>Запис черги відкладених змін (offline-first, патерн «outbox»).</summary>
[Table("SyncQueue")]
public class SyncQueueItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityUid { get; set; }

    /// <summary>Insert, Update або Delete (див. <see cref="SyncOperation"/>).</summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>JSON-знімок сутності (чутливі поля вже зашифровані).</summary>
    public string PayloadJson { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public static class SyncOperation
{
    public const string Insert = "Insert";
    public const string Update = "Update";
    public const string Delete = "Delete";
}
