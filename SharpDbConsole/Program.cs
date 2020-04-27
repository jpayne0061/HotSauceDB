using SharpDb;
using SharpDb.Enums;
using SharpDb.Models;
using SharpDb.Services;
using SharpDb.Services.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpDbConsole
{
    class Program
    {
        static void Main(string[] args)
        {

            try
            {
                var reader = new Reader();

                var indexPage = reader.GetIndexPage();

                var tableDef = indexPage.TableDefinitions.Where(x => x.TableName == "Tools").FirstOrDefault();

                var writer = new Writer();

                object[] rowz = new object[3];

                rowz[0] = " A very cool Drill ";
                rowz[1] = 678.99m;
                rowz[2] = 89;

                writer.WriteRow(rowz, tableDef);

                //object[] rowz3 = new object[3];

                //rowz3[0] = " A very cool Drill ";
                //rowz3[1] = 56.99m;
                //rowz3[2] = 256;

                //writer.WriteRow(rowz3, tableDef);


                //object[] rowz2 = new object[3];

                //rowz2[0] = "A big hammer  ";
                //rowz2[1] = 678.99m;
                //rowz2[2] = 89;

                //writer.WriteRow(rowz2, tableDef);

                //var stream = File.Create("data.txt");

                //stream.Close();

                //FillRows();

                SelectWithPredicates();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private static TableDefinition BuildToolsTable()
        {
            TableDefinition table = new TableDefinition();
            table.TableName = "Tools";

            table.ColumnDefinitions = new List<ColumnDefinition>();

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 0, Type = TypeEnums.String, ByteSize = 41, ColumnName = "ToolName" });

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 1, Type = TypeEnums.Decimal, ByteSize = Globals.DecimalByteLength, ColumnName = "Price" });

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 2, Type = TypeEnums.Int32, ByteSize = Globals.Int32ByteLength, ColumnName = "NumInStock" });

            return table;
        }

        private static TableDefinition BuildPersonTable()
        {
            TableDefinition table = new TableDefinition();
            table.TableName = "Person";

            table.ColumnDefinitions = new List<ColumnDefinition>();

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 0, Type = TypeEnums.String, ByteSize = 21, ColumnName = "Name" });

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 1, Type = TypeEnums.Int32, ByteSize = Globals.Int32ByteLength, ColumnName = "Age" });

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 2, Type = TypeEnums.Boolean, ByteSize = Globals.BooleanByteLength, ColumnName = "IsAdult" });

            return table;
        }

        private static TableDefinition BuildFamilyTable()
        {
            TableDefinition table = new TableDefinition();
            table.TableName = "Family";

            table.ColumnDefinitions = new List<ColumnDefinition>();

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 0, Type = TypeEnums.String, ByteSize = 21, ColumnName = "FamilyName" });

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 1, Type = TypeEnums.Int32, ByteSize = Globals.Int32ByteLength, ColumnName = "NumberMembers" });

            return table;
        }

        public static void FillRows()
        {
            var t = BuildToolsTable();

            var p = BuildPersonTable();

            var writer = new Writer();

            writer.WriteTableDefinition(t);
            writer.WriteTableDefinition(p);

            Random rd = new Random();

            var reader = new Reader();

            var indexPage = reader.GetIndexPage();

            var tableDef = indexPage.TableDefinitions.Where(x => x.TableName == "Tools").FirstOrDefault();


            //fill first data page, and partially fill second
            for (int i = 0; i < 1000; i++)
            {
                object[] row = new object[3];

                row[0] = CreateString(20);
                row[1] = (decimal)rd.NextDouble();
                row[2] = rd.Next();

                writer.WriteRow(row, tableDef);
            }

            object[] rowx = new object[3];

            rowx[0] = "Drill";
            rowx[1] = 33.78m;
            rowx[2] = 89;

            writer.WriteRow(rowx, tableDef);

            var personTableDef = indexPage.TableDefinitions.Where(x => x.TableName == "Person").FirstOrDefault();

            //write rows for second table
            for (int i = 0; i < 12000; i++)
            {
                object[] row = new object[3];

                row[0] = CreateString(10);
                row[1] = rd.Next();
                row[2] = DateTime.Now.Ticks % 2 == 0 ? true : false;

                writer.WriteRow(row, personTableDef);
            }

            //continue writing writing rows for first table
            for (int i = 0; i < 1800; i++)
            {
                object[] row = new object[3];

                row[0] = CreateString(20);
                row[1] = (decimal)rd.NextDouble();
                row[2] = rd.Next();

                writer.WriteRow(row, tableDef);
            }

            object[] rowz = new object[3];

            rowz[0] = "Drill";
            rowz[1] = 678.99m;
            rowz[2] = 89;

            writer.WriteRow(rowz, tableDef);

            object[] rowz2 = new object[3];

            rowz2[0] = "Drill";
            rowz2[1] = 22.21m;
            rowz2[2] = 4;

            writer.WriteRow(rowz2, tableDef);

        }

        internal static string CreateString(int stringLength)
        {
            Random rd = new Random();

            const string allowedChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789";
            char[] chars = new char[stringLength];

            for (int i = 0; i < stringLength; i++)
            {
                chars[i] = allowedChars[rd.Next(0, allowedChars.Length)];
            }

            return new string(chars);
        }



        public static void SelectWithPredicates()
        {
            var interpreter = new Interpreter(new SelectParser());

            string query = "select * from Tools WHERE ToolName = ' A very cool Drill                      '";

            //string query = "select * from Tools WHERE price > 500";

            //string query = "select * from Person WHERE IsAdult = false";

            var rows = interpreter.RunQuery(query);
        }


    }
}
