using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolanaPaymentHD.Models
{
    public class RPCData
    {
        public string RPC { get; init; }
        public int RatePerSecond { get; init; }
        public RPCData(string rPC, int ratePerSecond)
        {
            RPC = rPC;
            RatePerSecond = ratePerSecond;
        }
    }
}
