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

        public List<List<IComparable>> GetAllRows(string tableName)
        {
            var indexPage = GetIndexPage();

            var tableDefinition = indexPage.TableDefinitions.Where(x => x.TableName == tableName).FirstOrDefault();

            var rows = new List<List<IComparable>>();

            using (FileStream fileStream = new FileStream(Globals.FILE_NAME, FileMode.Open))
            {
                fileStream.Position = tableDefinition.DataAddress;

                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    while(binaryReader.PeekChar() != -1 && binaryReader.PeekChar() != 0)
                    {
                        List<IComparable> row = new List<IComparable>();

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

        public IComparable ReadColumn(ColumnDefinition columnDefintion, BinaryReader binaryReader)
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
