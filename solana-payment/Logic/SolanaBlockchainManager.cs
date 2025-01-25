using SolanaPaymentHD.Logic;
using SolanaPaymentHD.Models;
using Solnet.Programs;
using Solnet.Rpc.Builders;
using Solnet.Rpc;
using Solnet.Wallet;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Solnet.Rpc.Models;
using System.Text.Json;

[assembly: InternalsVisibleTo("SolanaPaymentTest")]
namespace SolanaPaymentHD.Logic
{
    internal class SolanaBlockchainManager
    {
        private string _Rpc;
        private TokenBucketRateLimiter _rateLimiter;
        private readonly object _rateLimiterLock = new object(); // Lock for thread-safe updates

        public SolanaBlockchainManager(RPCData rpc)
        {
            _Rpc = rpc.RPC;
            _rateLimiter = new TokenBucketRateLimiter(rpc.RatePerSecond, TimeSpan.FromSeconds(1));
        }

        public void UpdateRpc(RPCData rpc)
        {
            lock (_rateLimiterLock)
            {
                _Rpc = rpc.RPC;
                _rateLimiter = new TokenBucketRateLimiter(rpc.RatePerSecond, TimeSpan.FromSeconds(1));
            }
        }

        private TokenBucketRateLimiter GetCurrentRateLimiter()
        {
            lock (_rateLimiterLock)
            {
                return _rateLimiter;
            }
        }

        public async Task<decimal> CheckWalletBalance(string address)
        {
            var rateLimiter = GetCurrentRateLimiter();
            await rateLimiter.WaitAsync();
            try
            {
                var rpcClient = ClientFactory.GetClient(_Rpc);
                var balanceResult = await rpcClient.GetBalanceAsync(address);

                if (!balanceResult.WasSuccessful)
                {
                    throw new Exception($"Failed to fetch balance for address {address}: {balanceResult.Reason}");
                }

                return (decimal)balanceResult.Result.Value / 1_000_000_000m;
            }
            finally
            {

            }
        }

        public async Task<bool> TransferAllFunds(PaymentWallet wallet, string target)
        {
            var rateLimiter = GetCurrentRateLimiter();
            await rateLimiter.WaitAsync();
            try
            {
                var rpcClient = ClientFactory.GetClient(_Rpc);

                var senderAccount = new Account(wallet.PrivateKey, wallet.Address);
                var accountInfo = await rpcClient.GetAccountInfoAsync(senderAccount.PublicKey);

                if (accountInfo?.Result?.Value == null)
                {
                    Debug.WriteLine("Failed to fetch account info.");
                    return false;
                }

                await rateLimiter.WaitAsync();
                var rentExemptBalance = await rpcClient.GetMinimumBalanceForRentExemptionAsync(accountInfo.Result.Value.Data.Count);

                ulong currentBalance = accountInfo.Result.Value.Lamports;

                if (currentBalance <= rentExemptBalance.Result)
                {
                    Debug.WriteLine("Insufficient funds to cover rent exemption.");
                    return false;
                }

                ulong amountToSend = currentBalance - rentExemptBalance.Result;

                await rateLimiter.WaitAsync();
                var blockHash = await rpcClient.GetLatestBlockHashAsync();

                var transaction = new TransactionBuilder()
                    .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                    .SetFeePayer(senderAccount.PublicKey)
                    .AddInstruction(SystemProgram.Transfer(senderAccount.PublicKey, new PublicKey(target), amountToSend))
                    .Build(senderAccount);

                await rateLimiter.WaitAsync();
                var sendTransactionResult = await rpcClient.SendTransactionAsync(transaction);

                return !string.IsNullOrEmpty(sendTransactionResult.Result);
            }
            finally
            {
            }
        }

