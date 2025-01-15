using SolanaPaymentHD;
using SolanaPaymentHD.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolanaPaymentTest
{
    [TestClass]
    
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
        public async Task TestLastPayer()
        {
            var lastpayer =await _manager.GetLatestPayer("HoMo5BpknU1xhKDsuzcAfDJsG7E8weD9rruSFaCcvQuN",0.01m);
            Console.WriteLine(lastpayer);
            Assert.IsNotNull(lastpayer);

        }

        [TestMethod]
        public void TestTransfer()
        {

        }

        [TestMethod]
        public async Task TestBalance()
        {
            var balance = await _manager.CheckWalletBalance("C57XeiA2fru7MoHsa71QTYxZNFrdHBdxjSNSHbrgBWsc");
            Console.WriteLine(balance);
            Assert.IsTrue(balance>0,"balance is zero but supposed not to");
        }
    }
}
