using SharpDb.Cache;
using SharpDb.Helpers;
using SharpDb.Models;
using System;
using System.IO;
using System.Linq;

namespace SharpDb.Services
{
    public class Writer
    {
        public void WriteRow(object[] row, long diskLocation, TableDefinition tableDefinition)
        {
            long addressToWriteTo = EndOfPageCheck(diskLocation, tableDefinition.GetRowSizeInBytes());

            //if first row, write pointer as zero
            if((addressToWriteTo - 2) % Globals.PageSize == 0)
            {
                WriteZeroPointerForFirstRow(addressToWriteTo);
            }

            using (FileStream fileStream = File.OpenWrite(Globals.FILE_NAME))
            {
                fileStream.Position = addressToWriteTo;

                using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                {
                    for (var i = 0; i < row.Length; i++)
                    {
                        WriteColumnData(binaryWriter, row[i], tableDefinition.ColumnDefinitions[i]);
                    }
                }
            }
        }

        public void WriteZeroPointerForFirstRow(long currentAddress)
        {
            if((currentAddress - 2) % Globals.PageSize != 0)
            {
                throw new Exception("Invalid address for first row: " + currentAddress);
            }

            long zeroPointerAddress = currentAddress + (Globals.NextPointerAddress - 2);

            using (FileStream fileStream = File.OpenWrite(Globals.FILE_NAME))
            {
                fileStream.Position = zeroPointerAddress;

                using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                {
                    binaryWriter.Write((long)0);
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
                binaryWriter.Write((decimal)data);
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
                        binaryWriter.Write((byte)col.Type);
                        binaryWriter.Write(col.ByteSize);
                    }

                    binaryWriter.Write(Globals.EndTableDefinition);

                    stream.Position = nextFreeDataPage;
                    binaryWriter.Write((short)0);
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

        private long GetNextUnclaimedDataPage(IndexPage indexPage)
        {
            long headAddress = indexPage.TableDefinitions.Count == 0 ? 8000 :
                                indexPage.TableDefinitions.Max(x => x.DataAddress);

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

        private long GetNextUnclaimedDataPage()
        {
            var reader = new Reader();

            var indexPage = reader.GetIndexPage();

            return GetNextUnclaimedDataPage(indexPage);
        }

        public void WriteRow(object[] row, TableDefinition tableDef)
        {
            var writer = new Writer();

            var reader = new Reader();

            long addressToWrite = reader.GetFirstAvailableDataAddressOfTableByName(tableDef);

            writer.WriteRow(row, addressToWrite, tableDef);

            writer.UpdateRowCount(addressToWrite);
        }

        private void UpdateRowCount(long currentAddress)
        {
            long addressOfCount = currentAddress - (currentAddress % Globals.PageSize);

            short numRows = 0;

            using (FileStream fileStream = new FileStream(Globals.FILE_NAME, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    reader.BaseStream.Position = addressOfCount;

                    numRows = reader.ReadInt16();
                }
            }

            using (FileStream fileStream = new FileStream(Globals.FILE_NAME, FileMode.Open, FileAccess.Write, FileShare.Write))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                {
                    binaryWriter.BaseStream.Position = addressOfCount;

                    numRows += 1;

                    binaryWriter.Write(numRows);
                }
            }

        }

        /// <summary>
        /// Checks if current page is full. If so, writes a pointer to the end of the page and 
        /// returns next available writeable address. 
        /// Otherwise, return current address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private long EndOfPageCheck(long address, int rowSize)
        {
            long nextPageAddress = PageLocationHelper.GetNextDivisbleNumber(address, Globals.PageSize);

            if(address + (rowSize * 2) > nextPageAddress - 8)
            {
                long nextPagePointer = GetNextUnclaimedDataPage();

                using (FileStream fileStream = File.OpenWrite(Globals.FILE_NAME))
                {
                    using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                    {
                        binaryWriter.BaseStream.Position = nextPageAddress - 8; 

                        binaryWriter.Write(nextPagePointer);

                        binaryWriter.BaseStream.Position = nextPagePointer;

                        binaryWriter.Write((short)0);

                        //where is zero count recorded?
                        //need to write zero row count for here

                        return binaryWriter.BaseStream.Position;
                    }
                }
            }
            else
            {
                return address;
            }
        }

        public void GetPointerAddress()
        {

        }

    }
}
