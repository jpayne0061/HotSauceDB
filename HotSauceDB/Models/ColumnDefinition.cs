using SharpDb.Enums;

namespace SharpDb.Models
{
    public class ColumnDefinition
    {
        private string _columnName;

        public string ColumnName
        {
            get
            {
                return _columnName.ToLower();
            }
            set
            {
                _columnName = value;
            }
        } //20 bytes (length 10)
        public byte Index { get; set; } //1 byte
        public TypeEnum Type { get; set; } //1 byte
        public short ByteSize { get; set; } //2 bytes (limit 7k bytes)
    }
}
