using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solana_payment.Models
{
    public record PaidTransaction(string PaymentWallet,string Payer,DateTime Time,decimal Amount,object Payload);
    internal class ActiveTransaction
    {
        public string PaymentWallet { get; init; }
        public decimal Amount { get; private set; }
        public object Payload { get; init; }
        public string Payer { get; private set; }
        public ActiveTransaction(string paymentWallet,object payload)
        {
            PaymentWallet = paymentWallet;
            Payload = payload;
        }

        public void UpdatePay(string payer,decimal amount)
        {
            Payer = payer;
            Amount = amount;
        }

        public PaidTransaction GetRecord()
        {
            return new PaidTransaction(PaymentWallet,Payer, DateTime.UtcNow,Amount, Payload);
        }


    }
}
