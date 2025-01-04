using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solana_payment.Models
{
    internal sealed class PaymentWallet
    {

        public string Address { get; set; }
        public string PrivateKey { get; set; }
        public bool InUse { get; set; }
        public DateTime? InUseUntil { get; set; }
    }
}
