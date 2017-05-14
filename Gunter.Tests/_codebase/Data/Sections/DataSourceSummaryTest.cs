using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using Gunter.Data.SqlClient;
using Gunter.Data.Sections;
using System.Collections.Generic;
using Reusable.Data;
using Reusable.Logging;
using Gunter.Data;
using Gunter.Services;
using Gunter.Alerts.Sections;

namespace Gunter.Tests.Data.Sections
{
    [TestClass]
    public class DataSourceSummaryTest
    {
        //[TestMethod]
        public void Create_blah()
        {
            var testData = new DataTable();
            var now = new DateTime(2017, 3, 3);
            testData.Columns.Add("Timestamp", typeof(DateTime));
            testData.AddRow(now.AddDays(1));
            testData.AddRow(now.AddDays(2));
            testData.AddRow(now.AddDays(3));
            testData.AddRow(now.AddDays(4));

            var testContext = new Gunter.Data.TestContext
            {
                DataSource = new TableOrViewDataSource(new NullLogger())
                {
                    Commands =
                    {
                        ["Main"] =new Command { Text = "SELECT * FROM [Main]" },
                        ["Debug"] =new Command { Text = "SELECT * FROM [Debug]" }
                    }
                },
                //Data = testData
            };

            //var section = new DataSourceSummary(new NullLogger()).Create(testContext, ConstantResolver.Empty);

            //Assert.AreEqual(section.Data.Rows[0]["Value"], "SELECT * FROM [Main]");
            //Assert.AreEqual(section.Data.Rows[1]["Value"], "SELECT * FROM [Debug]");
            //Assert.AreEqual(section.Data.Rows[2]["Value"], "4");
            //Assert.AreEqual(section.Data.Rows[3]["Value"], new DateTime(2017, 3, 4).ToString());
            //Assert.AreEqual(section.Data.Rows[4]["Value"], TimeSpan.FromDays(3).ToString());
        }
    }
}
