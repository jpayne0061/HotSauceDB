using SharpDb.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpDb.Services
{
    public class Writer
    {
        public void WriteRow(object[] row, long diskLocation, TableDefinition tableDefinition)
        {

            using (FileStream fileStream = File.OpenWrite(Globals.FILE_NAME))
            {
                fileStream.Position = diskLocation;

                //var rowsSizeInBytes = NumberOfRows * tableDefinition.GetRowSizeInBytes();

                using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                {
                    for (var i = 0; i < row.Length; i++)
                    {
                        WriteColumnData(binaryWriter, row[i], tableDefinition.ColumnDefinitions[i]);
                    }
                    //NumberOfRows += 1;
                }
            }
        }

        public void WriteColumnData(BinaryWriter binaryWriter, object data, ColumnDefinition columnDefinition)
        {
            if (data is bool)
            {
                binaryWriter.Write((bool)data);
            }
            else if (data is char)
            {
                binaryWriter.Write((char)data);
            }
            else if (data is decimal)
            {
                binaryWriter.Write((bool)data);
            }
            else if (data is Int32)
            {
                binaryWriter.Write((Int32)data);
            }
            else if (data is Int64)
            {
                binaryWriter.Write((Int64)data);
            }
            else if (data is string)
            {
                var x = ((string)data).PadRight(columnDefinition.ByteSize - 1, ' ');
                binaryWriter.Write(x);
            }
        }

        public void WriteTableDefinition(TableDefinition tableDefinition)
        {
            var newDefinitionAddress = FindSpotForNewTableDefinition();

            var nextFreeDataPage = GetNextUnclaimedDataPage();

            using (FileStream stream = File.OpenWrite(Globals.FILE_NAME))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(stream))
                {
                    binaryWriter.BaseStream.Position = newDefinitionAddress;// lastTableDefAddress;

                    binaryWriter.Write(nextFreeDataPage); //need address of table defintion - where will it live?
                    binaryWriter.Write(tableDefinition.TableName);

                    foreach (var col in tableDefinition.ColumnDefinitions)
                    {
                        binaryWriter.Write(col.ColumnName);
                        binaryWriter.Write(col.Index);
                        binaryWriter.Write(col.Type);
                        binaryWriter.Write(col.ByteSize);
                    }

                    //binaryWriter.BaseStream.Position = newDefinitionAddress + 529;

                    binaryWriter.Write(Globals.EndTableDefinition);
                }
            }
        }

        private long FindSpotForNewTableDefinition()
        {
            using (FileStream stream = File.OpenRead(Globals.FILE_NAME))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    reader.BaseStream.Position = 0;

                    while (reader.PeekChar() != -1 && reader.PeekChar() != 0)
                    {
                        reader.BaseStream.Position += 530;
                    }

                    return reader.BaseStream.Position;
                }
            }
        }

        private long GetNextUnclaimedDataPage()
        {
            Reader reader = new Reader();

            var indexPage = reader.GetIndexPage();

            long headAddress = indexPage.TableDefinitions.Count == 0 ? 8000 :
                                indexPage.TableDefinitions.Max(x => x.DataAddress) + 8000;

            long nextFreeAddress = headAddress;

            using (FileStream stream = File.OpenRead(Globals.FILE_NAME))
            {
                using (BinaryReader binaryReader = new BinaryReader(stream))
                {
                    binaryReader.BaseStream.Position = headAddress;

                    while (binaryReader.PeekChar() != -1)
                    {
                        binaryReader.BaseStream.Position += 8000;
                    }

                    return binaryReader.BaseStream.Position;
                }
            }

        }

        public long GetFirstAvailableAddress()
        {
            //use table map

            return 8000;
        }

        public BinaryWriter GetBinaryWriter()
        {
            using (FileStream stream = File.OpenWrite(Globals.FILE_NAME))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(stream))
                {
                    return binaryWriter;
                }
            }
        }

        public void WriteRow(string tableName, object[] row)
        {
            DataPage dataPage = new DataPage();

            Reader reader = new Reader();
            var indexPage = reader.GetIndexPage();

            long addressToWrite = reader.GetFirstAvailableDataAddressOfTableByName(tableName, indexPage.TableDefinitions);

            TableDefinition tableDefinition = indexPage.TableDefinitions
                .Where(x => x.TableName == tableName).FirstOrDefault();

            dataPage.WriteRow(row, addressToWrite, tableDefinition);
        }

    }
}
