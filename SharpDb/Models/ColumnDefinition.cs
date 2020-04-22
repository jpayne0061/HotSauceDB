namespace SharpDb.Models
{
    public class ColumnDefinition
    {
        public string ColumnName { get; set; } //20 bytes (length 10)
        public byte Index { get; set; } //1 byte
        public byte Type { get; set; } //1 byte
        public short ByteSize { get; set; } //2 bytes (limit 7k bytes)
    }
}
