using NuGet.Frameworks;
using SolanaPaymentHD.Utils;
using SolanaPaymentHD;
using System.Diagnostics;
using System.Text.Json;
namespace SolanaPaymentTest
{
    [TestClass]
    public class ServiceTest
    {
        string rpc = "";
        string seed = "";
        string targetwallet = "";
        [TestMethod]
        public void TestHdWalletsAndPrint()
        {
            var Payserv = new SolPaymentService(seed, targetwallet, rpc);
            for (int i = 0; i < 20; i++)
            {
                var address = Payserv.GetHDwalletAddress(i);
                Assert.IsNotNull(address);
                Console.WriteLine(address);
            }
        }

        [TestMethod]
        public void TestMnemonicGenerator()
        {
            var words24 = MnemonicUtils.GenerateMnemonic(true);
            var words = MnemonicUtils.GenerateMnemonic(false);
            Assert.IsTrue(words24.Count() == 24, "MnemonicGenerator Isn't 24 words");
            Assert.IsTrue(words.Count() == 12, "MnemonicGenerator Isn't 12 words");
            Console.WriteLine(String.Join(' ', words));
            Console.WriteLine(String.Join(' ',words24));
        }

        [TestMethod]
        [Ignore]
        public async Task TestPaymentPoolExpireWalletWithoutPayment()
        {
            var Payserv = new SolPaymentService(seed, targetwallet, rpc);
            Payserv.UpdateWalletExpirationTime(1);

            var newpaywallet = Payserv.GetNewPaymentWallet("sdffhgs");
            Console.WriteLine(newpaywallet);
            Assert.IsTrue(Payserv.GetInUseWalletsCount() == 1, "In use wallet don't match the number {0}", Payserv.GetInUseWalletsCount());
            Assert.IsNotNull(newpaywallet);
            await Task.Delay(76000);
            Assert.IsTrue(Payserv.GetInUseWalletsCount() == 0, "In use wallet don't match the number {0}", Payserv.GetInUseWalletsCount());

        }

        [TestMethod]
        public async Task TestPaymentWalletSuccessPayment()
        {
            var Payserv = new SolPaymentService(seed, targetwallet, rpc);
            Payserv.UpdateWalletExpirationTime(15);
            var newpaywallet = Payserv.GetNewPaymentWallet("paymenthere");
            Debug.WriteLine(newpaywallet);
            Assert.IsTrue(Payserv.GetInUseWalletsCount() == 1, "In use wallet don't match the number {0}", Payserv.GetInUseWalletsCount());

            while(true)
            {
                var paidones = Payserv.GetPaidTransactions();
                //Debug.WriteLine("count:" + paidones.Count());
                foreach (var paidtransaction in paidones)
                {
                    Debug.WriteLine(paidtransaction);
                    Debug.WriteLine(JsonSerializer.Serialize(paidtransaction));
                }
                if (paidones.Count() > 0) { break; }
                await Task.Delay(1000);
            }
            await Task.Delay(20000);
            Assert.IsTrue(Payserv.GetInUseWalletsCount() == 0, "In use wallet don't match the number {0}", Payserv.GetInUseWalletsCount());
        }

    }
}