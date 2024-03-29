using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using DataMigrator.Core.Models;
using DataMigrator.Intrastructure.Data;


namespace DataMigrator
{
    public class Scheduler
    {
        private static string query = "select * from c where c.status = 'pending'";
        private static ICosmosDbRespository<MigrationTask> _repo;
        public Scheduler(ICosmosDbRespository<MigrationTask> repo)
        {
            _repo = repo;
        }

        [FunctionName("scheduler")]
        public void Run(
            [TimerTrigger("0 */2 * * * *")] TimerInfo timer, 
            [ServiceBus("tasks", Connection = "SERVICEBUS_CONNECTIONSTRING", EntityType = EntityType.Queue)] ICollector<string> output,
            ILogger log)
        {
            log.LogInformation($"{DateTime.Now} Timer trigger function");
            var tasks = _repo.GetByQuery(query);
            
            if ( tasks.Count() == 0 ) {
                log.LogInformation($"No pending tasks to scheduled. Count - {tasks.Count()}");
                log.LogInformation($"Tasks info - {JsonConvert.SerializeObject(tasks)}");
            }     
               
            foreach( var task in tasks ) {
                var t = JsonConvert.SerializeObject(task);
                log.LogInformation($"Emitting task to Service Bus - {t}");
                output.Add(t);
            }  
        }
    }
}
