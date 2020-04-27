using SharpDb.Enums;
using SharpDb.Helpers;
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
                            columnDefinition.Type = (TypeEnums)reader.ReadByte();
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

        public List<List<IComparable>> GetRows(string tableName, List<PredicateOperation> predicateOperations)
        {
            var indexPage = GetIndexPage();

            var tableDefinition = indexPage.TableDefinitions.Where(x => x.TableName == tableName).FirstOrDefault();

            var rows = new List<List<IComparable>>();

            short rowCount = GetRowCount(tableDefinition.DataAddress);

            using (FileStream fileStream = new FileStream(Globals.FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    bool isDataToRead = true;

                    fileStream.Position = tableDefinition.DataAddress + 2;

                    while (isDataToRead)
                    {
                        for (int i = 0; i < rowCount; i++)
                        {
                            List<IComparable> row = new List<IComparable>();

                            for (int j = 0; j < tableDefinition.ColumnDefinitions.Count; j++)
                            {
                                row.Add(ReadColumn(tableDefinition.ColumnDefinitions[j], binaryReader));
                            }

                            bool addRow = EvaluateRow(predicateOperations, row);

                            if (addRow)
                                rows.Add(row);
                        }

                        long nextPagePointer = GetPointerToNextPage(fileStream.Position);

                        if (nextPagePointer == 0m)
                        {
                            return rows;
                        }
                        else
                        {
                            rowCount = GetRowCount(nextPagePointer);

                            fileStream.Position = nextPagePointer + Globals.Int16ByteLength;
                        }
                    }
                }
            }

            return rows;
        }

        public long GetPointerToNextPage(long pageAddress)
        {
            long pointerToNextPage = PageLocationHelper.GetNextDivisbleNumber(pageAddress, Globals.PageSize)
                                        - Globals.Int64ByteLength;

            using (FileStream fs = new FileStream(Globals.FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (BinaryReader binaryReader = new BinaryReader(fs))
                {
                    binaryReader.BaseStream.Position = pointerToNextPage;

                    long nextPageAddress = binaryReader.ReadInt64();

                    return nextPageAddress;
                }
            }
        }

        public short GetRowCount(long rowCountPointer)
        {
            using (FileStream fileStream = new FileStream(Globals.FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fileStream.Position = rowCountPointer;

                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    short rowCount = binaryReader.ReadInt16();

                    return rowCount;
                }
            }
        }

        public List<List<IComparable>> StreamRows(string tableName)
        {
            var indexPage = GetIndexPage();

            var tableDefinition = indexPage.TableDefinitions.Where(x => x.TableName == tableName).FirstOrDefault();

            var rows = new List<List<IComparable>>();

            return rows;
        }

        public IComparable ReadColumn(ColumnDefinition columnDefintion, BinaryReader binaryReader)
        {
            switch (columnDefintion.Type)
            {
                case TypeEnums.Boolean:
                    return binaryReader.ReadBoolean();
                case TypeEnums.Char:
                    return binaryReader.ReadChar();
                case TypeEnums.Decimal:
                    return binaryReader.ReadDecimal();
                case TypeEnums.Int32:
                    return binaryReader.ReadInt32();
                case TypeEnums.Int64:
                    return binaryReader.ReadInt64();
                case TypeEnums.String:
                    return binaryReader.ReadString();
                default:
                    throw new Exception("invalid column definition type");
            }
        }

        public long GetFirstAvailableDataAddressOfTableByName(TableDefinition tableDef)
        {
            long address = tableDef.DataAddress;

            while(IsPageFull(address, tableDef))
            {
                address = PageLocationHelper.GetNextPagePointer(address);
            }

            int rowSize = tableDef.GetRowSizeInBytes();

            using (FileStream fileStream = File.OpenRead(Globals.FILE_NAME))
            {
                fileStream.Position = address;

                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    short numRows = binaryReader.ReadInt16();

                    return rowSize * numRows + 2 + address;
                }
            }
        }

        public bool IsPageFull(long address, TableDefinition tableDefinition)
        {
            int rowSize = tableDefinition.GetRowSizeInBytes();

            using (FileStream fileStream = File.OpenRead(Globals.FILE_NAME))
            {
                fileStream.Position = address;

                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    short numRows = binaryReader.ReadInt16();

                    //2 bytes for row count
                    //8 bytes for page pointer
                    //x bytes for row data

                    return rowSize + (numRows * rowSize) + Globals.Int64ByteLength  + Globals.Int16ByteLength > Globals.PageSize; 
                }
            }
        }

        public List<List<IComparable>> GetRowsWithPredicate(string tableName, List<PredicateOperation> predicateOperations)
        {
            var indexPage = GetIndexPage();

            var tableDefinition = indexPage.TableDefinitions.Where(x => x.TableName == tableName).FirstOrDefault();

            var rows = new List<List<IComparable>>();

            using (FileStream fileStream = new FileStream(Globals.FILE_NAME, FileMode.Open))
            {
                fileStream.Position = tableDefinition.DataAddress;

                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    while (binaryReader.PeekChar() != -1 && binaryReader.PeekChar() != 0)
                    {
                        List<IComparable> row = new List<IComparable>();

                        for (int j = 0; j < tableDefinition.ColumnDefinitions.Count; j++)
                        {
                            row.Add(ReadColumn(tableDefinition.ColumnDefinitions[j], binaryReader));
                        }

                        bool addRow = EvaluateRow(predicateOperations, row);

                        if(addRow)
                            rows.Add(row);
                    }
                }
            }

            return rows;
        }

        public bool EvaluateRow(List<PredicateOperation> predicateOperations, List<IComparable> row)
        {
            bool addRow = false;

            for (int i = 0; i < predicateOperations.Count(); i++)
            {
                bool delegateResult = predicateOperations[i].Delegate(row[predicateOperations[i].ColumnIndex], predicateOperations[i].Value);

                if (i == 0)
                {
                    addRow = delegateResult;
                    continue;
                }
                else
                {
                    addRow = EvaluateOperator(predicateOperations[i].Operator, delegateResult, addRow);
                }
            }

            return addRow;
        }

        private bool EvaluateOperator(string operation, bool delgateResult, bool willAddRow)
        {
            switch (operation)
            {
                case "and":
                    return willAddRow && delgateResult;
                case "or":
                    return willAddRow || delgateResult;
                default:
                    throw new Exception("Invalid operator: " + operation);
            }
        }

    }
}
