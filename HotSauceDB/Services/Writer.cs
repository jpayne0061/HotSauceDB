using HotSauceDB.Helpers;
using HotSauceDB.Models;
using HotSauceDb.Enums;
using HotSauceDb.Helpers;
using HotSauceDb.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HotSauceDb.Services
{
    public class Writer
    {
        private Reader _reader;
        private IndexPage _indexPage;
        public Writer(Reader reader)
        {
            _reader = reader;

            if (NoTables())
            {
                WriteZero(0);
            }

            _indexPage = _reader.GetIndexPage();
        }

        public IComparable WriteRow(IComparable[] row, TableDefinition tableDef, long addressToWrite, bool updateCount = true)
        {
            bool isEdit = !updateCount;

            IComparable identity = WriteRow(row, addressToWrite, tableDef, isEdit);

            if (!updateCount)
                return identity;

            UpdateObjectCount(addressToWrite);

            return identity;
        }

        private IComparable WriteRow(IComparable[] row, long diskLocation, TableDefinition tableDefinition, bool isEdit)
        {
            int rowSize = tableDefinition.GetRowSizeInBytes();

            long addressToWriteTo = diskLocation;

            WritePointerIfLastObjectOnPage(diskLocation, rowSize);

            bool isFirstRowOnPage = (addressToWriteTo - Constants.Int16_Byte_Length) % Constants.Page_Size == 0 && !isEdit;

            //if first row, write pointer as zero
            if (isFirstRowOnPage)
            {
                WriteZeroPointerForFirstRow(addressToWriteTo);
            }

            bool isFirstRowOfTable = addressToWriteTo - Constants.Int16_Byte_Length == tableDefinition.DataAddress;

            IComparable newIdentityValue = 0;

            if (!isEdit && tableDefinition.TableContainsIdentityColumn)
            {
                newIdentityValue = GetNewIdentityValue(tableDefinition, isFirstRowOfTable);

                if (row.Length == tableDefinition.ColumnDefinitions.Count())
                {
                    row = row.Skip(1).ToArray();
                }

                row = CopyArrayWithNewIdentity(row, newIdentityValue);
            }

            using (FileStream fileStream = File.Open(Constants.FILE_NAME, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
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

            return newIdentityValue;
        }

        private IComparable GetNewIdentityValue(TableDefinition tableDefinition, bool isFirstRowOfTable)
        {
            IComparable newIdentityValue = 0;

            IComparable previousIdentityValue; 

            if(!isFirstRowOfTable)
            {
                previousIdentityValue = _reader.GetLastRowFromTable(tableDefinition)[0];
            }
            else
            {
                previousIdentityValue = ConvertToNumberType(newIdentityValue, tableDefinition.ColumnDefinitions[0].Type);
            }

            newIdentityValue = IncrementNumberValue(previousIdentityValue, tableDefinition.ColumnDefinitions[0].Type);

            return newIdentityValue;
        }

        private IComparable[] CopyArrayWithNewIdentity(IComparable[] row, IComparable newIdentityValue)
        {
            IComparable[] newValues = new IComparable[row.Length + 1];
            newValues[0] = newIdentityValue;
            Array.Copy(row, 0, newValues, 1, row.Length);

            return newValues;
        }

        private IComparable IncrementNumberValue(IComparable comparable, TypeEnum typeEnum)
        {
            switch (typeEnum)
            {
                case TypeEnum.Decimal:
                    return (decimal)comparable + 1;
                case TypeEnum.Int32:
                    return (int)comparable + 1;
                case TypeEnum.Int64:
                    return (long)comparable + 1;
                default:
                    throw new Exception("invalid identity column");
            }
        }

        private IComparable ConvertToNumberType(IComparable comparable, TypeEnum typeEnum)
        {
            switch (typeEnum)
            {
                case TypeEnum.Decimal:
                    return Convert.ToDecimal(comparable);
                case TypeEnum.Int32:
                    return Convert.ToInt32(comparable);
                case TypeEnum.Int64:
                    return Convert.ToInt64(comparable);
                default:
                    throw new Exception("invalid identity column");
            }
        }

        public void WriteZeroPointerForFirstRow(long currentAddress)
        {
            if((currentAddress - 2) % Constants.Page_Size != 0)
            {
                throw new Exception("Invalid address for first row: " + currentAddress);
            }

            long zeroPointerAddress = currentAddress + (Constants.Next_Pointer_Address - 2);

            using (FileStream fileStream = File.Open(Constants.FILE_NAME, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
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
                    binaryWriter.Write(string.Empty.PadRight(columnDefinition.ByteSize - 1, ' '));
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
            //need to pass in address of current page, not zero
            long addressToWrite = _reader.GetFirstAvailableDataAddress(0, Constants.TABLE_DEF_LENGTH);

            if (IsFirstRow(addressToWrite))
            {
                var pointerToNextIndexRecord = GetNextUnclaimedDataPage();
                WriteLong(addressToWrite - 2 + Constants.Next_Pointer_Address, pointerToNextIndexRecord);
                WriteZero(pointerToNextIndexRecord);
            }

            var resultMessage =  WriteTableDefinition(tableDefinition, addressToWrite);

            var nextFreeDataPage = tableDefinition.DataAddress == 0 ? GetNextUnclaimedDataPage() : tableDefinition.DataAddress;

            WriteLong(nextFreeDataPage, (long)0);

            UpdateObjectCount(resultMessage.Address);

            return resultMessage;
        }

        public ResultMessage RenameTable(TableDefinition tableDefinition)
        {
            using (FileStream stream = File.Open(Constants.FILE_NAME, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(stream))
                {
                    binaryWriter.BaseStream.Position = tableDefinition.TableDefinitionAddress + Constants.Int64_Byte_Length;// lastTableDefAddress;

                    binaryWriter.Write(tableDefinition.TableName);
                }
            }

            return new ResultMessage { Message = $"table {tableDefinition.TableName} has been renamed"};
        }
        
        private bool IsFirstRow(long addressToWrite)
        {
            return (addressToWrite - 2) % Constants.Page_Size == 0;
        }

        private ResultMessage WriteTableDefinition(TableDefinition tableDefinition, long addressToWrite)
        {
            //this should return current next page, instead of the first page address
            //first page isn't really full
            var newDefinitionAddress = addressToWrite;

            WritePointerIfLastObjectOnPage(addressToWrite, Constants.TABLE_DEF_LENGTH);

            var nextFreeDataPage = tableDefinition.DataAddress == 0 ? GetNextUnclaimedDataPage() : tableDefinition.DataAddress;

            long tableDefEnd = 0;

            using (FileStream stream = File.Open(Constants.FILE_NAME, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(stream))
                {
                    binaryWriter.BaseStream.Position = newDefinitionAddress;// lastTableDefAddress;

                    binaryWriter.Write(nextFreeDataPage);
                    binaryWriter.Write(tableDefinition.TableName);

                    for (int i = 0; i < tableDefinition.ColumnDefinitions.Count; i++)
                    {
                        binaryWriter.Write(tableDefinition.ColumnDefinitions[i].ColumnName);
                        binaryWriter.Write(tableDefinition.ColumnDefinitions[i].Index);
                        binaryWriter.Write((byte)tableDefinition.ColumnDefinitions[i].Type);
                        binaryWriter.Write(tableDefinition.ColumnDefinitions[i].ByteSize);
                        if(i == 0 && tableDefinition.TableContainsIdentityColumn)
                        {
                            binaryWriter.Write((byte)1);
                        }
                        else
                        {
                            binaryWriter.Write((byte)0);
                        }
                    }

                    tableDefEnd = stream.Position;

                    binaryWriter.Write(Constants.End_Table_Definition);
                }
            }

            _indexPage = _reader.GetIndexPage();

            return new ResultMessage { Message = $"table {tableDefinition.TableName} has been added successfully", Address = tableDefEnd, Data = nextFreeDataPage };
        }

        public ResultMessage AlterTableDefinition(TableDefinition tableDefinition, ColumnDefinition newColumn)
        {
            int oldRowSize = tableDefinition.GetRowSizeInBytes();

            tableDefinition.ColumnDefinitions.Add(newColumn);

            WriteTableDefinition(tableDefinition, tableDefinition.TableDefinitionAddress);

            tableDefinition.ColumnDefinitions.RemoveAt(tableDefinition.ColumnDefinitions.Count - 1);

            if(_reader.GetObjectCount(tableDefinition.DataAddress) > 0)
            {
                BackFillRows(tableDefinition, oldRowSize, newColumn);
            }

            return new ResultMessage();
        }

        /// Data for the table being edited needs to be back filled with the new column
        /// 
        /// Example:
        /// 
        /// old row structure:                     "a string value",6,false
        /// new row structure (adds char column):  "a string value",6,false,'v'
        /// 
        /// Since rows are written back-to-back, they need to be pulled up into memory, edited to add the
        /// new column value, whatever the default is, and written back to disk
        public ResultMessage BackFillRows(TableDefinition tableDefinition, int oldRowSize, ColumnDefinition newColumn)
        {
            var currentPages = GetPages(tableDefinition.DataAddress); 

            //get first address of data for table
            long dataStart = tableDefinition.DataAddress;

            //how many rows need to be read into buffer?
            int uneditedBufferCount = (int)Math.Ceiling((decimal)(tableDefinition.GetRowSizeInBytes() + newColumn.ByteSize) / oldRowSize);

            var newDefinition = new TableDefinition(tableDefinition);
            newDefinition.ColumnDefinitions.Add(newColumn);

            int newRowSize = newDefinition.GetRowSizeInBytes();

            var proposedPages = AddNewPagesAndRowCounts(currentPages, tableDefinition.DataAddress, oldRowSize, newRowSize);

            Queue<List<IComparable>> unEditedRowBufferQueue = new Queue<List<IComparable>>();

            int maxNewRowsToBeWrittenToSinglePage = (Constants.Page_Size - (Constants.Int64_Byte_Length + Constants.Int16_Byte_Length)) / newRowSize;

            long nextAddressToWriteTo = 0L;

            List<long> pageAddresses = proposedPages.Keys.OrderBy(k => k).ToList();

            for (int i = 0; i < pageAddresses.Count; i++)
            {
                long pageAddress = pageAddresses[i];

                BackfillPage backfillPage = proposedPages[pageAddress];

                if(!backfillPage.NewlyAllocatedPage)
                {
                    using (FileStream fs = new FileStream(Constants.FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (BinaryReader binaryReader = new BinaryReader(fs))
                        {
                            var rows = _reader.ReadDataFromPage(pageAddress, tableDefinition.ColumnDefinitions, binaryReader);

                            foreach (List<IComparable> row in rows)
                            {
                                unEditedRowBufferQueue.Enqueue(row);
                            }
                        }
                    }
                }

                nextAddressToWriteTo = pageAddress + Constants.Int16_Byte_Length;

                int rowCount = backfillPage.NewlyAllocatedPage ? backfillPage.NewRowCount : backfillPage.OldRowCount;

                for (int j = 0; j < rowCount; j++)
                {
                    if (nextAddressToWriteTo + newRowSize > (pageAddress + Constants.Next_Pointer_Address))
                    {
                        SetObjectCount(pageAddress, (short)j);
                        break;
                    }

                    List<IComparable> rowToEdit = unEditedRowBufferQueue.Dequeue();

                    rowToEdit.Add(DefaultValues.GetDefaultValueForType(newColumn.Type));

                    WriteRow(rowToEdit.ToArray(), newDefinition, nextAddressToWriteTo, updateCount: false);

                    nextAddressToWriteTo += newRowSize;

                    if (j == rowCount - 1)
                    {
                        SetObjectCount(pageAddress, (short)(j + 1));
                    }
                }

                //next page is newly allocated, so need to set pointer
                if(i != pageAddresses.Count - 1 && proposedPages[pageAddresses[i + 1]].NewlyAllocatedPage)
                {
                    WriteLong(pageAddress + Constants.Next_Pointer_Address, proposedPages[pageAddresses[i + 1]].StartAddress);
                }

                if(backfillPage.NewlyAllocatedPage)
                {
                    WriteLong(pageAddress + Constants.Next_Pointer_Address, 0L);
                }

            }

            return new ResultMessage();
        }

        public Dictionary<long, BackfillPage> GetPages(long tableDataAddress)
        {
            var pages = new Dictionary<long, BackfillPage>();

            long currentPageAddress = tableDataAddress;

            do {
                BackfillPage backfillPage = new BackfillPage();
                backfillPage.OldRowCount = _reader.GetObjectCount(currentPageAddress);
                backfillPage.StartAddress = currentPageAddress;
                backfillPage.NextPagePointer = _reader.GetPointerToNextPage(currentPageAddress);
                currentPageAddress = backfillPage.NextPagePointer;

                pages[backfillPage.StartAddress] = backfillPage;
            }
            while (currentPageAddress != 0L);

            return pages;
        }

        public Dictionary<long, BackfillPage> AddNewPagesAndRowCounts(Dictionary<long, BackfillPage> currentPages, long firstPageAddress, int oldRowSize, int newRowSize)
        {
            //var pages = new Dictionary<long, BackfillPage>();

            int numNewRowsMax = (Constants.Page_Size - (Constants.Int64_Byte_Length + Constants.Int16_Byte_Length)) / newRowSize;

            int totalRows = 0;

            long currentPageAddress = firstPageAddress;

            //get count of rows
            while (currentPages.ContainsKey(currentPageAddress))
            {
                totalRows += currentPages[currentPageAddress].OldRowCount;

                currentPageAddress = currentPages[currentPageAddress].NextPagePointer;
            }

            currentPageAddress = firstPageAddress;

            while (currentPages.ContainsKey(currentPageAddress))
            {
                var page = currentPages[currentPageAddress];

                bool pageIsFull = (Constants.Page_Size - (Constants.Int16_Byte_Length + Constants.Int64_Byte_Length)) / oldRowSize == page.OldRowCount;

                if(pageIsFull || (page.OldRowCount * newRowSize) > Constants.PAGE_DATA_MAX)
                {
                    page.NewRowCount = (short)numNewRowsMax;
                    totalRows -= page.NewRowCount;
                }
                else if(!pageIsFull)
                {
                    page.NewRowCount = page.OldRowCount;
                    totalRows -= page.OldRowCount;
                }

                currentPageAddress = currentPages[currentPageAddress].NextPagePointer;

                if (currentPageAddress == 0 && totalRows > 0)
                {
                    while(totalRows > 0)
                    {
                        //add new pages
                        BackfillPage newPage = new BackfillPage();
                        newPage.StartAddress = GetNextUnclaimedDataPage();

                        //**update old page pointer
                        page.NextPagePointer = newPage.StartAddress;

                        newPage.OldRowCount = 0;
                        newPage.NewlyAllocatedPage = true;

                        if (totalRows > numNewRowsMax)
                        {
                            newPage.NewRowCount = (short)numNewRowsMax;
                            totalRows -= numNewRowsMax;
                        }
                        else if (totalRows <= numNewRowsMax)
                        {
                            newPage.NewRowCount = (short)totalRows;
                            totalRows = 0;
                        }

                        currentPages[newPage.StartAddress] = newPage;

                        page = newPage;
                    }
                    
                }
            }

            return currentPages;
        }




        void WriteZero(long address)
        {
            using (FileStream stream = File.Open(Constants.FILE_NAME, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
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
            using (FileStream stream = File.Open(Constants.FILE_NAME, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(stream))
                {
                    binaryWriter.BaseStream.Position = address;

                    binaryWriter.Write((long)num);
                }
            }
        }

        private bool NoTables()
        {
            using (FileStream stream = new FileStream(Constants.FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
            using (FileStream stream = new FileStream(Constants.FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    reader.BaseStream.Position = Constants.Int16_Byte_Length;

                    while (reader.PeekChar() != -1 && reader.PeekChar() != 0)
                    {
                        reader.BaseStream.Position += Constants.TABLE_DEF_LENGTH;
                    }

                    return reader.BaseStream.Position;
                }
            }
        }

        private long GetNextUnclaimedDataPage(IndexPage indexPage)
        {
            long headAddress = indexPage.TableDefinitions.Count == 0 ? Constants.Page_Size :
                                indexPage.TableDefinitions.Max(x => x.DataAddress);

            long nextFreeAddress = headAddress;

            using (FileStream stream = new FileStream(Constants.FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (BinaryReader binaryReader = new BinaryReader(stream))
                {
                    binaryReader.BaseStream.Position = headAddress;

                    while (binaryReader.PeekChar() != -1)
                    {
                        binaryReader.BaseStream.Position += Constants.Page_Size;
                    }

                    return binaryReader.BaseStream.Position;
                }
            }

        }

        private long GetNextUnclaimedDataPage()
        {
            lock(_reader)
            {
                return GetNextUnclaimedDataPage(_indexPage);
            }
        }

        private void UpdateObjectCount(long currentAddress)
        {
            long addressOfCount = currentAddress - (currentAddress % Constants.Page_Size);

            short numRows = 0;

            using (FileStream fileStream = new FileStream(Constants.FILE_NAME, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    reader.BaseStream.Position = addressOfCount;

                    numRows = reader.ReadInt16();
                }
            }

            using (FileStream fileStream = new FileStream(Constants.FILE_NAME, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                {
                    binaryWriter.BaseStream.Position = addressOfCount;

                    numRows += 1;

                    binaryWriter.Write(numRows);
                }
            }

        }

        private void SetObjectCount(long countAddress, short objectCount)
        {
            using (FileStream fileStream = new FileStream(Constants.FILE_NAME, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                {
                    binaryWriter.BaseStream.Position = countAddress;

                    binaryWriter.Write(objectCount);
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
        private void WritePointerIfLastObjectOnPage(long address, int objectSize)
        {
            long nextPageAddress = PageLocationHelper.GetNextDivisbleNumber(address, Constants.Page_Size);

            long load = address + (objectSize + Constants.Int64_Byte_Length);

            bool willBeLastObjectOnPage = load + objectSize  >= nextPageAddress - Constants.Int64_Byte_Length;

            if(willBeLastObjectOnPage)
            {
                long nextPagePointer = GetNextUnclaimedDataPage();

                using (FileStream fileStream = File.Open(Constants.FILE_NAME, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
                {
                    using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                    {
                        binaryWriter.BaseStream.Position = nextPageAddress - Constants.Int64_Byte_Length;

                        binaryWriter.Write(nextPagePointer);

                        binaryWriter.BaseStream.Position = nextPagePointer;

                        binaryWriter.Write((short)0);
                    }
                }
            }
        }

    }
}
