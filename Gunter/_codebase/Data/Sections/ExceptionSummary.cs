using Gunter.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Gunter.Data.Sections
{
    internal class ExceptionSummary : ISectionFactory
    {
        public string Heading { get; set; }

        public string[] Columns { get; set; }

        public string[] GroupBy { get; set; }

        public ISection Create(TestContext context, IConstantResolver constants)
        {
            return new Section
            {
                Heading = "Exceptions",
                Data = new DataTable(),
                Orientation = Orientation.Horizontal
            };

            // Groups exceptions by message and exception message.

            // We don't want the entire exception list. A sigle one from each group is enough.

            //var exceptions = data.AsEnumerable().GroupBy(x => new
            //{
            //    //LogLevel = x.Field<string>(nameDictionary[DataColumn.LogLevel]),
            //    //Logger = x.Field<string>(nameDictionary[DataColumn.Logger]),
            //    //Message = x.Field<string>(nameDictionary[DataColumn.Message]),
            //    //Exception = GetExceptionMessage(x.Field<string>(nameDictionary[DataColumn.Message])),
            //})
            //.Select(g => new ExceptionInfo
            //{
            //    Id = g.First().Field<int>(nameDictionary[DataColumn.PrimaryKey]),
            //    Timestamp = g.First().Field<DateTime>(nameDictionary[DataColumn.Timestamp]),
            //    Logger = g.Key.Logger,
            //    LogLevel = g.Key.LogLevel,
            //    Message = g.Key.Message,
            //    Exception = g.Key.Exception,
            //    Count = g.Count()
            //});
            //return exceptions;
        }

        private static string GetExceptionMessage(string exceptionString)
        {
            if (string.IsNullOrEmpty(exceptionString)) { return string.Empty; }

            // Exception message is at the first line of an exception string. Extract it.
            var result = exceptionString.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return result;
        }
    }
}
