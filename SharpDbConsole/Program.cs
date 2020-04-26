using SharpDb;
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
                FillRows();

                //var reader = new Reader();
                SelectWithPredicates();
                //var allToolRows = reader.GetAllRows("Tools");



                //FillRows();
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

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 0, Type = Globals.StringType, ByteSize = 41, ColumnName = "ToolName" });

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 1, Type = Globals.DecimalType, ByteSize = Globals.DecimalByteLength, ColumnName = "Price" });

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 2, Type = Globals.Int32Type, ByteSize = Globals.Int32ByteLength, ColumnName = "NumInStock" });

            return table;
        }

        private static TableDefinition BuildPersonTable()
        {
            TableDefinition table = new TableDefinition();
            table.TableName = "Person";

            table.ColumnDefinitions = new List<ColumnDefinition>();

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 0, Type = Globals.StringType, ByteSize = 21, ColumnName = "Name" });

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 1, Type = Globals.Int32Type, ByteSize = Globals.Int32ByteLength, ColumnName = "Age" });

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 2, Type = Globals.BooleanType, ByteSize = Globals.BooleanByteLength, ColumnName = "IsAdult" });

            return table;
        }

        private static TableDefinition BuildFamilyTable()
        {
            TableDefinition table = new TableDefinition();
            table.TableName = "Family";

            table.ColumnDefinitions = new List<ColumnDefinition>();

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 0, Type = Globals.StringType, ByteSize = 21, ColumnName = "FamilyName" });

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 1, Type = Globals.Int32Type, ByteSize = Globals.Int32ByteLength, ColumnName = "NumberMembers" });

            return table;
        }

        //private static void WritePersonRows()
        //{
        //    var writer = new Writer();

        //    object[] row = new object[3];

        //    row[0] = "Evan";
        //    row[1] = 33;
        //    row[2] = true;

        //    writer.WriteRow("Person", row);

        //    object[] row2 = new object[3];

        //    row2[0] = "Jess";
        //    row2[1] = 33;
        //    row2[2] = true;

        //    writer.WriteRow("Person", row2);

        //    object[] row3 = new object[3];

        //    row3[0] = "Anna";
        //    row3[1] = 9;
        //    row3[2] = false;

        //    writer.WriteRow("Person", row3);

        //    object[] row4 = new object[3];

        //    row4[0] = "Emmy";
        //    row4[1] = 5;
        //    row4[2] = false;

        //    writer.WriteRow("Person", row4);
        //}

        //private static void WriteToolRows()
        //{
        //    var writer = new Writer();

        //    object[] row = new object[3];

        //    row[0] = "Sawzall";
        //    row[1] = 33.34m;
        //    row[2] = 2;

        //    writer.WriteRow("Tools", row);

        //    object[] row2 = new object[3];

        //    row2[0] = "Hammer";
        //    row2[1] = 12.67m;
        //    row2[2] = 45;

        //    writer.WriteRow("Tools", row2);

        //    object[] row3 = new object[3];

        //    row3[0] = "Screwdriver";
        //    row3[1] = 2.34m;
        //    row3[2] = 55;

        //    writer.WriteRow("Tools", row3);

        //    object[] row4 = new object[3];

        //    row4[0] = "Drill";
        //    row4[1] = 45.99m;
        //    row4[2] = 12;

        //    writer.WriteRow("Tools", row4);
        //}

        //private static void RunFullTest()
        //{
        //    var p = BuildPersonTable();
        //    var t = BuildToolsTable();
        //    var f = BuildFamilyTable();

        //    var writer = new Writer();

        //    writer.WriteTableDefinition(p);
        //    writer.WriteTableDefinition(t);
        //    writer.WriteTableDefinition(f);

        //    WritePersonRows();
        //    WriteToolRows();

        //    //var reader = new Reader();

        //    //var allPersonRows = reader.GetAllRows("Person");

        //    //var allToolRows = reader.GetAllRows("Tools");
        //}

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

            for (int i = 0; i < 160; i++)
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


            for (int i = 0; i < 120; i++)
            {
                object[] row = new object[3];

                row[0] = CreateString(10);
                row[1] = rd.Next();
                row[2] = DateTime.Now.Ticks % 2 == 0 ? true : false;

                writer.WriteRow(row, personTableDef);
            }

            for (int i = 0; i < 180; i++)
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
            var parser = new SelectParser();

            var interpreter = new Interpreter(parser);

            string query = "select * from Tools WHERE Price > 10.00 or ToolName = 'Drill' and Price > 25.00";

            var rows = interpreter.RunQuery(query);
        }


    }
}