        public async Task<bool> TransferFunds(PaymentWallet wallet, decimal balance, string target)
        {
            var rateLimiter = GetCurrentRateLimiter();
            await rateLimiter.WaitAsync();
            try
            {
                var rpcClient = ClientFactory.GetClient(_Rpc);

                await rateLimiter.WaitAsync();
                var blockHash = await rpcClient.GetLatestBlockHashAsync();

                var senderAccount = new Account(wallet.PrivateKey, wallet.Address);
                ulong amountInLamports = (ulong)(balance * 1_000_000_000m);

                await rateLimiter.WaitAsync();
                var accountInfo = await rpcClient.GetAccountInfoAsync(senderAccount.PublicKey);

                if (accountInfo?.Result?.Value == null)
                {
                    Debug.WriteLine("Failed to fetch account info.");
                    return false;
                }

                await rateLimiter.WaitAsync();
                var rentExemptBalance = await rpcClient.GetMinimumBalanceForRentExemptionAsync(accountInfo.Result.Value.Data.Count);

                if (accountInfo.Result.Value.Lamports < amountInLamports + rentExemptBalance.Result)
                {
                    Debug.WriteLine("Insufficient funds to cover both transaction and rent.");
                    return false;
                }

                var transaction = new TransactionBuilder()
                    .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                    .SetFeePayer(senderAccount.PublicKey)
                    .AddInstruction(SystemProgram.Transfer(senderAccount.PublicKey, new PublicKey(target), amountInLamports))
                    .Build(senderAccount);

                await rateLimiter.WaitAsync();
                var sendTransactionResult = await rpcClient.SendTransactionAsync(transaction);

                return !string.IsNullOrEmpty(sendTransactionResult.Result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
            }
        }

        public async Task<string?> GetLatestPayer(string address, decimal minamount)
        {
            var rateLimiter = GetCurrentRateLimiter();
            await rateLimiter.WaitAsync();
            try
            {
                var payeraddress = "";
                // Initialize the rpc client and a wallet
                var rpcClient = ClientFactory.GetClient(_Rpc);
                // Query transaction history for transactions to the given address
                var transactions = await rpcClient.GetSignaturesForAddressAsync(address, limit: 5);
                var iii = 0;
                if (transactions != null && transactions.Result.Count > 0)
                {
                    foreach (var transsign in transactions.Result)
                    {
                        var innerrateLimiter = GetCurrentRateLimiter();
                        await innerrateLimiter.WaitAsync();
                        var transactionDetails = await rpcClient.GetTransactionAsync(transsign.Signature);
                        File.WriteAllText(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + iii + ".json", JsonSerializer.Serialize(transactionDetails));
                        iii++;
                        if (transactionDetails != null)
                        {
                            if (IsOnlyTransfer(transactionDetails.Result) == false) { continue; }
                            if (IsReceiveSol(transactionDetails.Result.Meta) == false) { continue; }
                            if (IsAboveMinimumAmount(transactionDetails.Result.Meta, minamount) == false) { continue; }

                            var resultbalance = transactionDetails.Result.Meta.PreBalances[0] - transactionDetails.Result.Meta.PostBalances[0];
                            decimal balanceInSol = (decimal)resultbalance / 1_000_000_000m;
                            var payerAddress = transactionDetails.Result.Transaction.Message.AccountKeys[0].ToString();
                            return payerAddress;
                        }
                    }

                }

                return null;
            }
            finally
            {
            }
        }

        private bool IsReceiveSol(TransactionMeta meta)
        {
            return meta.PreBalances[0] > meta.PostBalances[0];
        }

        private bool IsAboveMinimumAmount(TransactionMeta meta, decimal minimum)
        {
            var resultbalance = meta.PreBalances[0] - meta.PostBalances[0];
            decimal balanceInSol = (decimal)resultbalance / 1_000_000_000m;
            return balanceInSol >= minimum;
        }

        private bool IsOnlyTransfer(TransactionMetaSlotInfo transactionResult)
        {
            if (transactionResult == null) { return false; }
            return !(transactionResult.Meta.InnerInstructions != null && transactionResult.Meta.InnerInstructions.Length > 0);
        }
    }
}

