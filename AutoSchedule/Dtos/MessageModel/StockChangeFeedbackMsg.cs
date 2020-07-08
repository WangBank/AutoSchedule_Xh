using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoSchedule.Dtos.MessageModel
{
    public class StockChangeFeedbackMsg
    {
        public List<StockChangeItem> items { get; set; }
    }

    public class StockChangeItem
    {
        public string ownerCode { get; set; }
        public string warehouseCode { get; set; }
        public string orderCode { get; set; }
        public string orderType { get; set; }
        public string outBizCode { get; set; }
        public string itemCode { get; set; }
        public string itemId { get; set; }
        public string inventoryType { get; set; }
        public string quantity { get; set; }
        public string changeTime { get; set; }

        public string batchCode { get; set; }

        public string produceCode { get; set; }

        public string remark { get; set; }

        public string isLocked { get; set; }
    }

}
