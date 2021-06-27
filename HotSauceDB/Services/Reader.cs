﻿using HotSauceDB.Models;
using HotSauceDb.Enums;
using HotSauceDb.Helpers;
using HotSauceDb.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HotSauceDb.Services
{
    public class Reader
    {
        public IndexPage GetIndexPage()
        {
            IndexPage indexPage = new IndexPage();

            short objectCount = GetObjectCount(0);

            if(objectCount == 0)
            {
                return new IndexPage();
            }

            long tableDefinitionAddress = 2;

            bool nextPageHasTables = true;

            using (FileStream fileStream = new FileStream(Globals.FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    reader.BaseStream.Position = 2;

                    while(nextPageHasTables)
                    {
                        for (int i = 0; i < objectCount; i++)
                        {
                            var tableDefinition = new TableDefinition();
                            tableDefinition.DataAddress = reader.ReadInt64();
                            tableDefinition.TableName = reader.ReadString();
                            tableDefinition.TableDefinitionAddress = tableDefinitionAddress;

                            tableDefinitionAddress += Globals.TABLE_DEF_LENGTH;


                            try
                            {
                                while (reader.PeekChar() != '|') // | signifies end of current table defintion
                                {
                                    var columnDefinition = new ColumnDefinition();
                                    columnDefinition.ColumnName = reader.ReadString();
                                    columnDefinition.Index = reader.ReadByte();
                                    columnDefinition.Type = (TypeEnum)reader.ReadByte();
                                    columnDefinition.ByteSize = reader.ReadInt16();
                                    columnDefinition.IsIdentity = reader.ReadByte();

                                    if (columnDefinition.IsIdentity == 1)
                                    {
                                        tableDefinition.TableContainsIdentityColumn = true;
                                    }

                                    tableDefinition.ColumnDefinitions.Add(columnDefinition);
                                }
                            }
                            catch (Exception ex)
                            {

                                throw;
                            }



                            reader.BaseStream.Position = GetNextTableDefinitionStartAddress(reader.BaseStream.Position);

                            indexPage.TableDefinitions.Add(tableDefinition);
                        }

                        long nextPageAddress = GetPointerToNextPage(reader.BaseStream.Position);

                        reader.BaseStream.Position = nextPageAddress;

                        short numObjectsOnNextPage = reader.ReadInt16();

                        tableDefinitionAddress = nextPageAddress + 2;

                        if (numObjectsOnNextPage > 0)
                        {
                            objectCount = numObjectsOnNextPage;
                        }
                        else
                        {
                            nextPageHasTables = false;
                        }
                    }


                    return indexPage;
                }
            }

        }

        public long GetNextTableDefinitionStartAddress(long currentPosition)
        {
            while ((currentPosition - 2) % Globals.TABLE_DEF_LENGTH != 0)//todo: rewrite without while loop
            {
                currentPosition += 1;
            }

            return currentPosition;
        }

        public SelectData GetRows(TableDefinition tableDefinition, IEnumerable<SelectColumnDto> selects, List<PredicateOperation> predicateOperations)
        {
            var selectData = new SelectData();

            selectData.Rows = new List<List<IComparable>>();
            selectData.RowLocations = new List<long>();

            short rowCount = GetObjectCount(tableDefinition.DataAddress);

            using (FileStream fileStream = new FileStream(Globals.FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    bool isDataToRead = true;

                    fileStream.Position = tableDefinition.DataAddress + 2;

                    while (isDataToRead)
                    {
                        for (int i = 0; i < rowCount; i++)
                        {
                            long rowLocation = fileStream.Position;

                            List<IComparable> row = new List<IComparable>();

                            List<IComparable> rowToEvaluate = new List<IComparable>();

                            foreach (SelectColumnDto select in selects)
                            {
                                IComparable val;

                                val = ReadColumn(select, binaryReader);

                                rowToEvaluate.Add(val);

                                if (select.IsInSelect)
                                {
                                    row.Add(val);
                                }
                            }

                            bool addRow = EvaluateRow(predicateOperations, rowToEvaluate);

                            if (addRow)
                            {
                                selectData.Rows.Add(row);
                                selectData.RowLocations.Add(rowLocation);
                            }
                                
                        }

                        long nextPagePointer = GetPointerToNextPage(fileStream.Position);

                        if (nextPagePointer == 0m)
                        {
                            return selectData;
                        }
                        else
                        {
                            rowCount = GetObjectCount(nextPagePointer);

                            fileStream.Position = nextPagePointer + Globals.Int16ByteLength;
                        }
                    }
                }
            }

            return selectData;
        }

        public List<List<IComparable>> ReadDataFromPage(long pageAddress, List<ColumnDefinition> columnDefinitions, BinaryReader binaryReader)
        {
            int rowCount = GetObjectCount(pageAddress);

            var data = new List<List<IComparable>>();

            binaryReader.BaseStream.Position = pageAddress + Globals.Int16ByteLength;

            for (int i = 0; i < rowCount; i++)
            {
                List<IComparable> row = new List<IComparable>();

                foreach (ColumnDefinition column in columnDefinitions)
                {
                    row.Add(ReadColumn(column, binaryReader));
                }

                data.Add(row);
            }

            return data;
        }


        public long GetPointerToNextPage(long pageAddress)
        {
            long pointerToNextPage = PageLocationHelper.GetNextDivisbleNumber(pageAddress, Globals.PageSize)
                                        - Globals.Int64ByteLength;

            using (FileStream fs = new FileStream(Globals.FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (BinaryReader binaryReader = new BinaryReader(fs))
                {
                    binaryReader.BaseStream.Position = pointerToNextPage;

                    long nextPageAddress = binaryReader.ReadInt64();

                    return nextPageAddress;
                }
            }
        }


        public short GetObjectCount(long rowCountPointer)
        {
            using (FileStream fileStream = new FileStream(Globals.FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fileStream.Position = rowCountPointer;

                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    short rowCount = binaryReader.ReadInt16();

                    return rowCount;
                }
            }
        }

        public IComparable ReadColumn(ColumnDefinition columnDefintion, BinaryReader binaryReader)
        {
            switch (columnDefintion.Type)
            {
                case TypeEnum.Boolean:
                    return binaryReader.ReadBoolean();
                case TypeEnum.Char:
                    return binaryReader.ReadChar();
                case TypeEnum.Decimal:
                    return binaryReader.ReadDecimal();
                case TypeEnum.Int32:
                    return binaryReader.ReadInt32();
                case TypeEnum.Int64:
                    return binaryReader.ReadInt64();
                case TypeEnum.String:
                    return binaryReader.ReadString();
                case TypeEnum.DateTime:
                    return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                        .AddSeconds(binaryReader.ReadInt64());
                default:
                    throw new Exception("invalid column definition type");
            }
        }

        public List<IComparable> ReadRow(List<ColumnDefinition> columnDefinitions, BinaryReader binaryReader)
        {
            List<IComparable> data = new List<IComparable>();

            foreach (var columnDefinition in columnDefinitions)
            {
                data.Add(ReadColumn(columnDefinition, binaryReader));
            }

            return data;
        }

        public long GetFirstAvailableDataAddress(long dataStart, int objectSize)
        {
            while (PageIsFull(dataStart, objectSize))
            {
                dataStart = PageLocationHelper.GetNextPagePointer(dataStart);
            }

            using (FileStream fileStream = new FileStream(Globals.FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fileStream.Position = dataStart;

                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    short numObjects = binaryReader.ReadInt16();

                    return objectSize * numObjects + 2 + dataStart;//todo: use global 2 byte integer size
                }
            }
        }

        public bool PageIsFull(long address, int objectSize)
        {
            using (FileStream fileStream = new FileStream(Globals.FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fileStream.Position = address;

                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    short numObjects = binaryReader.ReadInt16();

                    //2 bytes for row count
                    //8 bytes for page pointer
                    //x bytes for row data

                    return objectSize + (numObjects * objectSize) + Globals.Int64ByteLength  + Globals.Int16ByteLength > Globals.PageSize; 
                }
            }
        }

        //this method should be in a different class. it doesnt have anything to do with data access
        public bool EvaluateRow(List<PredicateOperation> predicateOperations, List<IComparable> row)
        {
            if(predicateOperations.Count() == 0)
            {
                return true;
            }

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

        public bool DatabaseEmpty()
        {
            using (FileStream fileStream = new FileStream(Globals.FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return fileStream.Length == 0;
            }
        }

        //this method should be in a different class. it doesnt have anything to do with data access
        private bool EvaluateOperator(string operation, bool delgateResult, bool willAddRow)
        {
            switch (operation.ToLower())
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
