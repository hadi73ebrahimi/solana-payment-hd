using Org.BouncyCastle.Asn1;
using solana_payment.Logic;
using SolanaPaymentHD.Logic;
using SolanaPaymentHD.Models;
using Solnet.Wallet;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SolanaPaymentHD
{
    public class SolPaymentService
    {
        private readonly List<PaymentWallet> _walletPool = new();
        private readonly List<ActiveTransaction> _WaitingForPayment = new();
        private readonly List<PaidTransaction> _FinalTransactions = new();

        private readonly object _lock = new();

        private readonly HdWalletGenerator _hdWalletGenerator;
        private readonly SolanaBlockchainManager _solanacrypto;
        private readonly string _masterAddress = "MASTER_SOLANA_ADDRESS";
        
        public string Rpc { get; private set; }
        public int WalletExpireation { get; private set; } = 1;
        

        private decimal MinValue = 0.0001m;
        public SolPaymentService(string masterSeed,string finaladdress, string rpc, int initialWalletCount = 20 )
        {
            _hdWalletGenerator = new HdWalletGenerator(masterSeed);
            _solanacrypto = new SolanaBlockchainManager(rpc);
            // Initialize the pool with initial wallets
            for (int i = 0; i < initialWalletCount; i++)
            {
                _walletPool.Add(_hdWalletGenerator.GenerateWallet());
            }
            _masterAddress = finaladdress;
            // Start the checker cycle
            Task.Run(() => StartCheckerCycle());
            Rpc = rpc;
        }

        public void UpdateWalletExpirationTime(int minutes)
        {
            WalletExpireation = minutes;
        }

        public string GetHDwalletAddress(int id)
        {
            return _walletPool[id].Address;
        }

        public void UpdateRpc(string rpc)
        {
            this.Rpc = rpc;
            _solanacrypto.UpdateRpc(Rpc);
        }

        public string GetNewPaymentWallet(object payload)
        {
            lock (_lock)
            {
                var availableWallet = GetFreeWallet(); 


                availableWallet.InUse = true;
                availableWallet.InUseUntil = DateTime.UtcNow.AddMinutes(WalletExpireation);

                // Record the payload information in PaidTransaction
                _WaitingForPayment.Add(new ActiveTransaction(availableWallet.Address, payload));

                return availableWallet.Address;
            }
        }

        public IEnumerable<PaidTransaction> GetPaidTransactions()
        {
            lock (_lock)
            {
                var transactions = _FinalTransactions.ToArray();
                _FinalTransactions.Clear();
                return transactions;
            }
        }

        public int GetInUseWalletsCount()
        {
            return _WaitingForPayment.Count;
        }

        public int GetTotalWalletPoolCount()
        {
            return _walletPool.Count;
        }

        private async Task StartCheckerCycle()
        {
            while (true)
            {
                foreach (var wallet in _walletPool.Where(w => w.InUse))
                {
                    var activepayment = _WaitingForPayment.FirstOrDefault(it => it.PaymentWallet == wallet.Address);
                    if (activepayment!=null && activepayment.IsPaid()) { continue; }

                    decimal balance = await _solanacrypto.CheckWalletBalance(wallet.Address);
                    string payer =await _solanacrypto.GetLatestPayer(wallet.Address);
                    
                    lock (_lock)
                    {
                        if (balance > MinValue)
                        {
                            UpdateTransactionRecord(wallet.Address, balance, payer);
                        }
                        // using balance to wait for transfer
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

        private async void TransferFunds(PaymentWallet wallet, decimal balance)
        {
            await _solanacrypto.TransferFunds(wallet, balance);
            ReleaseAddress(wallet);
        }

        private void ReleaseAddress(PaymentWallet wallet)
        {
            wallet.InUse = false;
            wallet.InUseUntil = null;
            var activewallet = _WaitingForPayment.FirstOrDefault(it => it.PaymentWallet == wallet.Address);
            if(activewallet!=null)
            {
                _WaitingForPayment.Remove(activewallet);
            }
        }

        private void UpdateTransactionRecord(string address,decimal amount,string paidby)
        {
            var activewallet = _WaitingForPayment.FirstOrDefault(it=> it.PaymentWallet==address);
            if (activewallet == null) { return; }
            activewallet.UpdatePay(paidby,amount);


        }


        private PaymentWallet GetFreeWallet()
        {
            var wallet = _walletPool.FirstOrDefault(w => !w.InUse);

            if (wallet == null)
            {
                // Generate a new wallet if none are available
                wallet = _hdWalletGenerator.GenerateWallet();
                _walletPool.Add(wallet);
            }
            return wallet;
        }
    }

}
