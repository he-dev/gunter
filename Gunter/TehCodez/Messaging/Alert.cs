using System;
using System.Collections.Generic;
using System.Linq;
using Gunter.Data;
using Gunter.Reporting;
using Gunter.Services;
using Newtonsoft.Json;
using Reusable.Logging;

namespace Gunter.Messaging
{
    public interface IAlert
    {
        [JsonRequired]
        int Id { get; set; }

        [JsonRequired]
        List<int> Reports { get; set; }

        void Publish(TestContext context);
    }

    public abstract class Alert : IAlert
    {
        public int Id { get; set; }

        public List<int> Reports { get; set; }

        public abstract void Publish(TestContext context);        
    }
}
