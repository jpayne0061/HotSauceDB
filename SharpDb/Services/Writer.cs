using SharpDb.Enums;
using SharpDb.Helpers;
using SharpDb.Models;
using System;
using System.IO;
using System.Linq;

namespace SharpDb.Services
{
    public class Writer
    {
        public void WriteRow(IComparable[] row, TableDefinition tableDef, long addressToWrite)
        {
            WriteRow(row, addressToWrite, tableDef);

            UpdateObjectCount(addressToWrite);
        }
        public void WriteRow(IComparable[] row, long diskLocation, TableDefinition tableDefinition)
        {
            long addressToWriteTo = EndOfPageCheck(diskLocation, tableDefinition.GetRowSizeInBytes(), isRow: true);

            //if first row, write pointer as zero
            if((addressToWriteTo - 2) % Globals.PageSize == 0)
            {
                WriteZeroPointerForFirstRow(addressToWriteTo);
            }

            using (FileStream fileStream = File.Open(Globals.FILE_NAME, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
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

            using (FileStream fileStream = File.Open(Globals.FILE_NAME, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                fileStream.Position = zeroPointerAddress;

                using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                {
                    binaryWriter.Write((long)0);
                }
            }
        }

        public void WriteColumnData(BinaryWriter binaryWriter, IComparable data, ColumnDefinition columnDefinition)
        {
            if (columnDefinition.Type == TypeEnum.Boolean)
            {
                binaryWriter.Write((bool)data);
            }
            else if (columnDefinition.Type == TypeEnum.Char)
            {
                binaryWriter.Write((char)data);
            }
            else if (columnDefinition.Type == TypeEnum.Decimal)
            {
                binaryWriter.Write((decimal)data);
            }
            else if (columnDefinition.Type == TypeEnum.Int32)
            {
                binaryWriter.Write((Int32)data);
            }
            else if (columnDefinition.Type == TypeEnum.Int64)
            {
                binaryWriter.Write((Int64)data);
            }
            else if (columnDefinition.Type == Enums.TypeEnum.DateTime)
            {
                long unixTime = ((DateTimeOffset)(DateTime)data).ToUnixTimeSeconds();
                binaryWriter.Write(unixTime);
            }
            else if (columnDefinition.Type == TypeEnum.String)
            {
                if(data == null)
                {
                    binaryWriter.Write("".PadRight(columnDefinition.ByteSize - 1, ' '));
                }
                else
                {
                    var x = ((string)data).PadRight(columnDefinition.ByteSize - 1, ' ');
                    binaryWriter.Write(x);
                }


            }
        }

        public ResultMessage WriteTableDefinition(TableDefinition tableDefinition)
        {
            //gte first free spot to write table def

            bool isFirstTable = IsFirstTable();

            if(isFirstTable)
            {
                WriteZero(0); 
            }
            var reader = new Reader();

            //need to pass in address of current page, not zero
            long addressToWrite = reader.GetFirstAvailableDataAddress(0, Globals.TABLE_DEF_LENGTH);

            if ((addressToWrite - 2) % Globals.PageSize == 0)
            {
                var pointerToNextIndexRecord = GetNextUnclaimedDataPage();
                WriteLong((addressToWrite - 2) + Globals.NextPointerAddress, pointerToNextIndexRecord);
                WriteZero(pointerToNextIndexRecord);
            }

            //this should return current next page, instead of the first page address
            //first page isn't really full
            var newDefinitionAddress = EndOfPageCheck(addressToWrite, Globals.TABLE_DEF_LENGTH);

            var nextFreeDataPage = GetNextUnclaimedDataPage();

            long tableDefEnd = 0;

            using (FileStream stream = File.Open(Globals.FILE_NAME, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(stream))
                {
                    binaryWriter.BaseStream.Position = newDefinitionAddress;// lastTableDefAddress;

                    binaryWriter.Write(nextFreeDataPage);
                    binaryWriter.Write(tableDefinition.TableName);

                    foreach (var col in tableDefinition.ColumnDefinitions)
                    {
                        binaryWriter.Write(col.ColumnName);
                        binaryWriter.Write(col.Index);
                        binaryWriter.Write((byte)col.Type);
                        binaryWriter.Write(col.ByteSize);
                    }

                    tableDefEnd = stream.Position;

                    binaryWriter.Write(Globals.EndTableDefinition);

                    stream.Position = nextFreeDataPage;
                    binaryWriter.Write((short)0);
                }
            }

            UpdateObjectCount(tableDefEnd);

            return new ResultMessage { Message = $"table {tableDefinition.TableName} has been added successfully" };
        }

        void WriteZero(long address)
        {
            using (FileStream stream = File.Open(Globals.FILE_NAME, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(stream))
                {
                    binaryWriter.BaseStream.Position = address;

                    binaryWriter.Write((short)0);
                }
            }
        }

        void WriteLong(long address, long num)
        {
            using (FileStream stream = File.Open(Globals.FILE_NAME, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(stream))
                {
                    binaryWriter.BaseStream.Position = address;

                    binaryWriter.Write((long)num);
                }
            }
        }

        private bool IsFirstTable()
        {
            using (FileStream stream = new FileStream(Globals.FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    reader.BaseStream.Position = 0;

                    return reader.PeekChar() == -1;
                }
            }
        }

        private long FindSpotForNewTableDefinition()
        {
            using (FileStream stream = new FileStream(Globals.FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    reader.BaseStream.Position = 2;

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

            using (FileStream stream = new FileStream(Globals.FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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

        private void UpdateObjectCount(long currentAddress)
        {
            long addressOfCount = currentAddress - (currentAddress % Globals.PageSize);

            short numRows = 0;

            using (FileStream fileStream = new FileStream(Globals.FILE_NAME, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    reader.BaseStream.Position = addressOfCount;

                    numRows = reader.ReadInt16();
                }
            }

            using (FileStream fileStream = new FileStream(Globals.FILE_NAME, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
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
        private long EndOfPageCheck(long address, int objectSize, bool isRow = false)
        {
            long nextPageAddress = PageLocationHelper.GetNextDivisbleNumber(address, Globals.PageSize);

            long load = address + (objectSize + Globals.Int64ByteLength);

            if(isRow)
            {
                load += objectSize;
            }

            if ( load > nextPageAddress - 8)
            {
                long nextPagePointer = GetNextUnclaimedDataPage();

                using (FileStream fileStream = File.Open(Globals.FILE_NAME, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
                {
                    using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                    {
                        binaryWriter.BaseStream.Position = nextPageAddress - 8; 

                        binaryWriter.Write(nextPagePointer);

                        binaryWriter.BaseStream.Position = nextPagePointer;

                        binaryWriter.Write((short)0);

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
