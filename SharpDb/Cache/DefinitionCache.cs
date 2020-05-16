using SharpDb.Models;
using SharpDb.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpDb.Cache
{
    public static class DefinitionCache
    {
        private static IndexPage _indexPage;

        public static List<TableDefinition> TableDefintions { get; set; }

        //public static IndexPage GetIndexPage()
        //{
        //    if(_indexPage == null)
        //    {
        //        Reader reader = new Reader();
        //        var indexPage = reader.GetIndexPage();

        //        _indexPage = indexPage;

        //        return indexPage;
        //    }
        //    else
        //    {
        //        return _indexPage;
        //    }
        //}

        public static void UpdateIndexPage(IndexPage indexPage)
        {
            _indexPage = indexPage;
        }
    }
}
