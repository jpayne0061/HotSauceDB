using SharpDb.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpDb.Services
{
    public class Reader
    {

        //public TableDefinition GetTableDefinition(long diskLocation)
        //{
        //    TableDefinition tableDefinition = new TableDefinition();
        //    tableDefinition.ColumnDefinitions = new List<ColumnDefinition>();

        //    using (FileStream fileStream = File.OpenRead(Globals.FILE_NAME))
        //    {
        //        fileStream.Position = diskLocation;

        //        using (BinaryReader binaryReader = new BinaryReader(fileStream))
        //        {
        //            for (int i = 0; i < NumColumnDefinitions; i++)
        //            {
        //                var c = new ColumnDefinition();
        //                c.Index = binaryReader.ReadByte();
        //                c.Type = binaryReader.ReadByte();
        //                c.ByteSize = binaryReader.ReadInt16();
        //                c.ColumnName = binaryReader.ReadString();

        //                tableDefinition.ColumnDefinitions.Add(c);
        //            }
        //        }
        //    }

        //    return tableDefinition;
        //}

        //public List<TableDefinition> GetAllTableDefinitions()
        //{
        //    using (FileStream fileStream = File.OpenRead(Globals.FILE_NAME))
        //    {
        //        fileStream.Position = 0;

        //        using (BinaryReader binaryReader = new BinaryReader(fileStream))
        //        {
        //            for (int i = 0; i < NumColumnDefinitions; i++)
        //            {
        //                var c = new ColumnDefinition();
        //                c.Index = binaryReader.ReadByte();
        //                c.Type = binaryReader.ReadByte();
        //                c.ByteSize = binaryReader.ReadInt16();
        //                c.ColumnName = binaryReader.ReadString();

        //                tableDefinition.ColumnDefinitions.Add(c);
        //            }
        //        }
        //    }
        //}

        public IndexPage GetIndexPage()
        {
            IndexPage indexPage = new IndexPage();

            using (FileStream fileStream = File.OpenRead(Globals.FILE_NAME))
            {
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    reader.BaseStream.Position = 0;

                    while (reader.PeekChar() != -1 && reader.PeekChar() != 0) // end of all table defintions
                    {
                        var tableDefinition = new TableDefinition();
                        tableDefinition.DataAddress = reader.ReadInt64();
                        tableDefinition.TableName = reader.ReadString();

                        while (reader.PeekChar() != '|') // | signifies end of current table defintion
                        {
                            var columnDefinition = new ColumnDefinition();
                            columnDefinition.ColumnName = reader.ReadString();
                            columnDefinition.Index = reader.ReadByte();
                            columnDefinition.Type = reader.ReadByte();
                            columnDefinition.ByteSize = reader.ReadInt16();
                            tableDefinition.ColumnDefinitions.Add(columnDefinition);
                        }

                        reader.BaseStream.Position = GetNextTableDefinitionStartAddress(reader.BaseStream.Position);

                        indexPage.TableDefinitions.Add(tableDefinition);
                    }


                    return indexPage;
                }
            }

        }

        public long GetNextTableDefinitionStartAddress(long currentPosition)
        {
            //to do - make this smarter
            while(currentPosition % 530 != 0)
            {
                currentPosition++;
            }

            return currentPosition;
        }

        public List<List<object>> GetAllRows(string tableName)
        {
            var indexPage = GetIndexPage();

            var tableDefinition = indexPage.TableDefinitions.Where(x => x.TableName == tableName).FirstOrDefault();

            var rows = new List<List<object>>();

            using (FileStream fileStream = new FileStream(Globals.FILE_NAME, FileMode.Open))
            {
                fileStream.Position = tableDefinition.DataAddress;

                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    while(binaryReader.PeekChar() != -1 && binaryReader.PeekChar() != 0)
                    {
                        List<object> row = new List<object>();

                        for (int j = 0; j < tableDefinition.ColumnDefinitions.Count; j++)
                        {
                            row.Add(ReadColumn(tableDefinition.ColumnDefinitions[j], binaryReader));
                        }

                        rows.Add(row);
                    }
                }
            }

            return rows;
        }

        public object ReadColumn(ColumnDefinition columnDefintion, BinaryReader binaryReader)
        {
            switch (columnDefintion.Type)
            {
                case 0:
                    return binaryReader.ReadBoolean();
                case 1:
                    return binaryReader.ReadChar();
                case 2:
                    return binaryReader.ReadDecimal();
                case 3:
                    return binaryReader.ReadInt32();
                case 4:
                    return binaryReader.ReadInt64();
                case 5:
                    return binaryReader.ReadString();
                default:
                    throw new Exception("invalid column definition type");
            }
        }

        public long GetFirstAvailableDataAddressOfTableByName(string tableName, IEnumerable<TableDefinition> tableDefinitions)
        {
            TableDefinition tableDef = tableDefinitions.Where(x => x.TableName == tableName).FirstOrDefault();

            long address = tableDef.DataAddress;

            int rowSize = tableDef.GetRowSizeInBytes();

            using (FileStream fileStream = File.OpenRead(Globals.FILE_NAME))
            {
                fileStream.Position = address;

                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    while (binaryReader.PeekChar() != -1)
                    {
                        fileStream.Position += rowSize;
                    }

                    return fileStream.Position;
                }
            }
        }

    }
}
