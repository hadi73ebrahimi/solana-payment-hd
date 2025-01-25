using Org.BouncyCastle.Asn1;
using SolanaPaymentHD.Logic;
using SolanaPaymentHD.Logic;
using SolanaPaymentHD.Models;
using Solnet.Wallet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection.PortableExecutable;
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
        
        public RPCData Rpc { get; private set; }
        public int WalletExpireation { get; private set; } = 15;

        public decimal MaxLeftover = 0.0001m;

        public SolPaymentService(RPCData rpc,string masterSeed,string finaladdress, int initialWalletCount = 20 )
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

        public void UpdateRpc(RPCData rpc)
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
                var tasks = _walletPool
                    .Where(w => w.InUse) 
                    .Select(pwall => ProcessWalletAsync(pwall)) 
                    .ToList();

                await Task.WhenAll(tasks);

                await Task.Delay(TimeSpan.FromSeconds(15));
            }
        }

        private async Task ProcessWalletAsync(PaymentWallet wallet)
        {
            try
            {
                var activePayment = GetActivePayment(wallet.Address);

                if (activePayment == null) return;
                if (activePayment.IsPaid()) return;
                var isexpired = IsActiveTransactionExpired(wallet);
                decimal balance = await CheckWalletBalance(wallet);
                if (balance <= MaxLeftover && isexpired) { ReleaseExpiredWallet(wallet);return; }
                if (balance <= MaxLeftover) { return; }

                string? payer = await GetLatestPayer(wallet);
                ProcessTransaction(wallet, balance, payer);

                if (balance > 0)
                {
                    await TransferFunds(wallet, balance- MaxLeftover);
                }
            }
            catch (Exception ex)
            {
                HandleError(wallet, ex);
            }
        }

        private ActiveTransaction? GetActivePayment(string walletAddress)
        {
            return _WaitingForPayment.FirstOrDefault(it => it.PaymentWallet == walletAddress);
        }

        private async Task<decimal> CheckWalletBalance(PaymentWallet wallet)
        {
            return await _solanacrypto.CheckWalletBalance(wallet.Address);
        }

        private async Task<string?> GetLatestPayer(PaymentWallet wallet)
        {
            return await _solanacrypto.GetLatestPayer(wallet.Address, MaxLeftover);
        }

        private void ReleaseExpiredWallet(PaymentWallet wallet)
        {
            lock(_lock)
            {
                ReleaseAddress(wallet);
            }
            
        }

        private void ProcessTransaction(PaymentWallet wallet, decimal balance, string? payer)
        {
            lock (_lock)
            {
                UpdateTransactionRecord(wallet.Address, balance, payer);
 
            }
        }

        private void HandleError(PaymentWallet wallet, Exception ex)
        {
            // Log the error but continue processing other wallets
            Console.WriteLine($"Error processing wallet {wallet.Address}: {ex.Message}");
            // You can log this to a file or monitoring system as well
        }

        private async Task TransferFunds(PaymentWallet wallet, decimal balance)
        {
            var state = await _solanacrypto.TransferAllFunds(wallet,_masterAddress);
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
            _FinalTransactions.Add(activewallet.GetRecord());
        }

        private bool IsActiveTransactionExpired(PaymentWallet wallet)
        {
            return wallet.InUseUntil < DateTime.UtcNow;
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
