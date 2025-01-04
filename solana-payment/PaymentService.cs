using Org.BouncyCastle.Asn1;
using solana_payment.Logic;
using solana_payment.Models;
using Solnet.Wallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace solana_payment
{
    public class PaymentService
    {
        private readonly List<PaymentWallet> _walletPool = new();
        private readonly List<ActiveTransaction> _paidTransactions = new();
        private readonly List<PaidTransaction> _FinalTransactions = new();

        private readonly object _lock = new();

        private readonly HdWalletGenerator _hdWalletGenerator;
        private readonly string _masterAddress = "MASTER_SOLANA_ADDRESS";

        private decimal MinValue = 0.0001m;
        public PaymentService(string masterSeed,string finaladdress, int initialWalletCount = 20)
        {
            _hdWalletGenerator = new HdWalletGenerator(masterSeed);

            // Initialize the pool with initial wallets
            for (int i = 0; i < initialWalletCount; i++)
            {
                _walletPool.Add(_hdWalletGenerator.GenerateWallet());
            }
            _masterAddress = finaladdress;
            // Start the checker cycle
            Task.Run(() => StartCheckerCycle());
        }

        public string GetWallet(object payload)
        {
            lock (_lock)
            {
                var availableWallet = _walletPool.FirstOrDefault(w => !w.InUse);

                if (availableWallet == null)
                {
                    // Generate a new wallet if none are available
                    availableWallet = _hdWalletGenerator.GenerateWallet();
                    _walletPool.Add(availableWallet);
                }

                availableWallet.InUse = true;
                availableWallet.InUseUntil = DateTime.UtcNow.AddMinutes(10);

                // Record the payload information in PaidTransaction
                _paidTransactions.Add(new ActiveTransaction(availableWallet.Address, payload));

                return availableWallet.Address;
            }
        }

        public IEnumerable<PaidTransaction> GetPaidTransactions()
        {
            lock (_lock)
            {
                var transactions = _paidTransactions.Where(it=> it.Amount>0).ToArray();
                foreach (var transaction in transactions)
                {
                    _paidTransactions.Remove(transaction);
                }
                return transactions;
            }
        }

        private async Task StartCheckerCycle()
        {
            while (true)
            {
                foreach (var wallet in _walletPool.Where(w => w.InUse))
                {
                    // Simulate checking the balance
                    decimal balance = await CheckWalletBalance(wallet.Address);
                    string payer =await GetLatestPayer(wallet.Address);

                    lock(_lock)
                    {
                        if (balance > MinValue)
                        {
                            UpdateTransactionRecord(wallet.Address, balance, payer);
                        }
                        else if (wallet.InUseUntil < DateTime.UtcNow && balance<= MinValue)
                        {
                            ReleaseAddress(wallet);
                        }
                    }


                    if (balance > 0) { Task.Run(()=> TransferFunds(wallet, balance)); }
                }

                await Task.Delay(TimeSpan.FromSeconds(15));
            }
        }

        private void ReleaseAddress(PaymentWallet wallet)
        {
            wallet.InUse = false;
            wallet.InUseUntil = null;
        }

        private void UpdateTransactionRecord(string address,decimal amount,string paidby)
        {
            //_paidTransactions.Add(new PaidTransaction(wallet.Address, balance, DateTime.UtcNow, null));
        }

        private async Task<decimal> CheckWalletBalance(string address)
        {
            // Simulate checking wallet balance from Solana API
            await Task.Delay(500); // Simulated API call
            return new Random().Next(0, 10); // Simulated balance
        }

        private async void TransferFunds(PaymentWallet wallet, decimal balance)
        {


            ReleaseAddress(wallet);
        }

        private async Task<string> GetLatestPayer(string address)
        {
            // Simulate checking wallet balance from Solana API
            await Task.Delay(500); // Simulated API call
            return ""; // Simulated balance
        }
    }

}
