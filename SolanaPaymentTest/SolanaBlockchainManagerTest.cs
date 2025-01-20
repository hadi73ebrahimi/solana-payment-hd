using SolanaPaymentHD;
using SolanaPaymentHD.Logic;
using SolanaPaymentHD.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolanaPaymentTest
{
    [TestClass]
    [Ignore]
    public class SolanaBlockchainManagerTest
    {

        private SolanaBlockchainManager _manager;
        string _Rpc = "";
        [TestInitialize]
        public void SetUp()
        {
            // Create an instance of SolanaBlockchainManager
            _manager = new SolanaBlockchainManager(_Rpc);
        }

        [TestMethod]
        [Ignore]
        public async Task TestLastPayer()
        {
            var lastpayer =await _manager.GetLatestPayer("", 0.01m);
            Assert.IsNull(lastpayer);
            lastpayer =await _manager.GetLatestPayer("",0.01m);
            Console.WriteLine(lastpayer);
            Assert.IsNotNull(lastpayer);

        }

        [TestMethod]
        public async Task TestTransfer()
        {
            var pv = "";
            var walletempty =new PaymentWallet()
            {
                Address = "",
                PrivateKey = pv,
            };

            var activewallet = new PaymentWallet()
            {
                Address = "",
                PrivateKey = ""
            };
            var falsetransfer = await _manager.TransferFunds(walletempty, 0.01m, "");
            Assert.IsFalse(falsetransfer,"falsetransfer is true");

            var truetransfer = await _manager.TransferFunds(activewallet, 0.01m, "");
            Assert.IsTrue(truetransfer,"truetransfer is not true");
        }

        [TestMethod]
        [Ignore]
        public async Task TestBalance()
        {
            var balance = await _manager.CheckWalletBalance("");
            Console.WriteLine(balance);
            Assert.IsTrue(balance>0,"balance is zero but supposed not to");
        }
    }
}
