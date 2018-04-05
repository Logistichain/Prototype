using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Cryptography;
using Mpb.Consensus.Logic.MiscLogic;

namespace Mpb.Consensus.Test.Logic.TransactionValidator
{
    /// <summary>
    /// These testmethods are structured by using the AAA method (Arrange, Act, Assert).
    /// </summary>
    [TestClass]
    public class TransactionValidatorTest
    {
        Mock<ITimestamper> _timestamper;

        [TestInitialize]
        public void Initialize()
        {
            _timestamper = new Mock<ITimestamper>(MockBehavior.Strict);
        }        

        [TestCleanup]
        public void Cleanup()
        {
            _timestamper.VerifyAll();
        }
    }
}
